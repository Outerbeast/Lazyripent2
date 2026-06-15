@echo off
dotnet test Lazyripent2.Tests\Lazyripent2.Tests.csproj
if %errorlevel% equ 0 (
    echo Tests succeeded
) else (
    echo Tests failed
    exit /b 1
)

if not exist Release\ mkdir Release

if exist Release\lazyripent.7z del Release\lazyripent.7z
if exist Release\lazyripent del Release\lazyripent
if exist Release\lazyripent.exe del Release\lazyripent.exe

call :Build "Linux" "linux-x64"
call :Build "Windows" "win-x64"
echo SHIP IT!!
goto :eof

:Build
echo Building %~1
dotnet publish Lazyripent2\Lazyripent2.csproj -o Build\%~1 -c Release -r %~2 -v q
if %errorlevel% neq 0 (
    echo %~1 build failed
    exit /b 1
)
echo %~1 build succeeded
echo 7-zipping
if "%~1"=="Linux" (set fileExt=) else (set fileExt=.exe)
move Build\%~1\Lazyripent2%fileExt% Release\lazyripent%fileExt%
7z a Release\lazyripent.7z Release\lazyripent%fileExt% -y -bso0
7z rn Release\lazyripent.7z Release\lazyripent%fileExt% lazyripent%fileExt% -y -bso0
goto :eof