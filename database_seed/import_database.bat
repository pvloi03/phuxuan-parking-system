@echo off
chcp 65001 > nul
echo =========================================================================
echo  KHỞI TẠO DỮ LIỆU CSDL MONGODB: PhuXuanParkingSystemDb
echo =========================================================================
echo.

where mongosh >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Đang thực thi nạp dữ liệu bằng mongosh...
    mongosh "mongodb://localhost:27017/PhuXuanParkingSystemDb" init_database.js
    goto DONE
)

where mongo >nul 2>&1
if %ERRORLEVEL% equ 0 (
    echo Đang thực thi nạp dữ liệu bằng mongo shell cũ...
    mongo "mongodb://localhost:27017/PhuXuanParkingSystemDb" init_database.js
    goto DONE
)

echo [LƯU Ý] Không tìm thấy lệnh mongosh hoặc mongo trong PATH hệ thống.
echo Bạn có thể:
echo   1. Mở MongoDB Compass, import trực tiếp file JSON.
echo   2. Hoặc mở MongoDB Compass -> mở MongoSH Tab ở góc dưới và paste nội dung file init_database.js.
echo.

:DONE
echo.
echo Hoàn tất! Nhấn phím bất kỳ để thoát.
pause > nul
