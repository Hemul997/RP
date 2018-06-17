@echo off
if %~1 == "" goto err

start /wait /d Frontend dotnet publish -o ..\..\Builds\%~1\Frontend
start /wait /d Backend dotnet publish -o ..\..\Builds\%~1\Backend
start /wait /d TextRankCalc dotnet publish -o ..\..\Builds\%~1\TextRankCalc
start /wait /d TextListener dotnet publish -o ..\..\Builds\%~1\TextListener
start /wait /d VowelConsCounter dotnet publish -o ..\..\Builds\%~1\VowelConsCounter
start /wait /d VowelConsRater dotnet publish -o ..\..\Builds\%~1\VowelConsRater
start /wait /d TextStatistics dotnet publish -o ..\..\Builds\%~1\TextStatistics
start /wait /d TextProcessingLimiter dotnet publish -o ..\..\Builds\%~1\TextProcessingLimiter
start /wait /d TextSuccessMarker dotnet publish -o ..\..\Builds\%~1\TextSuccessMarker

start /wait xcopy config ..\Builds\%~1\config /I
start /wait xcopy run.cmd ..\Builds\%~1
start /wait xcopy stop.cmd ..\Builds\%~1

echo "Compilation and build were successfully completed"
pause
exit 0

:err
echo "Arguments was not found"
exit 1