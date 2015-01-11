[doc in progress]

[![Build status](https://ci.appveyor.com/api/projects/status/d3w7r6o478u53o8d?svg=true)](https://ci.appveyor.com/project/mmaitre314/traceetw)
[![NuGet package](http://mmaitre314.github.io/images/nuget.png)](https://www.nuget.org/packages/MMaitre.TraceEtw/)

[Event Tracing for Windows (ETW)](http://msdn.microsoft.com/en-us/library/windows/desktop/aa363668(v=vs.85).aspx) is very powerful but notoriously complex. In C#, [EventSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx) made that technology much more approachable. This project aims at providing a similar solution for C++, both in Desktop apps and Windows/Windows Phone Universal Store apps. 

Defining events
---

Add an XML file with .epx extension (as in: Event Provider XML) to the Visual Studio project.

Say a file called EtwLogger.epx contains a basic marker event:

```xml
<?xml version="1.0" encoding="utf-8"?>
<EventProvider
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:noNamespaceSchemaLocation="http://mmaitre314.github.io/EventProvider.xsd"
    Name="MMaitre-TraceEtw" 
    Guid="{A006BF16-179A-4BDF-A0A2-917AC6CA98D6}"
    >

    <Event Name="Marker" />

</EventProvider>
```

A header called Events\EtwLogger.h gets generated during the build which lets the app send markers at appropriate times:

```C++
EtwLogger.Marker();
```

[WPA]

The xsi attributes are there to enable Intellisense in Visual Studio.

The XML file supports defining more complex events. For instance, an event can have arguments and a trace level:

```xml
<Event Name="Trace" Level="Verbose">
    <Arg Type="Pointer" Name="Object" />
    <Arg Type="UInt32" Name="Count" />
</Event>
```

```C++
EtwLogger.Trace(this, 314);
```

The default trace level is Informational.

An event can also have variable number of arguments, which become unstructured traces:

```xml
<Event Name="Trace">
    <VarArgs Type="AnsiString" Name="Message" />
</Event>
```

```C++
EtwLogger.Trace("Received %i calls from %s", 3, "localhost");
```

Beginning and end of operations can be tracked by defining tasks:

```xml
<Task Name="ALongOperation">
    <Start>
        <Arg Type="Pointer" Name="Object" />
    </Start>
    <Stop>
        <Arg Type="Pointer" Name="Object" />
        <Arg Type="Int32" Name="HResult" />
    </Stop>
</Task>
```

```C++
EtwLogger.ALongOperationStart(this);
...
EtwLogger.ALongOperationStop(this, S_OK);
```

The `IsEnabled()` method on the logger class allows traces which may require expensive computations to only run when events are being recorded:

```C++

if (EtwLogger.IsEnabled())
{
    string message = GatherTraceInformation()
    EtwLogger.Trace(L"Trace information: %s", message.c_str());
}

```

Recording and displaying events
---

[scripts wevtutil, xperf]
[WPA]

In-app event recording
---

The project also contains an API for apps to record events fired inside their own process. This is currently not included in the NuGet package as it works in Windows apps but not in Windows Store apps ([EnableTrace()](http://msdn.microsoft.com/en-us/library/windows/desktop/aa363710(v=vs.85).aspx) function banned there).

Recording events is just a matter of creating an `InProcEventListener` object, passing a file path and the list of event provider GUIDs to enable:

```C++
auto listener = ref new InProcEventListener(
    ApplicationData::Current->LocalFolder,
    L"log.etl",
    ref new Vector<Guid> { MMaitre_TraceEtw }
);

EtwLogger.Trace("1");
```

