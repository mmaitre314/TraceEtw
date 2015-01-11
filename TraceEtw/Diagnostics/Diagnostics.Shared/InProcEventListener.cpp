#include "pch.h"
#include "EventTraceProperties.h"
#include "InProcEventListener.h"

using namespace Diagnostics;
using namespace Platform;
using namespace Windows::Foundation::Collections;
using namespace Windows::Storage;

InProcEventListener::InProcEventListener(
    _In_ IStorageFolder^ folder,
    _In_ Platform::String^ filename,
    _In_ IIterable<Guid>^ providers
    )
    : _sessionHandle(0)
    , _traceProperties(folder, filename)
{
    CHKNULL(folder);
    CHKNULL(providers);

    _path = ref new String(_traceProperties.LogFileName);
    CHKWIN32(StartTraceW(&_sessionHandle, _traceProperties.LoggerName, &_traceProperties));
    
#if WINAPI_PARTITION_PC_APP // EnableTrace() not available on Phone
    for(Guid provider : providers)
    {
        GUID temp = provider;
        CHKWIN32(EnableTrace(true, /*all keywords*/ 0, TRACE_LEVEL_VERBOSE, &temp, _sessionHandle));
    }
#endif
}

InProcEventListener::~InProcEventListener()
{
    EventTraceProperties traceProperties(_traceProperties.Wnode.Guid);

    CHKWIN32(ControlTraceW(_sessionHandle, nullptr, &traceProperties, EVENT_TRACE_CONTROL_FLUSH));
    CHKWIN32(ControlTraceW(_sessionHandle, nullptr, &traceProperties, EVENT_TRACE_CONTROL_STOP));

    _sessionHandle = 0;
}
