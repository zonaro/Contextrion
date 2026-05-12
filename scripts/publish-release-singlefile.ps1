param(
    [string]$RuntimeIdentifier = 'win-x64',
    [string]$Configuration = 'Release'
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

$repoRoot = Split-Path -Parent $PSScriptRoot
$projectPath = Join-Path $repoRoot 'Contextrion\Contextrion.csproj'
$publishRoot = Join-Path $repoRoot 'publish'

if (-not (Test-Path -LiteralPath $projectPath)) {
    throw "Projeto nao encontrado: $projectPath"
}

if (Test-Path -LiteralPath $publishRoot) {
    Remove-Item -LiteralPath $publishRoot -Recurse -Force
}

New-Item -ItemType Directory -Path $publishRoot -Force | Out-Null

$dotnetArgs = @(
    'publish'
    $projectPath
    '--configuration', $Configuration
    '--runtime', $RuntimeIdentifier
    '--self-contained', 'true'
    '/p:PublishSingleFile=true'
    '/p:IncludeAllContentForSelfExtract=true'
    '/p:EnableCompressionInSingleFile=true'
    '/p:DebugType=None'
    '/p:DebugSymbols=false'
    '--output', $publishRoot
)

Write-Host "Publicando single-file para $RuntimeIdentifier em $publishRoot"
& dotnet @dotnetArgs

if ($LASTEXITCODE -ne 0) {
    throw "dotnet publish falhou com codigo $LASTEXITCODE"
}

$publishFiles = @(Get-ChildItem -LiteralPath $publishRoot -File)
$exeFiles = @($publishFiles | Where-Object { $_.Extension -ieq '.exe' })
$nonExeFiles = @($publishFiles | Where-Object { $_.Extension -ine '.exe' })
$subdirectories = @(Get-ChildItem -LiteralPath $publishRoot -Directory)

if ($exeFiles.Count -ne 1 -or $nonExeFiles.Count -gt 0 -or $subdirectories.Count -gt 0) {
    $entries = Get-ChildItem -LiteralPath $publishRoot | Select-Object -ExpandProperty Name
    throw "O publish nao gerou um unico .exe. Conteudo atual: $($entries -join ', ')"
}

if ($exeFiles[0].Length -lt 50000000) {
    throw "O .exe gerado ficou pequeno demais para um pacote self-contained e provavelmente nao embutiu o runtime."
}

Write-Host "OK: exe unico gerado em $($exeFiles[0].FullName)"
