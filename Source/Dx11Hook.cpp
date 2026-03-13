#include "Dx11Hook.h"
#include <vector>

namespace prt {

Present_t Dx11Hook::OriginalPresent = nullptr;
static std::function<void(IDXGISwapChain*)> g_renderCallback;
static uintptr_t* g_vmtPresent = nullptr;

HRESULT STDMETHODCALLTYPE HookedPresent(IDXGISwapChain* pSwapChain, UINT SyncInterval, UINT Flags) {
    if (g_renderCallback) g_renderCallback(pSwapChain);
    return Dx11Hook::OriginalPresent(pSwapChain, SyncInterval, Flags);
}

bool Dx11Hook::Initialize(std::function<void(IDXGISwapChain*)> callback) {
    g_renderCallback = callback;

    // Create dummy device/swapchain to find VTable
    D3D_FEATURE_LEVEL featureLevel = D3D_FEATURE_LEVEL_11_0;
    DXGI_SWAP_CHAIN_DESC scd = {};
    scd.BufferCount = 1;
    scd.BufferDesc.Format = DXGI_FORMAT_R8G8B8A8_UNORM;
    scd.BufferUsage = DXGI_USAGE_RENDER_TARGET_OUTPUT;
    scd.OutputWindow = GetForegroundWindow();
    scd.SampleDesc.Count = 1;
    scd.Windowed = TRUE;
    scd.SwapEffect = DXGI_SWAP_EFFECT_DISCARD;

    ID3D11Device* pDevice = nullptr;
    ID3D11DeviceContext* pContext = nullptr;
    IDXGISwapChain* pSwapChain = nullptr;

    HRESULT hr = D3D11CreateDeviceAndSwapChain(NULL, D3D_DRIVER_TYPE_HARDWARE, NULL, 0, &featureLevel, 1, D3D11_SDK_VERSION, &scd, &pSwapChain, &pDevice, NULL, &pContext);
    if (FAILED(hr)) return false;

    // Find Present address in VTable (index 8 for IDXGISwapChain)
    uintptr_t* vtable = *(uintptr_t**)pSwapChain;
    g_vmtPresent = &vtable[8];
    OriginalPresent = (Present_t)*g_vmtPresent;

    // Hook via simple memory rewrite (Internal bypass)
    DWORD oldProtect;
    VirtualProtect(g_vmtPresent, sizeof(uintptr_t), PAGE_EXECUTE_READWRITE, &oldProtect);
    *g_vmtPresent = (uintptr_t)HookedPresent;
    VirtualProtect(g_vmtPresent, sizeof(uintptr_t), oldProtect, &oldProtect);

    pDevice->Release();
    pContext->Release();
    pSwapChain->Release();

    return true;
}

void Dx11Hook::Shutdown() {
    if (g_vmtPresent && OriginalPresent) {
        DWORD oldProtect;
        VirtualProtect(g_vmtPresent, sizeof(uintptr_t), PAGE_EXECUTE_READWRITE, &oldProtect);
        *g_vmtPresent = (uintptr_t)OriginalPresent;
        VirtualProtect(g_vmtPresent, sizeof(uintptr_t), oldProtect, &oldProtect);
    }
}

} // namespace prt
