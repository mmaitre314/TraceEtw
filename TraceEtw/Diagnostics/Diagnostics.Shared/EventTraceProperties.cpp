#include "pch.h"
#include "EventTraceProperties.h"

using namespace Windows::Storage;

EventTraceProperties::EventTraceProperties()
{
    ::ZeroMemory(this, sizeof(*this));

    Wnode.BufferSize = sizeof(*this);
    Wnode.Flags = WNODE_FLAG_TRACED_GUID;
    Wnode.ClientContext = 1; // use QPC for timestamps

    // Ask for max 128KB * 500 = 64MB of RAM to store events
    BufferSize = 128;
    MaximumBuffers = 500;

    FlushTimer = 3;

    LogFileMode =
        EVENT_TRACE_FILE_MODE_SEQUENTIAL |
        EVENT_TRACE_PRIVATE_LOGGER_MODE |
        EVENT_TRACE_PRIVATE_IN_PROC |
        EVENT_TRACE_NO_PER_PROCESSOR_BUFFERING;

    LoggerNameOffset = offsetof(EventTraceProperties, LoggerName);
    LogFileNameOffset = offsetof(EventTraceProperties, LogFileName);
}

EventTraceProperties::EventTraceProperties(__in REFGUID sessionGuid)
    : EventTraceProperties()
{
    Wnode.Guid = sessionGuid;
}

EventTraceProperties::EventTraceProperties(__in IStorageFolder^ folder)
    : EventTraceProperties()
{
    // Use a GUID as session name
    CHK(CoCreateGuid(&Wnode.Guid));
    CHK(StringFromGUID2(Wnode.Guid, LoggerName, ARRAYSIZE(LoggerName)));
    CHK(StringCchPrintf(LogFileName, ARRAYSIZE(LogFileName), L"%s\\%s.etl", folder->Path->Data(), LoggerName));
}
