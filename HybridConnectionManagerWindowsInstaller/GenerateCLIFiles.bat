@echo off
set OUTPUT_DIR=..\HybridConnectionManagerCLI\bin\Release\net8.0\win-x64\publish
set HEAT_TOOL="C:\Program Files\WiX Toolset v5.0\bin\x64\heat.exe"

%HEAT_TOOL% dir %OUTPUT_DIR% -dr CLIDir -cg CLIFiles -gg -scom -sreg -sfrag -srd -ke -var var.CLIPublishDir -arch x64 -out CLIFiles.wxs

echo Generated CLIFiles.wxs