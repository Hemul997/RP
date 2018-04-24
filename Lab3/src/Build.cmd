@echo off
if %~1 == "" goto err

start /wait /d Frontend dotnet publish -o ..\..\Builds\%~1\Frontend
start /wait /d Backend dotnet publish -o ..\..\Builds\%~1\Backend
start /wait /d TextListener dotnet publish -o ..\..\Builds\%~1\TextListener

start /wait xcopy config ..\Builds\%~1\config /I
start /wait xcopy run.cmd ..\Builds\%~1
start /wait xcopy stop.cmd ..\Builds\%~1

echo "Compilation and build were successfully completed"
pause
exit 0

:err
echo "Arguments was not found"
exit 1