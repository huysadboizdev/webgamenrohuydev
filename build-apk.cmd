@echo off
setlocal
set "UNITY_EDITOR=C:\Program Files\Unity\Hub\Editor\2022.3.62f2\Editor\Unity.exe"
if not exist "%UNITY_EDITOR%" (
  echo Set UNITY_EDITOR to your Unity 2022.3.62f2 installation.
  exit /b 1
)
if not exist "%~dp0Logs" mkdir "%~dp0Logs"
"%UNITY_EDITOR%" -batchmode -nographics -quit -projectPath "%~dp0." -buildTarget Android -executeMethod ApkClientBuild.Run -logFile "%~dp0Logs\apk-build.log"
exit /b %errorlevel%
