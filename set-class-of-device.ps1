#Requires -RunAsAdministrator
param([ValidateSet('speaker', 'computer')][string]$Mode = 'speaker')

$key = 'HKLM:\SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters'

if ($Mode -eq 'speaker') {
    New-ItemProperty -Path $key -Name 'COD Major' -PropertyType DWord -Value 0x04 -Force | Out-Null
    New-ItemProperty -Path $key -Name 'COD Type' -PropertyType DWord -Value 0x05 -Force | Out-Null
} else {
    Remove-ItemProperty -Path $key -Name 'COD Major', 'COD Type' -ErrorAction SilentlyContinue
}
Get-ItemProperty $key | Select-Object 'COD Major', 'COD Type' | Format-List

foreach ($radio in Get-PnpDevice -Class Bluetooth -PresentOnly | Where-Object InstanceId -like 'USB\*') {
    Write-Host "Restarting $($radio.FriendlyName)..."
    pnputil /restart-device $radio.InstanceId
}
Write-Host 'If a restart was refused (pending reboot), reboot Windows for the change to take effect.'
