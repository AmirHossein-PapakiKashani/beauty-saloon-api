# Shared Inject helpers for run-backend-cycle.ps1
# Dot-source from the orchestrator and from Pester tests.

# $PSScriptRoot is set when this file is dot-sourced; $MyInvocation.MyCommand.Path is NULL inside functions.
$script:BackendCycleScriptsDir = $PSScriptRoot
if (-not $script:BackendCycleScriptsDir) {
    $script:BackendCycleScriptsDir = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "scripts"
}

function Get-BackendCycleScriptsDir {
    return $script:BackendCycleScriptsDir
}

function Get-ClineChildScriptPath {
    $child = Join-Path (Get-BackendCycleScriptsDir) "Invoke-ClineChild.ps1"
    if (-not (Test-Path $child)) {
        throw "Missing child script: $child"
    }
    return $child
}

function Get-AgentInjectCommand {
    param(
        [Parameter(Mandatory)]
        [string]$Agent,

        [Parameter(Mandatory)]
        [string]$Workspace,

        [Parameter(Mandatory)]
        [string]$PromptFile,

        [int]$TimeoutSeconds = 1500
    )

    $normalized = $Agent.ToLowerInvariant()
    if ($normalized -notin @("cline", "opencode", "agy")) {
        throw "Unknown agent '$Agent'. Use: cline | opencode | agy"
    }

    switch ($normalized) {
        "cline" {
            # MUST invoke with direct args — splatting @($args) breaks cline (TTY / interactive fallback).
            return [pscustomobject]@{
                Executable      = "cline"
                ArgumentList    = @("-c", $Workspace, "-t", "$TimeoutSeconds")
                UseStdin        = $false
                PromptAsArg     = $true
                PromptViaStdin  = $false
                UseDirectInvoke = $true
                LiveFeed        = $true
            }
        }
        "opencode" {
            return [pscustomobject]@{
                Executable      = "opencode"
                ArgumentList    = @("run", "--auto", "--dir", $Workspace)
                UseStdin        = $true
                PromptAsArg     = $false
                PromptViaStdin  = $false
                UseDirectInvoke = $false
                LiveFeed        = $false
            }
        }
        "agy" {
            return [pscustomobject]@{
                Executable      = "agy"
                ArgumentList    = @(
                    "--dangerously-skip-permissions",
                    "--mode", "accept-edits",
                    "--add-dir", $Workspace,
                    "--print-timeout", "${TimeoutSeconds}s",
                    "--print"
                )
                UseStdin        = $false
                PromptAsArg     = $false
                UseDirectInvoke = $false
                UseArgumentList = $true
                LiveFeed        = $true
            }
        }
    }
}

function Format-AgyTranscriptStep {
    param([Parameter(Mandatory)]$Step)

    $time = if ($Step.created_at) {
        try { [DateTime]::Parse($Step.created_at).ToLocalTime().ToString("HH:mm:ss") } catch { (Get-Date -Format "HH:mm:ss") }
    } else {
        (Get-Date -Format "HH:mm:ss")
    }
    $idx = if ($null -ne $Step.step_index) { $Step.step_index } else { "?" }

    if ($Step.tool_calls) {
        $lines = @()
        foreach ($tc in $Step.tool_calls) {
            $detail = $null
            if ($tc.args.AbsolutePath) { $detail = $tc.args.AbsolutePath }
            elseif ($tc.args.Path) { $detail = $tc.args.Path }
            elseif ($tc.args.TargetFile) { $detail = $tc.args.TargetFile }
            elseif ($tc.args.CommandLine) { $detail = $tc.args.CommandLine }
            elseif ($tc.args.command) { $detail = $tc.args.command }
            elseif ($tc.args.toolSummary) { $detail = $tc.args.toolSummary }
            else { $detail = "(no detail)" }
            if ($detail -and $detail.Length -gt 140) { $detail = $detail.Substring(0, 140) + "..." }
            $lines += [pscustomobject]@{
                Message = "[$time][STEP $idx][ACTION] $($tc.name) -> $detail"
                Color   = "Yellow"
            }
        }
        return $lines
    }

    if ($Step.thinking) {
        $thinkSummary = ($Step.thinking -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
        if ($thinkSummary.Length -gt 110) { $thinkSummary = $thinkSummary.Substring(0, 110) + "..." }
        return ,@([pscustomobject]@{
            Message = "[$time][STEP $idx][THINKING] $thinkSummary"
            Color   = "Cyan"
        })
    }

    if ($Step.type -eq "GENERIC" -and $Step.status -eq "DONE" -and $Step.content) {
        $firstLine = ($Step.content -split "`n")[0].Trim()
        if ($firstLine.Length -gt 120) { $firstLine = $firstLine.Substring(0, 120) + "..." }
        return ,@([pscustomobject]@{
            Message = "[$time][STEP $idx][RESULT] $firstLine"
            Color   = "DarkGreen"
        })
    }

    if ($Step.content -and $Step.type -eq "PLANNER_RESPONSE") {
        $respSummary = ($Step.content -split "`n" | Where-Object { $_.Trim() } | Select-Object -First 1)
        if ($respSummary.Length -gt 110) { $respSummary = $respSummary.Substring(0, 110) + "..." }
        return ,@([pscustomobject]@{
            Message = "[$time][STEP $idx][RESPONSE] $respSummary"
            Color   = "Green"
        })
    }

    return @()
}

function Enable-LocalProxyIfPresent {
    if ($env:HTTPS_PROXY -or $env:ALL_PROXY -or $env:HTTP_PROXY) { return }
    foreach ($port in @(7897, 10809)) {
        $ok = Test-NetConnection -ComputerName 127.0.0.1 -Port $port -InformationLevel Quiet -WarningAction SilentlyContinue
        if ($ok) {
            $env:HTTP_PROXY  = "http://127.0.0.1:$port"
            $env:HTTPS_PROXY = "http://127.0.0.1:$port"
            Write-Host "[INIT] Proxy → http://127.0.0.1:$port" -ForegroundColor Green
            return
        }
    }
    $socks = Test-NetConnection -ComputerName 127.0.0.1 -Port 10808 -InformationLevel Quiet -WarningAction SilentlyContinue
    if ($socks) {
        $env:ALL_PROXY = "socks5://127.0.0.1:10808"
        Write-Host "[INIT] SOCKS5 → socks5://127.0.0.1:10808" -ForegroundColor Green
    }
}

function Invoke-AgyWithLiveFeed {
    param(
        [Parameter(Mandatory)][string]$Workspace,
        [Parameter(Mandatory)][string]$Prompt,
        [int]$TimeoutSeconds = 1500
    )

    Enable-LocalProxyIfPresent

    $brainDir  = Join-Path $env:USERPROFILE ".gemini\antigravity-cli\brain"
    $cliLogDir = Join-Path $env:USERPROFILE ".gemini\antigravity-cli\log"

    $knownConvs = @{}
    if (Test-Path $brainDir) {
        Get-ChildItem $brainDir -Directory -ErrorAction SilentlyContinue | ForEach-Object { $knownConvs[$_.Name] = $true }
    }
    $knownCliLogs = @{}
    if (Test-Path $cliLogDir) {
        Get-ChildItem $cliLogDir -Filter "cli-*.log" -ErrorAction SilentlyContinue | ForEach-Object { $knownCliLogs[$_.FullName] = $true }
    }

    $agyPath = (Get-Command agy -ErrorAction Stop).Source
    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $agyPath
    $psi.WorkingDirectory = $Workspace
    $psi.UseShellExecute = $false
    # Do NOT redirect stdout/stderr — async PS handlers crash the host (no Runspace on thread pool).
    $psi.CreateNoWindow = $true
    $psi.ArgumentList.Add("--dangerously-skip-permissions")
    $psi.ArgumentList.Add("--mode")
    $psi.ArgumentList.Add("accept-edits")
    $psi.ArgumentList.Add("--add-dir")
    $psi.ArgumentList.Add($Workspace)
    $psi.ArgumentList.Add("--print-timeout")
    $psi.ArgumentList.Add("${TimeoutSeconds}s")
    $psi.ArgumentList.Add("--print")
    $psi.ArgumentList.Add($Prompt)

    Write-Host "[INJECT] agy live feed — THINKING/ACTION from transcript (main thread only)" -ForegroundColor Cyan

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    [void]$proc.Start()

    $transcriptReader = $null
    $transcriptFs = $null
    $cliLogReader = $null
    $cliLogFs = $null
    $activeCliLogPath = $null
    $lastStepSeen = -1
    $lastHeartbeat = [DateTime]::UtcNow
    $isAuthStalled = $false

    try {
        while (-not $proc.HasExited) {
            if (-not $activeCliLogPath -and (Test-Path $cliLogDir)) {
                $candidatesLog = Get-ChildItem $cliLogDir -Filter "cli-*.log" -ErrorAction SilentlyContinue |
                    Where-Object { -not $knownCliLogs.ContainsKey($_.FullName) }
                if ($candidatesLog) {
                    $latestLog = $candidatesLog | Sort-Object CreationTime -Descending | Select-Object -First 1
                    $activeCliLogPath = $latestLog.FullName
                    try {
                        $cliLogFs = [System.IO.FileStream]::new($activeCliLogPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                        $cliLogReader = [System.IO.StreamReader]::new($cliLogFs, [System.Text.Encoding]::UTF8)
                    } catch {}
                }
            }

            if ($cliLogReader) {
                while (-not $cliLogReader.EndOfStream) {
                    $logLine = $cliLogReader.ReadLine()
                    if (-not $logLine) { continue }
                    if ($logLine -match "triggering interactive OAuth|Authentication required") {
                        Write-Host "[$(Get-Date -Format 'HH:mm:ss')][AUTH-STALL] OAuth hang — aborting" -ForegroundColor Red
                        $isAuthStalled = $true
                        try { $proc.Kill() } catch {}
                        break
                    } elseif ($logLine -match "TLS handshake timeout") {
                        Write-Host "[$(Get-Date -Format 'HH:mm:ss')][NET] TLS timeout" -ForegroundColor Magenta
                    } elseif ($logLine -match "streamGenerateContent") {
                        Write-Host "[$(Get-Date -Format 'HH:mm:ss')][STREAM] LLM generating..." -ForegroundColor DarkGray
                    }
                }
            }
            if ($isAuthStalled) { break }

            if (-not $transcriptReader -and (Test-Path $brainDir)) {
                $candidates = Get-ChildItem $brainDir -Directory -ErrorAction SilentlyContinue |
                    Where-Object { -not $knownConvs.ContainsKey($_.Name) }
                if ($candidates) {
                    $latest = $candidates | Sort-Object CreationTime -Descending | Select-Object -First 1
                    $transPath = Join-Path $latest.FullName ".system_generated\logs\transcript.jsonl"
                    if (Test-Path $transPath) {
                        try {
                            $transcriptFs = [System.IO.FileStream]::new($transPath, [System.IO.FileMode]::Open, [System.IO.FileAccess]::Read, [System.IO.FileShare]::ReadWrite)
                            $transcriptReader = [System.IO.StreamReader]::new($transcriptFs, [System.Text.Encoding]::UTF8)
                            Write-Host "[$(Get-Date -Format 'HH:mm:ss')][SESSION] live transcript: $($latest.Name)" -ForegroundColor Green
                        } catch {}
                    }
                }
            }

            if ($transcriptReader) {
                while (-not $transcriptReader.EndOfStream) {
                    $tLine = $transcriptReader.ReadLine()
                    if (-not $tLine) { continue }
                    try {
                        $obj = $tLine | ConvertFrom-Json
                        if ($null -ne $obj.step_index -and $obj.step_index -gt $lastStepSeen) {
                            $lastStepSeen = $obj.step_index
                            $lastHeartbeat = [DateTime]::UtcNow
                            foreach ($line in (Format-AgyTranscriptStep -Step $obj)) {
                                Write-Host $line.Message -ForegroundColor $line.Color
                            }
                        }
                    } catch {}
                }
            }

            $silence = ([DateTime]::UtcNow - $lastHeartbeat).TotalSeconds
            if ($silence -gt 25) {
                Write-Host "[$(Get-Date -Format 'HH:mm:ss')][WAITING] still working ($([int]$silence)s quiet)..." -ForegroundColor DarkGray
                $lastHeartbeat = [DateTime]::UtcNow
            }

            Start-Sleep -Milliseconds 350
        }
    } finally {
        if ($cliLogReader) { $cliLogReader.Dispose(); if ($cliLogFs) { $cliLogFs.Dispose() } }
        if ($transcriptReader) { $transcriptReader.Dispose(); if ($transcriptFs) { $transcriptFs.Dispose() } }
    }

    if (-not $proc.HasExited) {
        try { $proc.WaitForExit() } catch {}
    }

    $exitCode = $proc.ExitCode
    if ($null -eq $exitCode) { $exitCode = 1 }
    if ($isAuthStalled) { $exitCode = 2 }

    return $exitCode
}

function Format-ClineOutputLine {
    param([Parameter(Mandatory)][string]$Line)

    $color = "White"
    if ($Line -match '^\[thinking\]') { $color = "Cyan" }
    elseif ($Line -match '^\[(run_commands|read_files|write_to_file|search_codebase|list_files|replace_in_file|execute_command|attempt_completion|submit_and_exit|use_mcp_tool|list_code_definition_names|search_files|new_rule|read_file|edited_existing_file|new_file_created|plan_mode_respond)\]') { $color = "Yellow" }
    elseif ($Line -match '^\s*⎿|error:|ERROR|FAILED') { $color = "Red" }
    elseif ($Line -match '^\[.*\]\[WAITING\]') { $color = "DarkGray" }

    return [pscustomobject]@{ Message = $Line; Color = $color }
}

function Write-ClineLiveLine {
    param(
        [AllowEmptyString()]
        [string]$Line,
        [Parameter(Mandatory)][string]$LogFile
    )
    if ($null -eq $Line) { return }
    if ($Line.Length -eq 0) {
        Write-Host ""
        Add-Content -Path $LogFile -Value "" -Encoding UTF8 -ErrorAction SilentlyContinue
        return
    }
    $formatted = Format-ClineOutputLine -Line $Line
    Write-Host $formatted.Message -ForegroundColor $formatted.Color
    Add-Content -Path $LogFile -Value $Line -Encoding UTF8 -ErrorAction SilentlyContinue
}

function Invoke-ClineWithLiveFeed {
    param(
        [Parameter(Mandatory)][string]$Workspace,
        [Parameter(Mandatory)][string]$PromptFile,
        [int]$TimeoutSeconds = 1500,
        [string]$UseCaseName = "inject"
    )

    $childScript = Get-ClineChildScriptPath

    $logDir = Join-Path $Workspace ".agents\logs"
    New-Item -ItemType Directory -Force -Path $logDir | Out-Null
    $logFile = Join-Path $logDir ("cline-{0}-{1}.log" -f ($UseCaseName -replace '[^\w\-]', '-'), (Get-Date -Format "yyyyMMdd-HHmmss"))

    Write-Host "[INJECT] cline live feed — log file: $logFile" -ForegroundColor Cyan
    Add-Content -Path $logFile -Value ("=== cline inject {0} {1} ===" -f $UseCaseName, (Get-Date -Format "yyyy-MM-dd HH:mm:ss")) -Encoding UTF8

    $pwsh = (Get-Command pwsh -ErrorAction SilentlyContinue).Source
    if (-not $pwsh) { $pwsh = (Get-Command powershell -ErrorAction Stop).Source }

    $psi = New-Object System.Diagnostics.ProcessStartInfo
    $psi.FileName = $pwsh
    $psi.WorkingDirectory = $Workspace
    $psi.UseShellExecute = $false
    $psi.RedirectStandardOutput = $true
    $psi.RedirectStandardError = $true
    $psi.CreateNoWindow = $true
    $psi.ArgumentList.Add("-NoProfile")
    $psi.ArgumentList.Add("-File")
    $psi.ArgumentList.Add($childScript)
    $psi.ArgumentList.Add("-Workspace")
    $psi.ArgumentList.Add($Workspace)
    $psi.ArgumentList.Add("-PromptFile")
    $psi.ArgumentList.Add($PromptFile)
    $psi.ArgumentList.Add("-TimeoutSeconds")
    $psi.ArgumentList.Add("$TimeoutSeconds")

    $proc = New-Object System.Diagnostics.Process
    $proc.StartInfo = $psi
    [void]$proc.Start()

    $stdout = $proc.StandardOutput
    $stderr = $proc.StandardError
    $lastActivity = [DateTime]::UtcNow

    while ($true) {
        $hadOutput = $false

        while ($stdout.Peek() -ge 0) {
            $line = $stdout.ReadLine()
            if ($null -eq $line) { break }
            try {
                Write-ClineLiveLine -Line $line -LogFile $logFile
            } catch {
                Write-Host "[WARN] cline stdout line skipped: $($_.Exception.Message)" -ForegroundColor DarkYellow
            }
            $lastActivity = [DateTime]::UtcNow
            $hadOutput = $true
        }

        while ($stderr.Peek() -ge 0) {
            $line = $stderr.ReadLine()
            if ($null -eq $line) { break }
            try {
                Write-ClineLiveLine -Line $line -LogFile $logFile
            } catch {
                Write-Host "[WARN] cline stderr line skipped: $($_.Exception.Message)" -ForegroundColor DarkYellow
            }
            $lastActivity = [DateTime]::UtcNow
            $hadOutput = $true
        }

        if ($proc.HasExited -and $stdout.Peek() -lt 0 -and $stderr.Peek() -lt 0) {
            break
        }

        if (-not $hadOutput) {
            $silence = ([DateTime]::UtcNow - $lastActivity).TotalSeconds
            if ($silence -gt 25) {
                $beat = "[$(Get-Date -Format 'HH:mm:ss')][WAITING] cline still running ({0}s quiet) — tail: {1}" -f [int]$silence, $logFile
                Write-ClineLiveLine -Line $beat -LogFile $logFile
                $lastActivity = [DateTime]::UtcNow
            }
            Start-Sleep -Milliseconds 250
        }
    }

    if (-not $proc.HasExited) {
        try { $proc.WaitForExit() } catch {}
    }

    $exitCode = $proc.ExitCode
    if ($null -eq $exitCode) { $exitCode = 1 }
    Add-Content -Path $logFile -Value ("=== exit code {0} {1} ===" -f $exitCode, (Get-Date -Format "yyyy-MM-dd HH:mm:ss")) -Encoding UTF8
    Write-Host "[INJECT] cline finished exit=$exitCode — full log: $logFile" -ForegroundColor $(if ($exitCode -eq 0) { "Green" } else { "Yellow" })
    return $exitCode
}

function Invoke-ClineInject {
    param(
        [Parameter(Mandatory)][string]$Workspace,
        [Parameter(Mandatory)][string]$Prompt,
        [Parameter(Mandatory)][string]$PromptFile,
        [int]$TimeoutSeconds = 1500,
        [string]$UseCaseName = "inject"
    )

    return (Invoke-ClineWithLiveFeed -Workspace $Workspace -PromptFile $PromptFile -TimeoutSeconds $TimeoutSeconds -UseCaseName $UseCaseName)
}

function Invoke-AgentInject {
    param(
        [Parameter(Mandatory)][string]$Agent,
        [Parameter(Mandatory)][string]$Workspace,
        [Parameter(Mandatory)][string]$PromptFile,
        [int]$TimeoutSeconds = 1500,
        [string]$UseCaseName = "inject"
    )

    if (-not (Test-Path $PromptFile)) {
        throw "Prompt file not found: $PromptFile"
    }

    $prompt = Get-Content $PromptFile -Raw -Encoding UTF8
    $normalized = $Agent.ToLowerInvariant()

    if ($normalized -eq "agy") {
        return (Invoke-AgyWithLiveFeed -Workspace $Workspace -Prompt $prompt -TimeoutSeconds $TimeoutSeconds)
    }

    if ($normalized -eq "cline") {
        return (Invoke-ClineInject -Workspace $Workspace -Prompt $prompt -PromptFile $PromptFile -TimeoutSeconds $TimeoutSeconds -UseCaseName $UseCaseName)
    }

    $cmd = Get-AgentInjectCommand -Agent $Agent -Workspace $Workspace -PromptFile $PromptFile -TimeoutSeconds $TimeoutSeconds

    if ($cmd.UseStdin) {
        $prompt | & $cmd.Executable @($cmd.ArgumentList)
        if ($null -eq $LASTEXITCODE) { return 0 }
        return $LASTEXITCODE
    }

    throw "Unsupported agent invoke path for '$Agent'"
}

function Test-HandoffExists {
    param(
        [Parameter(Mandatory)][string]$Workspace,
        [Parameter(Mandatory)][string]$AgentsDir,
        [Parameter(Mandatory)][string]$UseCaseName
    )
    $path = Join-Path $Workspace "$AgentsDir\state\handoff-reports\$UseCaseName.md"
    return (Test-Path $path)
}
