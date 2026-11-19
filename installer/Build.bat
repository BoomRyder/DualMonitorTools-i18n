"C:\Program Files (x86)\WiX Toolset v3.14\bin\Candle" DualMonitorTools.wks -ext WixNetFxExtension
"C:\Program Files (x86)\WiX Toolset v3.14\bin\Light" DualMonitorTools.wixobj -ext WixNetFxExtension -ext WixUIExtension
cscript TweakExitDialog.js DualMonitorTools.msi
