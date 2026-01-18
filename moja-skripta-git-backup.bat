@echo off
set PROJECT_PATH=D:\Caupo
set LOG_FILE=D:\Caupo\git-backup.log

cd /d %PROJECT_PATH%

echo ================================ >> "%LOG_FILE%"
echo Backup pokrenut %date% %time% >> "%LOG_FILE%"

git status --porcelain > temp_git_status.txt

for %%A in (temp_git_status.txt) do set SIZE=%%~zA

if %SIZE% GTR 0 (
    echo Promjene pronadjene >> "%LOG_FILE%"
    git add . >> "%LOG_FILE%" 2>&1
    git commit -m "Auto backup %date% %time%" >> "%LOG_FILE%" 2>&1
    git push >> "%LOG_FILE%" 2>&1
    echo Backup ZAVRSEN >> "%LOG_FILE%"
) else (
    echo Nema promjena >> "%LOG_FILE%"
)

del temp_git_status.txt

echo Backup gotov.
pause
