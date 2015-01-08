#include "stdafx.h"

using namespace Microsoft::VisualStudio::CppUnitTestFramework;

TEST_CLASS(UnitTest1)
{
public:
        
    TEST_METHOD(CX_Desktop_TestBasic)
    {
        if (EtwLogger.IsEnabled())
        {
            Logger::WriteMessage("EtwLogger enabled");
        }

        EtwLogger.Trace("");
        EtwLogger.Error("");
        EtwLogger.OpStart(nullptr);
        EtwLogger.OpStop(nullptr, E_FAIL);
        EtwLogger.TraceVarArgsAnsi(""); // TODO: test varargs events
        EtwLogger.TraceVarArgsUnicode(L"");
        EtwLogger.Marker();
        EtwLogger.MarkerOpStart();
        EtwLogger.MarkerOpStop();
        EtwLogger.MarkerOp2Start();
        EtwLogger.MarkerOp2Stop(&GUID_NULL);
        EtwLogger.ManyArgs(true, 0, 0, 0, 0, 0, 0, 0, 0, 0.f, 0., &GUID_NULL, nullptr, "", L"");
    }

};
