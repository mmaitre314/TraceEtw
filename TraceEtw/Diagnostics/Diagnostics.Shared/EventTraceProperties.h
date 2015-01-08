#pragma once

class EventTraceProperties : public EVENT_TRACE_PROPERTIES
{
public:

    EventTraceProperties(_In_ const GUID& sessionGuid);
    EventTraceProperties(_In_ Windows::Storage::IStorageFolder^ folder);

    WCHAR LogFileName[1024];
    WCHAR LoggerName[1024];

protected:

    EventTraceProperties();
};
