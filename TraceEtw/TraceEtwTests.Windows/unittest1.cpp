#include "pch.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;
using namespace Diagnostics;
using namespace Platform;
using namespace Platform::Collections;
using namespace Windows::Storage;

TEST_CLASS(UnitTest1)
{
public:

    TEST_METHOD(CX_Windows_TestBasic)
    {
        StorageFile^ file;

        {
            auto listener = ref new InProcEventListener(
                ApplicationData::Current->LocalFolder,
                L"CX_Windows_TestBasic.etl",
                ref new Vector < Guid > { MMaitre_TraceEtw }
            );

            file = Await(listener->GetLogFileAsync());

            Logger::WriteMessage(file->Path->Data());
            Logger::WriteMessage("\n");

            Assert::IsTrue(EtwLogger.IsEnabled());

            EtwLogger.Trace("1");
            EtwLogger.Error("2");
            EtwLogger.OpStart(nullptr);
            EtwLogger.OpStop(nullptr, E_FAIL);
            EtwLogger.TraceVarArgsAnsi("Value: %i", 0);
            EtwLogger.TraceVarArgsUnicode(L"Record: %s", L"foo");
            EtwLogger.Marker();
            EtwLogger.MarkerOpStart();
            EtwLogger.MarkerOpStop();
            EtwLogger.MarkerOp2Start();
            EtwLogger.MarkerOp2Stop(&GUID_NULL);
            EtwLogger.ManyArgs(true, 0, 0, 0, 0, 0, 0, 0, 0, 0.f, 0., &GUID_NULL, nullptr, "", L"");
        }

        auto props = Await(file->GetBasicPropertiesAsync());
        Assert::IsTrue(props->Size > 0);

        auto folder = Await(KnownFolders::SavedPictures->CreateFolderAsync("Tests", CreationCollisionOption::OpenIfExists));
        Await(file->MoveAsync(folder, "CX_Windows_TestBasic.etl", NameCollisionOption::ReplaceExisting));

        Logger::WriteMessage(file->Path->Data());
    }

};
