@echo off
setlocal
cd /d "%~dp0"
if not exist "Builds\Windows\Chroma Strike.exe" goto missing
if not exist "Builds\Windows\UnityPlayer.dll" goto missing
if not exist "Builds\Windows\Chroma Strike_Data\globalgamemanagers" goto missing
pushd "Builds\Windows"
start "" "Chroma Strike.exe" -logFile "%TEMP%\ChromaStrike-Player.log"
popd
exit /b 0
:missing
echo Game files are missing. Extract the ENTIRE ZIP first, then run PLAY.cmd.
echo Keep Builds\Windows intact, including the Data folder and UnityPlayer.dll.
pause
exit /b 1
