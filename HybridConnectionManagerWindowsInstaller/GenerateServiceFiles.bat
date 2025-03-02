@echo off
set OUTPUT_DIR=..\HybridConnectionManagerService\bin\Release\net8.0\win-x64\publish
set HEAT_TOOL="C:\Program Files\WiX Toolset v5.0\bin\x64\heat.exe"

%HEAT_TOOL% dir %OUTPUT_DIR% -dr ServiceDir -cg ServiceFiles -gg -scom -sreg -sfrag -srd -ke -var var.ServicePublishDir -arch x64 -out ServiceFiles.wxs

echo Generated ServiceFiles.wxs