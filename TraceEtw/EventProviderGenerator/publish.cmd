@echo off
setlocal

set VERSION=1.0.24

set OUTPUT=c:\NuGet\

%OUTPUT%nuget.exe push %OUTPUT%Packages\MMaitre.TraceEtw.%VERSION%.nupkg