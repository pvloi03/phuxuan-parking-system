# ==============================================================================
# SCRIPT GỠ BỎ DỊCH VỤ MÁY CHỦ: PHÚ XUÂN PARKING SYSTEM
# Dừng và gỡ bỏ Windows Service PhuXuanParkingApi
# ==============================================================================

[CmdletBinding()]
param (
    [string]$ServiceName = "PhuXuanParkingApi"
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================================" -ForegroundColor Yellow
Write-Host "         GO BO DICH VU WINDOWS: PHU XUAN PARKING SYSTEM          " -ForegroundColor Yellow
Write-Host "=================================================================" -ForegroundColor Yellow

$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Script nay bat buoc phai chay voi quyen Administrator!`nVui long click phai chuot vao PowerShell va chon 'Run as Administrator'."
    exit 1
}

$svc = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $svc) {
    Write-Host "Dich vu '$ServiceName' khong ton tai tren he thong." -ForegroundColor Green
    exit 0
}

Write-Host "Dang dung dich vu '$ServiceName'..." -ForegroundColor Yellow
if ($svc.Status -eq 'Running') {
    Stop-Service -Name $ServiceName -Force
    Write-Host "  -> Da dung dich vu." -ForegroundColor Green
}

Write-Host "Dang go bo dich vu '$ServiceName'..." -ForegroundColor Yellow
& sc.exe delete $ServiceName | Out-Null
Start-Sleep -Seconds 2

$checkAgain = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($null -eq $checkAgain) {
    Write-Host "Da go bo Windows Service '$ServiceName' thanh cong!" -ForegroundColor Green
} else {
    Write-Warning "Service van con tren he thong (co the can khoi dong lai hoac tat Services.msc)."
}

Write-Host "Luu y: Du lieu CSDL MongoDB va cac file anh trong Captures van duoc giu nguyen an toan." -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Yellow
