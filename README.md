[![Build status](https://ci.appveyor.com/api/projects/status/d3w7r6o478u53o8d?svg=true)![Test status](http://teststatusbadge.azurewebsites.net/api/status/mmaitre314/traceetw)](https://ci.appveyor.com/project/mmaitre314/traceetw)
[![NuGet package](http://mmaitre314.github.io/images/nuget.png)](https://www.nuget.org/packages/MMaitre.TraceEtw/)

[Event Tracing for Windows (ETW)](http://msdn.microsoft.com/en-us/library/windows/desktop/aa363668(v=vs.85).aspx) is  powerful but notoriously complex. In C#, [EventSource](http://msdn.microsoft.com/en-us/library/system.diagnostics.tracing.eventsource(v=vs.110).aspx) made that technology much more approachable. This project aims at providing a similar solution for C++, both for Desktop apps and for Windows/Windows Phone Universal Store apps. 

Defining events
---

To begin with, add an XML file with an .epx extension (as in 'Event Provider XML') to the Visual Studio project:

```xml
<?xml version="1.0" encoding="utf-8"?>
<EventProvider
    xmlns:xsi="http://www.w3.org/2001/XMLSchema-instance"
    xsi:noNamespaceSchemaLocation="http://mmaitre314.github.io/EventProvider.xsd"
    Name="MMaitre-TraceEtw" 
    >

</EventProvider>
```

where the `Name` attribute should be replaced with some appropriate value.

A basic marker event with no payload can be added using:

```xml
<Event Name="Marker" />
```

When the project is compiled a header gets generated from the XML file, which lets the app raise the marker event when appropriate:

```C++
#include "Events\EtwLogger.h"

EtwLogger.Marker();
```

where EtwLogger is the name of the .epx file.

[Windows Performance Analyzer (WPA)](http://msdn.microsoft.com/en-us/library/windows/hardware/hh448170.aspx) will start displaying the event:

![WPA](http://mmaitre314.github.io/images/TraceEtwWpa.PNG)

The XML file allows defining more complex events. Events can have arguments and a trace level:

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

Events can also have a variable number of arguments, which generates unstructured traces (i.e. `printf`):

```xml
<Event Name="Trace">
    <VarArgs Type="AnsiString" Name="Message" />
</Event>
```

```C++
EtwLogger.Trace("Received %i calls from %s", 3, "localhost");
```

Defining task events allows tracking the beginning and end of long operations:

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

Besides the logger header, the build also generates a set of scripts, a WPRP profile, and an event-provider manifest:

- RegisterEtwLogger.cmd/UnregisterEtwLogger.cmd - scripts to register and unregister the event provider. Must be run elevated.
- RecordEtwLogger.cmd - script to record events.
- EtwLogger.wprp - [recording profile](http://msdn.microsoft.com/en-us/library/windows/hardware/hh448223.aspx) for [Windows Performance Recorder (WPR)](http://msdn.microsoft.com/en-us/library/windows/hardware/hh448205.aspx)
- EtwLogger.man - event-provider manifest

The files are placed in the output folder along with the binaries. Registering the manifest is optional when using WPR.

The trace script relies on xperf to record events. It is part of the [Windows Performance Toolkit](http://msdn.microsoft.com/en-us/library/windows/hardware/hh162945.aspx) like WPA and WPR. Its ability to run a merge pass on the .etl event log files tends to make it more robust when it comes to collecting event-manifest info.

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

