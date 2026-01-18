@echo off
set PROJECT_PATH=D:\Caupo
set LOG_FILE=D:\Caupo\git-backup.log

cd /d %PROJECT_PATH%

echo ================================ >> "%LOG_FILE%"
echo Backup pokrenut %date% %time% >> "%LOG_FILE%"

rem Provjera promjena direktno bez temp fajla
git diff --quiet --exit-code

if errorlevel 1 (
    git add . >> "%LOG_FILE%" 2>&1
    git commit -m "Auto backup %date% %time%" >> "%LOG_FILE%" 2>&1
    git push >> "%LOG_FILE%" 2>&1
    echo Backup ZAVRSEN >> "%LOG_FILE%"
) else (
    echo Nema promjena >> "%LOG_FILE%"
)

pause
