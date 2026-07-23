@echo off
set EXE=bin\Release\net8.0-windows\SpeechApp.exe

if not exist "%EXE%" (
    echo Chua thay file exe. Hay chay build.bat truoc.
    pause
    exit /b 1
)

start "" "%EXE%"
