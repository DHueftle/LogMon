call "%ProgramFiles%\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
call "%ProgramFiles(x86)%\Microsoft Visual Studio 11.0\VC\vcvarsall.bat"
set /p buildVersion=Build version: 
msbuild LogMon_Package.dproj /t:CompletePackage /p:MSIProductVersion=%buildVersion%
pause
