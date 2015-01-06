@echo off
setlocal

set VERSION=1.0.0

set OUTPUT=c:\NuGet\

%OUTPUT%nuget.exe push %OUTPUT%Packages\MMaitre.TraceEtw.%VERSION%.nupkg