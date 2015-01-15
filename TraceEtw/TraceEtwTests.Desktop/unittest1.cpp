#include "stdafx.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(UnitTest1)
{
public:
        
    TEST_METHOD(CPP_Desktop_TestBasic)
    {
        const GUID providerGuid = { 0xd7bcc40a, 0x0866, 0x52c3, { 0xaa, 0xde, 0xb3, 0xc8, 0xd3, 0x2f, 0xd3, 0x8e } };

        Assert::IsTrue(__uuidof(EtwLogger) == providerGuid);

        if (EtwLogger.IsEnabled())
        {
            Logger::WriteMessage("EtwLogger enabled");
        }
        else
        {
            Logger::WriteMessage("EtwLogger disabled");
        }

        EtwLogger.Trace("");
        EtwLogger.Error("");
        EtwLogger.OpStart(nullptr);
        EtwLogger.OpStop(nullptr, E_FAIL);
        EtwLogger.TraceVarArgsAnsi("An Ansi message with value: %i", 0);
        EtwLogger.TraceVarArgsUnicode(L"A Unicode message with value: %i", 1);
        EtwLogger.Marker2();
        EtwLogger.MarkerOpStart();
        EtwLogger.MarkerOpStop();
        EtwLogger.MarkerOp2Start();
        EtwLogger.MarkerOp2Stop(&GUID_NULL);
        EtwLogger.ManyArgs(true, 0, 0, 0, 0, 0, 0, 0, 0, 0.f, 0., &GUID_NULL, nullptr, "Ansi string", L"Unicode string");
    }

};
