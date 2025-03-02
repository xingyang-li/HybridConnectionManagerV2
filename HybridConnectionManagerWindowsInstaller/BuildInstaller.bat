@echo off
echo Building and publishing all projects...

rem Publish Service
dotnet publish ..\HybridConnectionManagerService\HybridConnectionManagerService.csproj -c Release -r win-x64 -p:PublishSingleFile=true
rem Publish GUI
dotnet publish ..\HybridConnectionManagerGUI\HybridConnectionManagerGUI.csproj -c Release -r win-x64 --self-contained false

rem Publish CLI
dotnet publish ..\HybridConnectionManagerCLI\HybridConnectionManagerCLI.csproj -c Release -r win-x64 -p:PublishSingleFile=true

rem Generate WiX component files
call GenerateServiceFiles.bat
call GenerateGUIFiles.bat
call GenerateCLIFiles.bat

rem Build WiX project
dotnet build -c Release

echo Installer build complete. MSI is located in bin\Release folder.