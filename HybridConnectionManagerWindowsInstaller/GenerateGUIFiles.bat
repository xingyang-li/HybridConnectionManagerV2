@echo off
set OUTPUT_DIR=..\HybridConnectionManagerGUI\bin\Desktop\win-unpacked
set HEAT_TOOL="C:\Program Files\WiX Toolset v5.0\bin\x64\heat.exe"

%HEAT_TOOL% dir %OUTPUT_DIR% -dr GUIDir -cg GUIFiles -gg -scom -sreg -sfrag -srd -ke -var var.GUIPublishDir -arch x64 -out GUIFiles.wxs

echo Generated GUIFiles.wxs