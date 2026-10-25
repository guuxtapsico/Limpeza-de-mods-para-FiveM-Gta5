$ErrorActionPreference = "Stop"

$projectRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$src = Join-Path $projectRoot "src\GXCleanerGui.cs"
$assets = Join-Path $projectRoot "assets"
$icon = Join-Path $assets "GXCleaner.ico"
$dist = Join-Path $projectRoot "dist"
$out = Join-Path $dist "GX Limpeza.exe"

# Imagens embutidas no .exe (arquivo, nome usado no codigo)
$images = @(
    @("fivem.png", "GXFiveM"),
    @("gtav.jpg", "GXGta"),
    @("gx-logo.png", "GXLogo")
)

New-Item -ItemType Directory -Force -Path $dist | Out-Null

$csc = Join-Path $env:WINDIR "Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if (-not (Test-Path -LiteralPath $csc)) {
    $csc = Join-Path $env:WINDIR "Microsoft.NET\Framework\v4.0.30319\csc.exe"
}

if (-not (Test-Path -LiteralPath $csc)) {
    throw "csc.exe do .NET Framework nao encontrado."
}

$resources = @("/resource:$icon,GXCleanerIcon")
foreach ($image in $images) {
    $path = Join-Path $assets $image[0]
    if (-not (Test-Path -LiteralPath $path)) { throw "Imagem nao encontrada: $path" }
    $resources += "/resource:$path,$($image[1])"
}

& $csc `
    /nologo `
    /codepage:65001 `
    /target:winexe `
    /platform:anycpu `
    /win32icon:"$icon" `
    /out:"$out" `
    /reference:System.Windows.Forms.dll `
    /reference:System.Drawing.dll `
    /reference:Microsoft.CSharp.dll `
    $resources `
    "$src"

if ($LASTEXITCODE -ne 0) {
    throw "A compilacao falhou."
}

Write-Host "Build concluido: $out" -ForegroundColor Green
