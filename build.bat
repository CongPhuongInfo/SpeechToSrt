@echo off
setlocal

echo ============================================
echo   Build SpeechApp (VB.NET - System.Speech)
echo ============================================
echo.

where dotnet >nul 2>nul
if errorlevel 1 (
    echo [Loi] Khong tim thay .NET SDK.
    echo Cai dat tai: https://dotnet.microsoft.com/download
    echo Can ban .NET 8 SDK tro len.
    pause
    exit /b 1
)

echo Dang restore NuGet packages...
dotnet restore
if errorlevel 1 goto :error

echo.
echo Dang build (Release)...
dotnet build -c Release
if errorlevel 1 goto :error

echo.
echo ============================================
echo   Build thanh cong!
echo ============================================
echo File .exe nam trong: bin\Release\net8.0-windows\SpeechApp.exe
echo.
echo Cach chay:
echo   run.bat "duong_dan_file_am_thanh.wav"
echo   hoac: bin\Release\net8.0-windows\SpeechApp.exe "duong_dan_file.mp3"
echo.
pause
exit /b 0

:error
echo.
echo [Loi] Build that bai. Xem chi tiet loi o tren.
pause
exit /b 1
