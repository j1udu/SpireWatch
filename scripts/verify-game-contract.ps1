param(
    [string]$Sts2DataDir = $env:STS2_DATA_DIR,
    [string]$DotnetExecutable = 'dotnet'
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($Sts2DataDir)) {
    $repoRoot = Split-Path -Parent $PSScriptRoot
    [xml]$project = Get-Content -Raw (Join-Path $repoRoot 'local.props')
    $Sts2DataDir = $project.Project.PropertyGroup.Sts2DataDir
}

$dataDirectory = (Resolve-Path -LiteralPath $Sts2DataDir).Path
$checkerProject = Join-Path (Split-Path -Parent $PSScriptRoot) 'tools\GameContractCheck\GameContractCheck.csproj'
& $DotnetExecutable run --project $checkerProject --configuration Release -- $dataDirectory
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}
