@echo off
echo Starting Zlinks Package System Backend...
cd /d "%~dp0\backend"

echo Checking Java version...
java -version

echo Building project with Maven...
call mvnw.cmd clean package -DskipTests

echo Starting application...
java -jar target\package-system-1.0.0.jar

pause