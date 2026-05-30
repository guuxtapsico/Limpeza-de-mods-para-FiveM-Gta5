$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $projectRoot "src\GXCleanerGui.cs"
$icon = Join-Path $projectRoot "assets\GXCleaner.ico"
$dist = Join-Path $projectRoot "dist"
$out = Join-Path $dist "GX Limpeza.exe"

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path -LiteralPath $csc)) {
    throw "csc.exe do .NET Framework nao encontrado."
}

& $csc `
    /nologo `
    /target:winexe `
    /platform:anycpu `
    /win32icon:"$icon" `
    /out:"$out" `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:Microsoft.CSharp.dll `
    /resource:"$icon,GXCleanerIcon" `
    "$src"

Write-Host "Build concluido: $out" -ForegroundColor Green
