# ==============================================================================
# SCRIPT CÀI ĐẶT MÁY CHỦ MẠNG LAN: PHÚ XUÂN PARKING SYSTEM
# Mở Firewall, cấu hình Biến Môi Trường và cài đặt Windows Service
# ==============================================================================

[CmdletBinding()]
param (
    [string]$InstallDir = "",
    [string]$CapturesDir = "",
    [string]$ShareName = "Captures",
    [int]$Port = 80,
    [string]$ServiceName = "PhuXuanParkingApi",
    [string]$DisplayName = "Phu Xuan Parking System Web API & Admin",
    [string]$MongoServiceName = "MongoDB",
    [string]$MongoDbConnection = "mongodb://127.0.0.1:27017",
    [string]$DatabaseName = "PhuXuanParkingSystemDb",
    [string]$JwtSecret = ""
)

$ErrorActionPreference = "Stop"

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "    PHU XUAN PARKING SYSTEM - AUTOMATED SERVER PROVISIONING     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan

# 1. Kiem tra quyen Administrator
$isAdmin = ([Security.Principal.WindowsPrincipal][Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole]::Administrator)
if (-not $isAdmin) {
    Write-Error "Script nay bat buoc phai chay voi quyen Administrator!`nVui long click phai chuot vao PowerShell va chon 'Run as Administrator'."
    exit 1
}

# 2. Xac dinh thu muc cai dat (InstallDir)
if ([string]::IsNullOrWhiteSpace($InstallDir)) {
    # Thu muc hien tai hoac thu muc cha (neu chay tu scripts\)
    if (Test-Path (Join-Path $PSScriptRoot "PhuXuanParkingSystem.Api.exe")) {
        $InstallDir = $PSScriptRoot
    } elseif (Test-Path (Join-Path (Split-Path $PSScriptRoot -Parent) "PhuXuanParkingSystem.Api.exe")) {
        $InstallDir = Split-Path $PSScriptRoot -Parent
    } elseif (Test-Path (Join-Path (Resolve-Path (Join-Path $PSScriptRoot "..\publish\server")) "PhuXuanParkingSystem.Api.exe")) {
        $InstallDir = (Resolve-Path (Join-Path $PSScriptRoot "..\publish\server")).Path
    } else {
        $InstallDir = (Get-Location).Path
    }
}

$ExePath = Join-Path $InstallDir "PhuXuanParkingSystem.Api.exe"
if (-not (Test-Path $ExePath)) {
    Write-Error "Khong tim thay file thuc thi PhuXuanParkingSystem.Api.exe tai: $InstallDir`nVui long chi dinh tham so -InstallDir chi dung thu muc chua goi ung dung."
    exit 1
}

Write-Host "Thu muc ung dung  : $InstallDir" -ForegroundColor Green
Write-Host "File thuc thi chinh: $ExePath" -ForegroundColor Green

# 3. Bat buoc nguoi dung phai tu nhap duong dan thu muc anh Captures (FolderPath)
Write-Host "`n[1/5] Thiet lap duong dan thu muc anh Captures..." -ForegroundColor Yellow

while ([string]::IsNullOrWhiteSpace($CapturesDir)) {
    Write-Host "  -> YEU CAU: Bat buoc phai tu nhap duong dan thu muc luu anh Captures tren may chu!" -ForegroundColor Cyan
    $inputDir = Read-Host "Nhap duong dan thu muc anh (vi du: D:\Captures, E:\Captures, C:\PhuXuanData\Captures)"
    if (-not [string]::IsNullOrWhiteSpace($inputDir)) {
        $CapturesDir = $inputDir.Trim('"', "'", " ")
    } else {
        Write-Warning "Duong dan thu muc anh bat buoc phai nhap, khong duoc de trong!"
    }
}

if (Test-Path $CapturesDir) {
    Write-Host "  -> Thu muc luu anh vat ly: $CapturesDir (Da ton tai tren o dia)" -ForegroundColor Green
} else {
    Write-Host "  -> Thu muc luu anh vat ly: $CapturesDir (Chua ton tai tren o dia)" -ForegroundColor Yellow
    Write-Warning "Luu y che do thu cong: Ban can tu tao thu muc '$CapturesDir' tren o dia may chu truoc khi may bot luu anh!"
}

# Kiem tra trang thai SMB Share (neu da duoc tao thu cong)
$existingShare = Get-SmbShare -Name $ShareName -ErrorAction SilentlyContinue
if ($existingShare) {
    Write-Host "  -> SMB Share '$ShareName': da ton tai (Tro toi: $($existingShare.Path))" -ForegroundColor Green
} else {
    Write-Host "  -> SMB Share '$ShareName': chua tao (Vui long share thu cong neu dung qua mang LAN)" -ForegroundColor Gray
}

# 4. Kiem tra & Xu ly xung dot cong mang (Kiem tra truoc khi mo tuong lua)
Write-Host "`n[2/5] Kiem tra trang thai cong mang..." -ForegroundColor Yellow
$portResolved = $false

while (-not $portResolved) {
    $portInUse = Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue
    if ($portInUse) {
        $ownerProc = Get-Process -Id $portInUse[0].OwningProcess -ErrorAction SilentlyContinue
        $procDesc = if ($ownerProc) { "$($ownerProc.ProcessName) (PID: $($portInUse[0].OwningProcess))" } else { "PID: $($portInUse[0].OwningProcess)" }
        
        Write-Host ""
        Write-Warning "CANH BAO XUNG DOT: Cong $Port hien dang bi chiem boi tien trinh: $procDesc"
        if ($ownerProc -and $ownerProc.ProcessName -match "System|svchost|w3wp") {
            Write-Host "  (Goi y: Dich vu IIS hoac World Wide Web Publishing Service (W3SVC) co the dang chiem cong 80)." -ForegroundColor Yellow
            Write-Host "  (Neu khong dung IIS, ban co the tat bang: Stop-Service W3SVC -Force; Set-Service W3SVC -StartupType Disabled)" -ForegroundColor Gray
        }
        
        Write-Host "`nLua chon xu ly:" -ForegroundColor Cyan
        Write-Host "  [1] Nhap cong mang khac (vi du: 8080, 5000, 8000, 8888...)"
        Write-Host "  [2] Kiem tra lai voi cong $Port (sau khi ban da tat tien trinh chiem cong)"
        $choice = Read-Host "Nhap lua chon [1/2] (Mac dinh: 1)"
        
        if ($choice -eq "2") {
            Start-Sleep -Milliseconds 500
            continue
        } else {
            $newPortInput = Read-Host "Nhap so cong moi ban muon su dung (vi du: 8080)"
            $parsedPort = 0
            if ([int]::TryParse($newPortInput, [ref]$parsedPort) -and $parsedPort -gt 0 -and $parsedPort -le 65535) {
                $Port = $parsedPort
                Write-Host "  -> Chuyen sang kiem tra cong moi: $Port" -ForegroundColor Cyan
            } else {
                Write-Warning "So cong khong hop le! Vui long nhap so nguyen tu 1 den 65535."
            }
        }
    } else {
        Write-Host "  -> Cong $Port dang san sang va hop le de su dung!" -ForegroundColor Green
        $portResolved = $true
    }
}

# 5. Cau hinh Tuong lua Windows Firewall (Ap dung cho cong $Port da chot)
Write-Host "`n[3/5] Cau hinh Tuong lua Windows Firewall..." -ForegroundColor Yellow

$firewallRules = @(
    @{ Name = "PhuXuan_Web_API_Admin"; Port = $Port; Protocol = "TCP"; Desc = "Cong Web Admin va REST API Phu Xuan" },
    @{ Name = "PhuXuan_MongoDB"; Port = 27017; Protocol = "TCP"; Desc = "Cong CSDL MongoDB Server" },
    @{ Name = "PhuXuan_SMB_Share"; Port = 445; Protocol = "TCP"; Desc = "Cong chia se file Windows SMB (Captures)" }
)

foreach ($r in $firewallRules) {
    $existing = Get-NetFirewallRule -DisplayName $r.Name -ErrorAction SilentlyContinue
    if ($null -eq $existing) {
        New-NetFirewallRule -DisplayName $r.Name -Description $r.Desc -Direction Inbound -LocalPort $r.Port -Protocol $r.Protocol -Action Allow | Out-Null
        Write-Host "  -> Da mo cong $($r.Port)/$($r.Protocol) ($($r.Name))" -ForegroundColor Green
    } else {
        # Neu da ton tai, cap nhat lai port neu co thay doi
        Set-NetFirewallRule -DisplayName $r.Name -LocalPort $r.Port -Protocol $r.Protocol | Out-Null
        Write-Host "  -> Luat tuong lua '$($r.Name)' da cap nhat tro toi cong $($r.Port)" -ForegroundColor Gray
    }
}

# 6. Cai dat / Cap nhat Windows Service
Write-Host "`n[4/5] Dang ky Windows Service '$ServiceName'..." -ForegroundColor Yellow

# Dung service cu neu dang chay
$existingService = Get-Service -Name $ServiceName -ErrorAction SilentlyContinue
if ($existingService) {
    Write-Host "  -> Phat hien service '$ServiceName' dang ton tai. Dang dung va go bo de cai lai..." -ForegroundColor Yellow
    if ($existingService.Status -eq 'Running') {
        Stop-Service -Name $ServiceName -Force
    }
    # Go bo service cu
    & sc.exe delete $ServiceName | Out-Null
    Start-Sleep -Seconds 2
}

# Thiet lap Bien Moi Truong cap he thong (Machine-level Environment Variables)
Write-Host "  -> Thiet lap Bien Moi Truong he thong (Nap tu dong vao builder.Configuration)..." -ForegroundColor Yellow
[Environment]::SetEnvironmentVariable("ConnectionStrings__MongoDb", $MongoDbConnection, "Machine")
[Environment]::SetEnvironmentVariable("DatabaseName", $DatabaseName, "Machine")
if (-not [string]::IsNullOrWhiteSpace($CapturesDir)) {
    [Environment]::SetEnvironmentVariable("CapturesSettings__FolderPath", $CapturesDir, "Machine")
    Write-Host "     + CapturesSettings__FolderPath: $CapturesDir" -ForegroundColor Gray
}
if (-not [string]::IsNullOrWhiteSpace($JwtSecret)) {
    [Environment]::SetEnvironmentVariable("JwtSettings__SecretKey", $JwtSecret, "Machine")
    Write-Host "     + Da nap JwtSettings__SecretKey vao Bien Moi Truong." -ForegroundColor Green
}
Write-Host "     + ConnectionStrings__MongoDb : $MongoDbConnection" -ForegroundColor Gray
Write-Host "     + DatabaseName               : $DatabaseName" -ForegroundColor Gray

# Kiem tra xem MongoDB co dang cai dat lam Service khong
$mongoSvc = Get-Service -Name $MongoServiceName -ErrorAction SilentlyContinue
$dependParam = ""
if ($mongoSvc) {
    $dependParam = "depend= $MongoServiceName"
    Write-Host "  -> Tim thay dich vu MongoDB. Thiet lap phu thuoc: $dependParam" -ForegroundColor Green
} else {
    Write-Host "  -> Khong tim thay dich vu Windows '$MongoServiceName'. Khong bat buoc phu thuoc." -ForegroundColor Gray
}

# Dang ky Service bang sc.exe
$binPath = "`"$ExePath`" --urls http://0.0.0.0:$Port"
if (-not [string]::IsNullOrWhiteSpace($CapturesDir)) {
    $binPath += " --CapturesSettings:FolderPath `"$CapturesDir`""
}
$scArgs = @("create", $ServiceName, "binPath=", $binPath, "start=", "auto", "DisplayName=", $DisplayName)
if (-not [string]::IsNullOrWhiteSpace($dependParam)) {
    $scArgs += "depend="
    $scArgs += $MongoServiceName
}

& sc.exe @scArgs | Out-Null

# Cau hinh mo ta dich vu
& sc.exe description $ServiceName "Dich vu cong quan tri Web Admin va REST API cho He thong bai xe Phu Xuan." | Out-Null

# Cau hinh tu dong phuc hoi (Restart on failure sau 10 giay)
& sc.exe failure $ServiceName reset= 86400 actions= restart/10000/restart/10000/restart/10000 | Out-Null

Write-Host "  -> Da dang ky Windows Service '$ServiceName' thanh cong!" -ForegroundColor Green

# 7. Khoi dong Service & Kiem tra
Write-Host "`n[5/5] Khoi dong dich vu va kiem tra ket noi..." -ForegroundColor Yellow
Start-Service -Name $ServiceName
Start-Sleep -Seconds 3

$checkService = Get-Service -Name $ServiceName
Write-Host "  -> Trang thai dich vu: $($checkService.Status)" -ForegroundColor Green

# Tim dia chi IP LAN
$lanIps = (Get-NetIPAddress -AddressFamily IPv4 -ErrorAction SilentlyContinue | Where-Object { 
    $_.IPAddress -notlike "127.*" -and $_.IPAddress -notlike "169.254.*" 
}).IPAddress

$primaryLanIp = if ($lanIps) { $lanIps[0] } else { "192.168.1.254" }

# Thu nghiem truy cap Localhost
try {
    $response = Invoke-WebRequest -Uri "http://localhost:$Port" -UseBasicParsing -TimeoutSec 5 -ErrorAction Stop
    Write-Host "  -> Kiem tra truy cap HTTP localhost:$Port thanh cong (Ma HTTP: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Warning "Khong the truy cap http://localhost:$Port ngay: $($_.Exception.Message)"
    Write-Warning "Vui long kiem tra file log tai: $InstallDir\logs\ de biet them chi tiet."
}

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host "          CAI DAT VA KHOI DONG HE THONG THANH CONG!              " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "Link truy cap Web Admin trong mang LAN:"
foreach ($ip in $lanIps) {
    if ($Port -eq 80) {
        Write-Host "  👉 http://$ip" -ForegroundColor Cyan
    } else {
        Write-Host "  👉 http://$ip`:$Port" -ForegroundColor Cyan
    }
}
Write-Host "Thu muc luu anh Captures tren may chu: $CapturesDir"
Write-Host "Duong dan mang SMB cho may bot       : \\$primaryLanIp\$ShareName"
Write-Host "File nhat ky he thong (Logs)         : $InstallDir\logs\"
Write-Host "=================================================================" -ForegroundColor Green
