#include <windows.h>
#include <d3d11.h>
#include <thread>
#include <atomic>
#include "Dx11Hook.h"
#include "Menu.h"
#include "Types.h"
#include "ConfigService.h"
#include "MemoryReader.h"
#include "Bot/GameReader.h"
#include "Bot/BotFsm.h"
#include "Bot/FeatureManager.h"
#include "Actuator/AgentPipeServer.h"

// ImGui
#include "../vendor/imgui/imgui.h"
#include "../vendor/imgui/backends/imgui_impl_win32.h"
#include "../vendor/imgui/backends/imgui_impl_dx11.h"

namespace prt {

static ID3D11Device* g_pd3dDevice = nullptr;
static ID3D11DeviceContext* g_pd3dDeviceContext = nullptr;
static ID3D11RenderTargetView* g_mainRenderTargetView = nullptr;
static bool g_imguiInitialized = false;
static AppConfig g_config;
static ClientProfile g_activeProfile;
static std::atomic<bool> g_running{true};
static std::unique_ptr<AgentPipeServer> g_pipeServer;

// Forward declare wndproc hook
extern LRESULT IMGUI_IMPL_API ImGui_ImplWin32_WndProcHandler(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam);
static WNDPROC g_originalWndProc = nullptr;

LRESULT CALLBACK HookedWndProc(HWND hWnd, UINT msg, WPARAM wParam, LPARAM lParam) {
    if (Menu::IsOpen() && ImGui_ImplWin32_WndProcHandler(hWnd, msg, wParam, lParam))
        return true;
        
    // Toggle Menu on INSERT
    if (msg == WM_KEYDOWN && wParam == VK_INSERT) {
        Menu::Toggle();
    }

    return CallWindowProc(g_originalWndProc, hWnd, msg, wParam, lParam);
}

void OnPresent(IDXGISwapChain* pSwapChain) {
    if (!g_imguiInitialized) {
        if (SUCCEEDED(pSwapChain->GetDevice(__uuidof(ID3D11Device), (void**)&g_pd3dDevice))) {
            g_pd3dDevice->GetImmediateContext(&g_pd3dDeviceContext);
            
            DXGI_SWAP_CHAIN_DESC sd;
            pSwapChain->GetDesc(&sd);
            
            ID3D11Texture2D* pBackBuffer;
            pSwapChain->GetBuffer(0, __uuidof(ID3D11Texture2D), (LPVOID*)&pBackBuffer);
            g_pd3dDevice->CreateRenderTargetView(pBackBuffer, NULL, &g_mainRenderTargetView);
            pBackBuffer->Release();

            ImGui::CreateContext();
            ImGui_ImplWin32_Init(sd.OutputWindow);
            ImGui_ImplDX11_Init(g_pd3dDevice, g_pd3dDeviceContext);
            
            // Hook WNDPROC
            g_originalWndProc = (WNDPROC)SetWindowLongPtr(sd.OutputWindow, GWLP_WNDPROC, (LONG_PTR)HookedWndProc);
            
            g_imguiInitialized = true;
        }
        return;
    }

    ImGui_ImplDX11_NewFrame();
    ImGui_ImplWin32_NewFrame();
    ImGui::NewFrame();

    if (Menu::IsOpen()) {
        Menu::Render(g_config);
    }

    ImGui::Render();
    g_pd3dDeviceContext->OMSetRenderTargets(1, &g_mainRenderTargetView, NULL);
    ImGui_ImplDX11_RenderDrawData(ImGui::GetDrawData());
}

void BotThread() {
    // Initial load
    ConfigService::Load(g_config, "E:\\RAGNAROK ONLINE\\Personal Ragnarok Tool\\Config\\appconfig.json");
    
    // Find the profile for this process
    DWORD pid = GetCurrentProcessId();
    for (auto& profile : g_config.clientProfiles) {
        if (profile.boundWindow.processId == (int)pid) {
            g_activeProfile = profile;
            break;
        }
    }

    auto injector = std::make_shared<InputInjector>();
    auto mem = std::make_shared<MemoryReader>();
    GameReader reader(mem, g_config.addresses);
    FeatureManager features(injector);
    BotFsm fsm;

    // Start Pipe Server for live updates
    g_pipeServer = std::make_unique<AgentPipeServer>(g_activeProfile);
    g_pipeServer->Start();

    while (g_running) {
        if (g_config.botEnabled) {
            GameState state;
            if (reader.TryRead(state)) {
                features.Update(state, g_activeProfile);
                // Execute FSM logic...
            }
        }
        std::this_thread::sleep_for(std::chrono::milliseconds(20));
    }

    if (g_pipeServer) g_pipeServer->Stop();
}

void MainThread(HMODULE hModule) {
    if (Dx11Hook::Initialize(OnPresent)) {
        std::thread(BotThread).detach();
        
        while (g_running) {
            std::this_thread::sleep_for(std::chrono::milliseconds(500));
        }
    }
}

} // namespace prt

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    if (ul_reason_for_call == DLL_PROCESS_ATTACH) {
        DisableThreadLibraryCalls(hModule);
        std::thread(prt::MainThread, hModule).detach();
    }
    return TRUE;
}
