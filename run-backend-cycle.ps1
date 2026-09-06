# run-backend-cycle.ps1
# Headless backend implementation loop for BarberSalon API
# One command runs: Select → Enrich → Inject → Verify → Close (updates queue.json)
param(
    [string]$Workspace       = "E:\barber\beauty-saloon-api",
    [string]$AgentsDir       = ".agents",
    [ValidateSet("cline", "opencode", "agy")]
    [string]$Agent           = "cline",
    [string]$Model           = "",
    [int]   $MaxRetries      = 3,
    [int]   $TimeoutSeconds  = 1500,
    [switch]$DryRun,
    [switch]$EnrichOnly,
    [switch]$Once
)

$ErrorActionPreference = "Continue"
. "$PSScriptRoot\scripts\BackendCycle.Inject.ps1"

$timestamp = Get-Date -Format "yyyyMMdd-HHmmss"
$logFile   = "$Workspace\$AgentsDir\logs\backend-cycle-$timestamp.log"

New-Item -ItemType Directory -Force -Path "$Workspace\$AgentsDir\logs" | Out-Null
New-Item -ItemType Directory -Force -Path "$Workspace\$AgentsDir\state\handoff-reports" | Out-Null

function Write-Log {
    param([string]$Msg, [string]$Level = "INFO")
    $line = "$(Get-Date -Format 'HH:mm:ss') [$Level] $Msg"
    Write-Host $line
    Add-Content -Path $logFile -Value $line
}

function Update-Queue {
    param([object]$Queue, [string]$Path)
    $Queue | ConvertTo-Json -Depth 10 | Set-Content $Path -Encoding UTF8
}

function Test-UseCaseDependenciesMet {
    param($UseCase, [string[]]$CompletedIds)
    foreach ($dep in $UseCase.dependencies) {
        if ($CompletedIds -notcontains $dep) { return $false }
    }
    return $true
}

function Reset-RetryableFailedUseCases {
    param([object]$Queue)
    $completedIds = @($Queue.use_cases | Where-Object { $_.status -eq "completed" } | Select-Object -ExpandProperty id)
    $resetIds = @()
    foreach ($uc in ($Queue.use_cases | Where-Object { $_.status -eq "failed" })) {
        if (Test-UseCaseDependenciesMet -UseCase $uc -CompletedIds $completedIds) {
            $uc.status = "pending"
            $uc.retries = 0
            $resetIds += $uc.id
        }
    }
    return $resetIds
}

function Sync-QueueMeta {
    param([object]$Queue)
    $cases = @($Queue.use_cases)
    $Queue.meta.total = $cases.Count
    $Queue.meta.completed = @($cases | Where-Object { $_.status -eq "completed" }).Count
    $Queue.meta.failed = @($cases | Where-Object { $_.status -eq "failed" }).Count
    $Queue.meta.in_progress = @($cases | Where-Object { $_.status -eq "in-progress" }).Count
}

function Set-UseCaseStatusInQueue {
    param(
        [object]$Queue,
        [string]$Id,
        [string]$Status,
        [int]$Retries = 0
    )
    foreach ($uc in @($Queue.use_cases)) {
        if ($uc.id -eq $Id) {
            $uc.status = $Status
            $uc.retries = $Retries
            return $true
        }
    }
    return $false
}

function Write-BlockedQueueDiagnostics {
    param([object]$Queue, [string[]]$CompletedIds)
    $failed = @($Queue.use_cases | Where-Object { $_.status -eq "failed" })
    if ($failed.Count -gt 0) {
        Write-Log "Failed (blockers): $($failed.name -join ', ')" "WARN"
    }
    foreach ($uc in ($Queue.use_cases | Where-Object { $_.status -eq "pending" } | Sort-Object priority)) {
        $missing = @($uc.dependencies | Where-Object { $CompletedIds -notcontains $_ })
        if ($missing.Count -gt 0) {
            Write-Log "  $($uc.name) blocked by incomplete deps: $($missing -join ', ')" "WARN"
        }
    }
}

function Run-DotnetTest {
    $result = dotnet test "$Workspace" --verbosity quiet 2>&1
    return @{ ExitCode = $LASTEXITCODE; Output = $result -join "`n" }
}

function Build-UseCasePrompt {
    param($Selected, [string]$Template, [string]$Extra = "")
    $prompt = $Template -replace '\{\{use-case-name\}\}', $Selected.name
    $prompt = @"
# Mission
$prompt

## Authority (overrides conflicting template lines)
- Follow ``AGENTS.md`` and ``CONTEXT.md`` in this repo. **Plain Services only — NO MediatR / CQRS.**
- Pattern reference: ``docs/examples/SalonService-complete.md`` (when present).
- Frontend contract: ``$($Selected.frontend_ref)`` in ``beauty-saloon-front-V2/src/lib/data/dataService.ts``.
- If ``{{...}}`` placeholders above are empty, derive domain terms, files, events, and scenarios from those sources. Do not invent MediatR.

## Mandatory handoff
When done, write ``.agents/state/handoff-reports/$($Selected.name).md`` summarizing contract, files, and tests.

$Extra
"@
    return $prompt
}

# --- Startup ---
Write-Log "=== BACKEND CYCLE START ==="
Write-Log "Workspace: $Workspace | Agent: $Agent"
Write-Log "DryRun: $DryRun | EnrichOnly: $EnrichOnly | Once: $Once | Mode: $(if ($Once) { 'single use case' } else { 'continuous until blocked/done' })"

$queuePath = "$Workspace\$AgentsDir\state\queue.json"
$promptPath = "$Workspace\$AgentsDir\state\current-prompt.txt"

if (-not (Test-Path $queuePath)) {
    Write-Log "ERROR: queue.json not found at $queuePath. Run bootstrap first." "ERROR"
    exit 1
}

$queue = Get-Content $queuePath -Raw | ConvertFrom-Json

# Resume interrupted runs by making abandoned work selectable again.
$interrupted = @($queue.use_cases | Where-Object { $_.status -eq "in-progress" })
foreach ($useCase in $interrupted) {
    $useCase.status = "pending"
}
$queue.meta.in_progress = 0
Update-Queue $queue $queuePath
Write-Log "RESUME_RESET: Reset $($interrupted.Count) in-progress use case(s) to pending"

$retryFailed = Reset-RetryableFailedUseCases -Queue $queue
if ($retryFailed.Count -gt 0) {
    $queue.meta.failed = @($queue.use_cases | Where-Object { $_.status -eq "failed" }).Count
    Update-Queue $queue $queuePath
    Write-Log "RETRY_FAILED: Reset to pending (deps met): $($retryFailed -join ', ')"
}

$pending = @($queue.use_cases | Where-Object { $_.status -eq "pending" })
$completed = @($queue.use_cases | Where-Object { $_.status -eq "completed" })
$failed    = @($queue.use_cases | Where-Object { $_.status -eq "failed" })

if ($pending.Count -eq 0 -and $failed.Count -eq 0) {
    Write-Log "LOOP_COMPLETE: All $($completed.Count) use cases implemented." "DONE"
    exit 0
}

Write-Log "RESUME: $($pending.Count) pending, $($completed.Count) completed, $($failed.Count) failed"

if ($pending.Count -eq 0 -and $failed.Count -gt 0) {
    $completedIds = @($completed | Select-Object -ExpandProperty id)
    Write-Log "BLOCKED: Pending queue empty but failed items remain (dependencies not met for retry)." "WARN"
    Write-BlockedQueueDiagnostics -Queue $queue -CompletedIds $completedIds
    exit 1
}

# --- Main Loop ---
:mainLoop while ($true) {
    $queue = Get-Content $queuePath -Raw | ConvertFrom-Json
    $completedIds = @($queue.use_cases | Where-Object { $_.status -eq "completed" } | Select-Object -ExpandProperty id)

    # Phase 1: Select
    $selected = $null
    foreach ($uc in ($queue.use_cases | Where-Object { $_.status -eq "pending" } | Sort-Object priority)) {
        $depsOk = $true
        foreach ($dep in $uc.dependencies) {
            if ($completedIds -notcontains $dep) { $depsOk = $false; break }
        }
        if ($depsOk) { $selected = $uc; break }
    }

    if ($null -eq $selected) {
        Write-Log "BLOCKED: No pending use case has all dependencies completed." "WARN"
        Write-Log "Completed: $($completedIds -join ', ')"
        Write-BlockedQueueDiagnostics -Queue $queue -CompletedIds $completedIds
        break mainLoop
    }

    Write-Log "--- SELECTED: $($selected.name) (id: $($selected.id)) ---"
    $selected.status = "in-progress"
    Update-Queue $queue $queuePath

    if ($DryRun) {
        Write-Log "DRY RUN: Would implement $($selected.name) via Agent=$Agent"
        $selected.status = "pending"
        Update-Queue $queue $queuePath
        break mainLoop
    }

    # Phase 2: Enrich
    $template = Get-Content "$Workspace\$AgentsDir\skills\prompt-engineer\base-template.md" -Raw -ErrorAction SilentlyContinue
    if (-not $template) {
        Write-Log "ERROR: base-template.md not found" "ERROR"
        exit 1
    }
    $prompt = Build-UseCasePrompt -Selected $selected -Template $template
    Set-Content -Path $promptPath -Value $prompt -Encoding UTF8
    Write-Log "ENRICHED: Prompt written ($($prompt.Split("`n").Count) lines)"

    if ($EnrichOnly) {
        Write-Log "ENRICH_ONLY: Prompt ready — run Inject manually."
        $selected.status = "pending"
        Update-Queue $queue $queuePath
        break mainLoop
    }

    # Phase 3+4: Inject → Verify (re-inject on failure)
    $retries = 0
    $success = $false
    while ($retries -lt $MaxRetries -and -not $success) {
        $retries++
        Write-Log "INJECT: attempt $retries/$MaxRetries via $Agent for $($selected.name)"
        try {
            $injectCode = Invoke-AgentInject -Agent $Agent -Workspace $Workspace -PromptFile $promptPath -TimeoutSeconds $TimeoutSeconds -UseCaseName $selected.name
            Write-Log "INJECT: agent exit code = $injectCode"
        } catch {
            Write-Log "INJECT: exception: $($_.Exception.Message)" "ERROR"
            $injectCode = 1
        }

        if ($null -eq $injectCode) { $injectCode = 0 }

        if ($injectCode -ne 0) {
            Write-Log "INJECT: non-zero exit — skipping Verify success path; will retry" "WARN"
            $fixPrompt = Build-UseCasePrompt -Selected $selected -Template $template -Extra @"
## Fix required
Previous agent invoke failed (exit code $injectCode). Re-read the mission and implement this use case fully.
Write handoff to ``.agents/state/handoff-reports/$($selected.name).md`` when done.
"@
            Set-Content -Path $promptPath -Value $fixPrompt -Encoding UTF8
            continue
        }

        Write-Log "VERIFY: Attempt $retries of $MaxRetries"
        $testResult = Run-DotnetTest
        $handoffOk = Test-HandoffExists -Workspace $Workspace -AgentsDir $AgentsDir -UseCaseName $selected.name

        if ($testResult.ExitCode -eq 0 -and $handoffOk) {
            $success = $true
            Write-Log "VERIFY: GREEN + handoff present ✓"
        } elseif ($testResult.ExitCode -eq 0 -and -not $handoffOk) {
            Write-Log "VERIFY: tests green but handoff missing — will re-inject" "WARN"
            $fixPrompt = Build-UseCasePrompt -Selected $selected -Template $template -Extra @"
## Fix required
Tests are green but handoff report is missing.
Write ``.agents/state/handoff-reports/$($selected.name).md`` now. Do not invent new features.
"@
            Set-Content -Path $promptPath -Value $fixPrompt -Encoding UTF8
        } else {
            Write-Log "VERIFY: RED — tests failed (retry $retries)" "WARN"
            $snippet = if ($testResult.Output.Length -gt 4000) { $testResult.Output.Substring(0, 4000) } else { $testResult.Output }
            Write-Log $snippet
            $fixPrompt = Build-UseCasePrompt -Selected $selected -Template $template -Extra @"
## Fix required
``dotnet test`` failed. Fix ONLY this use case until green.
### Test output (truncated)
``````
$snippet
``````
Then update handoff ``.agents/state/handoff-reports/$($selected.name).md``.
"@
            Set-Content -Path $promptPath -Value $fixPrompt -Encoding UTF8
        }
    }

    # Phase 5: Close
    if ($success) {
        if (-not (Set-UseCaseStatusInQueue -Queue $queue -Id $selected.id -Status "completed" -Retries $retries)) {
            Write-Log "WARN: could not find $($selected.id) in queue to mark completed — patching selected object" "WARN"
            $selected.status = "completed"
            $selected.retries = $retries
        }
        Sync-QueueMeta -Queue $queue
        Update-Queue $queue $queuePath

        $progressPath = "$Workspace\$AgentsDir\state\progress.md"
        $total = $queue.meta.total
        $done  = $queue.meta.completed
        $pct   = [math]::Round(($done / $total) * 100)
        $progressContent = "# Implementation Progress`n`n**Last updated:** $(Get-Date -Format 'yyyy-MM-dd HH:mm')`n**Overall:** $done / $total use cases ($pct%)`n**Last closed:** $($selected.name)`n"
        Set-Content $progressPath $progressContent -Encoding UTF8

        Write-Log "CLOSED: $($selected.name) → completed ($done/$total = $pct%)"
    } else {
        Set-UseCaseStatusInQueue -Queue $queue -Id $selected.id -Status "failed" -Retries $MaxRetries | Out-Null
        Sync-QueueMeta -Queue $queue
        Update-Queue $queue $queuePath
        Write-Log "FAILED: $($selected.name) after $MaxRetries retries — stopping loop" "ERROR"
        exit 1
    }

    if ($Once) {
        Write-Log "ONCE: Stopping after one use case."
        break mainLoop
    }

    $queue = Get-Content $queuePath -Raw | ConvertFrom-Json
    $remaining = @($queue.use_cases | Where-Object { $_.status -eq "pending" })
    if ($remaining.Count -eq 0) {
        Write-Log "LOOP_COMPLETE: All $($queue.meta.completed) use cases done." "DONE"
        break mainLoop
    }

    $next = @($remaining | Sort-Object priority | Select-Object -First 1).name
    Write-Log "LOOP_CONTINUE: $($remaining.Count) pending — next up: $next"
    Write-Log "========== NEXT USE CASE (continuous loop) =========="
}

Write-Log "=== BACKEND CYCLE END ==="
