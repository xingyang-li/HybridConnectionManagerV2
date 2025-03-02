@echo off
set OUTPUT_DIR=..\HybridConnectionManagerGUI\bin\Release\net8.0\win-x64\publish
set HEAT_TOOL="C:\Program Files\WiX Toolset v5.0\bin\x64\heat.exe"

%HEAT_TOOL% dir %OUTPUT_DIR% -dr GUIDir -cg GUIFiles -gg -scom -sreg -sfrag -srd -ke -var var.GUIPublishDir -out GUIFiles.wxs

echo Generated GUIFiles.wxs