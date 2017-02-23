echo off
echo Precompiling SpineStub...
rmdir /Q /S "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\PrecompiledWeb\Fujitsu.SamlBridge.Web.SpineStub"
C:\WINNT\Microsoft.NET\Framework\v2.0.50727\aspnet_compiler -v /Spine -p "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\Fujitsu.SamlBridge.Web.SpineStub" "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\PrecompiledWeb\Fujitsu.SamlBridge.Web.SpineStub"
"D:\SamlBridge Program\Development\Fujitsu.SamlBridge\Tools\bin\AssemblyManager" -b "D:\SamlBridge Program\Development\Fujitsu.SamlBridge\Fujitsu.SamlBridge.Web.SpineStub\Properties\AssemblyInfo.cs
