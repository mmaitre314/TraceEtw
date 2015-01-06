REM Generate the C# XML-deserialization code of EventProvider.cs

REM set PATH=%PATH%;C:\Program Files (x86)\Microsoft SDKs\Windows\v8.1A\bin\NETFX 4.5.1 Tools

xsd.exe EventProvider.xsd /classes /namespace:EventProviderGenerator