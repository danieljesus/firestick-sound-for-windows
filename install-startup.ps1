param(
    [string]$Filter = 'fire',
    [switch]$Uninstall
)

$dest = Join-Path $env:LOCALAPPDATA 'Programs\firestick-sound'
$exe = Join-Path $dest 'firestick-sound.exe'
$log = Join-Path $dest 'firestick-sound.log'
$run = 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run'

Get-Process firestick-sound -ErrorAction SilentlyContinue | Stop-Process -Force

if ($Uninstall) {
    Remove-ItemProperty -Path $run -Name 'FireStickSound' -ErrorAction SilentlyContinue
    Remove-Item $dest -Recurse -Force -ErrorAction SilentlyContinue
    Write-Host 'Removed.'
    return
}

dotnet publish (Join-Path $PSScriptRoot 'firestick-sound.csproj') -c Release -o $dest -p:OutputType=WinExe -v q -nologo
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }

$cmd = "`"$exe`" $Filter --log `"$log`""
Set-ItemProperty -Path $run -Name 'FireStickSound' -Value $cmd
Start-Process $exe -ArgumentList $Filter, '--log', "`"$log`""
Write-Host "Installed to $dest, starts hidden at logon. Log: $log"
