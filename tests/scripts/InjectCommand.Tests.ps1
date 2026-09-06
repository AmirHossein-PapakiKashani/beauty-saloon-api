# Unit tests for headless Inject command construction (Pester 3.x compatible)
# Run: Invoke-Pester -Script @{ Path = 'tests/scripts/InjectCommand.Tests.ps1' }

$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here "..\..")
. (Join-Path $root "scripts\BackendCycle.Inject.ps1")

Describe "Get-AgentInjectCommand" {
    $workspace = "E:\barber\beauty-saloon-api"
    $promptFile = "$workspace\.agents\state\current-prompt.txt"

    It "builds cline command with cwd and timeout" {
        $cmd = Get-AgentInjectCommand -Agent "cline" -Workspace $workspace -PromptFile $promptFile -TimeoutSeconds 1500
        $cmd.Executable | Should Be "cline"
        ($cmd.ArgumentList -join " ") | Should Match "-c"
        ($cmd.ArgumentList -join " ") | Should Match "-t"
        ($cmd.ArgumentList -join " ") | Should Match "1500"
        $cmd.UseStdin | Should Be $false
        $cmd.PromptAsArg | Should Be $true
        $cmd.UseDirectInvoke | Should Be $true
        $cmd.LiveFeed | Should Be $true
    }

    It "cline must not use splat invoke (documented regression)" {
        $cmd = Get-AgentInjectCommand -Agent "cline" -Workspace $workspace -PromptFile $promptFile
        $cmd.UseDirectInvoke | Should Be $true
        ($cmd.ArgumentList -join " ") | Should Not Match "--"
    }

    It "builds opencode command that pipes prompt via stdin" {
        $cmd = Get-AgentInjectCommand -Agent "opencode" -Workspace $workspace -PromptFile $promptFile
        $cmd.Executable | Should Be "opencode"
        ($cmd.ArgumentList -join " ") | Should Match "run"
        ($cmd.ArgumentList -join " ") | Should Match "--auto"
        $cmd.UseStdin | Should Be $true
    }

    It "builds agy command with --print AFTER other flags" {
        $cmd = Get-AgentInjectCommand -Agent "agy" -Workspace $workspace -PromptFile $promptFile -TimeoutSeconds 900
        $cmd.Executable | Should Be "agy"
        $joined = $cmd.ArgumentList -join " "
        $joined | Should Match "--dangerously-skip-permissions"
        $joined | Should Match "--print"
        $printIdx = [array]::IndexOf($cmd.ArgumentList, "--print")
        $skipIdx = [array]::IndexOf($cmd.ArgumentList, "--dangerously-skip-permissions")
        ($printIdx -gt $skipIdx) | Should Be $true
        ($printIdx -eq ($cmd.ArgumentList.Count - 1)) | Should Be $true
        $cmd.PromptAsArg | Should Be $false
        $cmd.UseArgumentList | Should Be $true
        $joined | Should Match "900s"
    }

    It "agy argument string must NOT contain mission body" {
        $cmd = Get-AgentInjectCommand -Agent "agy" -Workspace $workspace -PromptFile $promptFile -TimeoutSeconds 60
        $joined = $cmd.ArgumentList -join " "
        $joined | Should Not Match "You are implementing"
        $joined | Should Not Match "GetActiveSalonServices"
    }

    It "rejects unknown agent" {
        $threw = $false
        try {
            Get-AgentInjectCommand -Agent "nope" -Workspace $workspace -PromptFile $promptFile | Out-Null
        } catch {
            $threw = $true
        }
        $threw | Should Be $true
    }
}

Describe "Get-ClineChildScriptPath" {
    It "resolves child script when dot-sourced (regression: MyCommand.Path null in functions)" {
        $child = Get-ClineChildScriptPath
        $child | Should Match "Invoke-ClineChild\.ps1$"
        (Test-Path $child) | Should Be $true
    }
}

Describe "Format-ClineOutputLine" {
    It "colors thinking lines cyan" {
        $f = Format-ClineOutputLine -Line "[thinking] Planning next step"
        $f.Color | Should Be "Cyan"
    }

    It "colors tool lines yellow" {
        $f = Format-ClineOutputLine -Line "[run_commands] dotnet test"
        $f.Color | Should Be "Yellow"
    }
}

Describe "Write-ClineLiveLine" {
    It "accepts empty lines without throwing" {
        $tmp = Join-Path $env:TEMP ("cline-live-test-{0}.log" -f [guid]::NewGuid())
        try {
            { Write-ClineLiveLine -Line "" -LogFile $tmp } | Should Not Throw
            { Write-ClineLiveLine -Line "hello" -LogFile $tmp } | Should Not Throw
        } finally {
            Remove-Item $tmp -Force -ErrorAction SilentlyContinue
        }
    }
}

Describe "Format-AgyTranscriptStep" {
    It "formats thinking steps" {
        $step = [pscustomobject]@{
            step_index = 2
            created_at = "2026-09-01T12:00:00Z"
            thinking   = "I should create SalonService entity next.`nMore"
        }
        $lines = Format-AgyTranscriptStep -Step $step
        $lines.Count | Should Be 1
        $lines[0].Message | Should Match "THINKING"
        $lines[0].Message | Should Match "SalonService"
    }

    It "formats tool/file actions" {
        $step = [pscustomobject]@{
            step_index = 3
            created_at = "2026-09-01T12:00:01Z"
            tool_calls = @(
                [pscustomobject]@{
                    name = "write_to_file"
                    args = [pscustomobject]@{ AbsolutePath = "E:\barber\beauty-saloon-api\src\Foo.cs" }
                }
            )
        }
        $lines = Format-AgyTranscriptStep -Step $step
        $lines.Count | Should Be 1
        $lines[0].Message | Should Match "ACTION"
        $lines[0].Message | Should Match "write_to_file"
        $lines[0].Message | Should Match "Foo.cs"
    }
}

Describe "Test-HandoffExists" {
    It "returns false when handoff missing" {
        $result = Test-HandoffExists -Workspace $root -AgentsDir ".agents" -UseCaseName "DoesNotExistUseCaseXYZ"
        $result | Should Be $false
    }

    It "returns true for GetAvailableDays handoff" {
        $result = Test-HandoffExists -Workspace $root -AgentsDir ".agents" -UseCaseName "GetAvailableDays"
        $result | Should Be $true
    }
}
