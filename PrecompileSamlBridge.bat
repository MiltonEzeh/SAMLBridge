echo off
echo Precompiling SamlBridge...
rmdir /Q /S "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\PrecompiledWeb\Fujitsu.SamlBridge.Web"
C:\WINNT\Microsoft.NET\Framework\v2.0.50727\aspnet_compiler -v /SamlBridge -p "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\Fujitsu.SamlBridge.Web" "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\PrecompiledWeb\Fujitsu.SamlBridge.Web"
"D:\SamlBridge Program\Development\Fujitsu.SamlBridge\Tools\bin\AssemblyManager" -b "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\Fujitsu.SamlBridge.Web\Properties\AssemblyInfo.cs
