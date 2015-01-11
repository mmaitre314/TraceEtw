#pragma once

namespace Diagnostics
{
    public ref class InProcEventListener sealed
    {
    public:

        InProcEventListener(
            _In_ Windows::Storage::IStorageFolder^ folder,
            _In_ Platform::String^ filename,
            _In_ Windows::Foundation::Collections::IIterable<Platform::Guid>^ providers
            );

        // IClosable
        virtual ~InProcEventListener();

        Windows::Foundation::IAsyncOperation<Windows::Storage::StorageFile^>^ GetLogFileAsync()
        {
            return Windows::Storage::StorageFile::GetFileFromPathAsync(_path);
        }

    private:

        TRACEHANDLE _sessionHandle;
        EventTraceProperties _traceProperties;
        Platform::String^ _path;
    };
}
