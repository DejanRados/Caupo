@echo off
set PROJECT_PATH=D:\Caupo
set LOG_FILE=D:\Caupo\git-backup.log

cd /d "%PROJECT_PATH%"

echo ================================ >> "%LOG_FILE%"
echo Backup pokrenut %date% %time% >> "%LOG_FILE%"

rem Provjera svih promjena: modified, deleted i untracked
for /f %%i in ('git status --porcelain') do goto HAS_CHANGES

echo Nema promjena >> "%LOG_FILE%"
goto END

:HAS_CHANGES
git add -A >> "%LOG_FILE%" 2>&1

git commit -m "Auto backup %date% %time%" >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
    echo GRESKA PRI COMMITU >> "%LOG_FILE%"
    goto END
)

git push >> "%LOG_FILE%" 2>&1

if errorlevel 1 (
    echo GRESKA PRI PUSHU >> "%LOG_FILE%"
    goto END
)

echo Backup ZAVRSEN >> "%LOG_FILE%"

:END
pause