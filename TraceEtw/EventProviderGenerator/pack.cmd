@echo off
setlocal

set VERSION=1.0.10

set OUTPUT=c:\NuGet\

%OUTPUT%nuget.exe pack "%~dp0\MMaitre.TraceEtw.nuspec" -OutputDirectory %OUTPUT%Packages -Prop NuGetVersion=%VERSION% -NoPackageAnalysis
