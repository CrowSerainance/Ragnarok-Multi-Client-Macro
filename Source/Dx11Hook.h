#pragma once
#include <windows.h>
#include <d3d11.h>
#include <functional>

namespace prt {

typedef HRESULT(STDMETHODCALLTYPE* Present_t)(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags);

class Dx11Hook {
public:
    static bool Initialize(std::function<void(IDXGISwapChain*)> callback);
    static void Shutdown();

    static Present_t OriginalPresent;
};

} // namespace prt
