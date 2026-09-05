# ==============================================================================
# SCRIPT ĐÓNG GÓI MÁY CHỦ MẠNG LAN: PHÚ XUÂN PARKING SYSTEM
# Tự động build Web Admin (Vite), nhúng vào wwwroot của Web API và publish self-contained win-x64
# ==============================================================================

[CmdletBinding()]
param (
    [string]$OutputDir = ""
)

$ErrorActionPreference = "Stop"

# 1. Xác định đường dẫn gốc của repository
$RepoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
if ([string]::IsNullOrWhiteSpace($OutputDir)) {
    $OutputDir = Join-Path $RepoRoot "publish\server"
}

Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "    PHU XUAN PARKING SYSTEM - LAN SERVER PUBLISH AUTOMATION     " -ForegroundColor Cyan
Write-Host "=================================================================" -ForegroundColor Cyan
Write-Host "Repository Root : $RepoRoot"
Write-Host "Output Directory: $OutputDir"
Write-Host ""

# 2. Kiểm tra các công cụ bắt buộc
Write-Host "[1/4] Kiem tra moi truong he thong..." -ForegroundColor Yellow
if (-not (Get-Command "node" -ErrorAction SilentlyContinue)) {
    Write-Error "Khong tim thay Node.js. Vui long cai dat Node.js de tiep tuc build Web Admin."
}
if (-not (Get-Command "dotnet" -ErrorAction SilentlyContinue)) {
    Write-Error "Khong tim thay .NET SDK. Vui long cai dat .NET 8 SDK."
}

$nodeVer = node -v
$dotnetVer = dotnet --version
Write-Host "  -> Node.js: $nodeVer" -ForegroundColor Green
Write-Host "  -> .NET SDK: $dotnetVer" -ForegroundColor Green

# 3. Build Web Admin (Vite SPA)
Write-Host "`n[2/4] Build Web Admin Vite SPA..." -ForegroundColor Yellow
$WebDir = Join-Path $RepoRoot "PhuXuanParkingSystem.Web"
$ApiDir = Join-Path $RepoRoot "PhuXuanParkingSystem.Api"
$WwwRootDir = Join-Path $ApiDir "wwwroot"

Push-Location $WebDir
try {
    Write-Host "  -> Dang chay 'npm run build'..." -ForegroundColor Gray
    npm run build
    if ($LASTEXITCODE -ne 0) {
        throw "Loi khi build Web Admin."
    }
}
finally {
    Pop-Location
}

# 4. Sao chép bản build Vite (dist) vào wwwroot của Web API
Write-Host "`n[3/4] Dong bo dist vao Api/wwwroot..." -ForegroundColor Yellow
$WebDistDir = Join-Path $WebDir "dist"
if (-not (Test-Path $WebDistDir)) {
    Write-Error "Khong tim thay thu muc dist tai: $WebDistDir"
}

if (-not (Test-Path $WwwRootDir)) {
    New-Item -ItemType Directory -Path $WwwRootDir -Force | Out-Null
} else {
    # Dọn dẹp wwwroot cũ
    Get-ChildItem -Path $WwwRootDir -Recurse | Remove-Item -Recurse -Force -ErrorAction SilentlyContinue
}

Copy-Item -Path "$WebDistDir\*" -Destination $WwwRootDir -Recurse -Force
Write-Host "  -> Da sao chep toan bo ban build Web Admin vao: $WwwRootDir" -ForegroundColor Green

# 5. Publish ASP.NET Core Web API (Self-contained win-x64)
Write-Host "`n[4/4] Publish ASP.NET Core API (Self-contained win-x64)..." -ForegroundColor Yellow
if (-not (Test-Path $OutputDir)) {
    New-Item -ItemType Directory -Path $OutputDir -Force | Out-Null
}

$ApiProj = Join-Path $ApiDir "PhuXuanParkingSystem.Api.csproj"
dotnet publish $ApiProj `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=false `
    -o $OutputDir

if ($LASTEXITCODE -ne 0) {
    Write-Error "Loi trong qua trinh dotnet publish API."
}

# Sao chép các script cài đặt vào thư mục release để kỹ thuật viên mang đi triển khai
$ScriptsReleaseDir = Join-Path $OutputDir "scripts"
if (-not (Test-Path $ScriptsReleaseDir)) {
    New-Item -ItemType Directory -Path $ScriptsReleaseDir -Force | Out-Null
}
Copy-Item -Path (Join-Path $PSScriptRoot "setup-lan-server.ps1") -Destination $ScriptsReleaseDir -Force -ErrorAction SilentlyContinue
Copy-Item -Path (Join-Path $PSScriptRoot "uninstall-lan-server.ps1") -Destination $ScriptsReleaseDir -Force -ErrorAction SilentlyContinue

Write-Host "`n=================================================================" -ForegroundColor Green
Write-Host "               DONG GOI THANH CONG HOAN TOAN!                   " -ForegroundColor Green
Write-Host "=================================================================" -ForegroundColor Green
Write-Host "Goi phat hanh may chu: $OutputDir"
Write-Host "File thuc thi chinh  : $OutputDir\PhuXuanParkingSystem.Api.exe"
Write-Host "Giao dien Web Admin  : $OutputDir\wwwroot\index.html"
Write-Host "Cac buoc tiep theo de trien khai tren may chu:"
Write-Host "  1. Copy toan bo thu muc '$OutputDir' sang may chu (Vi du: D:\PhuXuanServer)."
Write-Host "  2. Mo PowerShell (Run as Administrator) tai thu muc scripts."
Write-Host "  3. Chay lenh: .\setup-lan-server.ps1"
Write-Host "=================================================================" -ForegroundColor Green
