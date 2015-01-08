#pragma once

#include <collection.h>
#include <ppltasks.h>
#include <wrl.h>
#include <Evntrace.h>
#include <strsafe.h>

//
// Error handling
//

// Exception-based error handling
#define CHK(statement)  {HRESULT _hr = (statement); if (FAILED(_hr)) { throw ref new Platform::COMException(_hr); };}
#define CHKWIN32(statement) CHK(HRESULT_FROM_WIN32(statement))
#define CHKNULL(p)  {if ((p) == nullptr) { throw ref new Platform::NullReferenceException(L#p); };}
#define CHKOOM(p)  {if ((p) == nullptr) { throw ref new Platform::OutOfMemoryException(L#p); };}
