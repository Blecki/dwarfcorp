SETLOCAL
ECHO OFF

echo "Building for windows MONO"
DEL /Q "build\win32MONOGAME"
MD "build\win32MONOGAME
XCOPY "DwarfCorp\bin\mono\Release\." "build\win32MONOGAME" /Y /E /V /D || Goto :ERR

echo "All targets built."
goto :EOF

:ERR
echo "!!!Failure!!!"

:EOF
pause

