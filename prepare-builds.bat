SETLOCAL
ECHO OFF

echo "Building for windows XNA"
DEL /Q "build\win32XNA"
MD "build\win32XNA"
XCOPY "DwarfCorp\bin\x86\Release\." "build\win32XNA" /Y /E /V /D || Goto :ERR

echo "Building for OSX"
DEL /Q "build\osx\DwarfCorp.app\Contents\MacOS"
MD "build\osx\DwarfCorp.app\Contents\MacOS"
XCOPY "Build_Metadata\osx\*" "build\osx" /Y /E /V || Goto :ERR
XCOPY "FNA_libs\osx\*" "build\osx\DwarfCorp.app\Contents\MacOS" /Y /E /V || Goto :ERR
XCOPY "FNA_libs\mono\*" "build\osx\DwarfCorp.app\Contents\MacOS" /Y /E /V || Goto :ERR
XCOPY "DwarfCorp\bin\FNA\Release\." "build\osx\DwarfCorp.app\Contents\MacOS" /Y /E /V || Goto :ERR

echo "Building for linux 32"
DEL /Q "build\linux32"
MD "build\linux32"
XCOPY "FNA_libs\lib\*" "build\linux32" /Y /E /V /D || Goto :ERR
XCOPY "FNA_libs\mono\*" "build\linux32" /Y /E /V /D || Goto :ERR
XCOPY "DwarfCorp\bin\FNA\Release\." "build\linux32" /Y /E /V /D || Goto :ERR

echo "Building for linux 64"
DEL /Q "build\linux64"
MD "build\linux64"
XCOPY "FNA_libs\lib64\*" "build\linux64" /Y /E /V /D || Goto :ERR
XCOPY "FNA_libs\mono\*" "build\linux64" /Y /E /V /D || Goto :ERR
XCOPY "DwarfCorp\bin\FNA\Release\." "build\linux64" /Y /E /V /D || Goto :ERR

echo "Building for windows FNA"
DEL /Q "build\win32FNA"
MD "build\win32FNA"
XCOPY "DwarfCorp\bin\FNA\Release\." "build\win32FNA" /Y /E /V /D || Goto :ERR

echo "All targets built."
goto :EOF

:ERR
echo "!!!Failure!!!"

:EOF
pause

