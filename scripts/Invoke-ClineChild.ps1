# Child worker: run cline with prompt file; stream every line to stdout for parent to tail.
param(
    [Parameter(Mandatory)][string]$Workspace,
    [Parameter(Mandatory)][string]$PromptFile,
    [int]$TimeoutSeconds = 1500
)

$ErrorActionPreference = "Continue"
$prompt = Get-Content $PromptFile -Raw -Encoding UTF8

& cline -c $Workspace -t "$TimeoutSeconds" -v $prompt 2>&1 | ForEach-Object {
    Write-Output ($_.ToString())
}

$code = $LASTEXITCODE
if ($null -eq $code) { $code = 0 }
exit $code
