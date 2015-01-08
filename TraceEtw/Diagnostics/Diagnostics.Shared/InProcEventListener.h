#pragma once

namespace Diagnostics
{
    public ref class InProcEventListener sealed
    {
    public:

        InProcEventListener(
            _In_ Windows::Storage::IStorageFolder^ folder,
            _In_ Windows::Foundation::Collections::IIterable<Platform::Guid>^ providers
            );

        // IClosable
        virtual ~InProcEventListener();

        property Platform::String^ Path { Platform::String^ get() { return _path; } }

    private:

        TRACEHANDLE _sessionHandle;
        EventTraceProperties _traceProperties;
        Platform::String^ _path;
    };
}
