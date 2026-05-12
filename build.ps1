$csc = "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
$src = "src\launcher.cs"
$icon = "assets\cc_logo.ico"
$out = "Claude_Code_Launcher.exe"

Write-Host "Building Claude Code Launcher..." -ForegroundColor Cyan
& $csc /target:winexe /win32icon:$icon /out:$out $src

if ($LASTEXITCODE -eq 0) {
    $size = (Get-Item $out).Length
    Write-Host "OK  $out ($([math]::Round($size/1024, 1)) KB)" -ForegroundColor Green
} else {
    Write-Host "FAILED (exit $LASTEXITCODE)" -ForegroundColor Red
}
