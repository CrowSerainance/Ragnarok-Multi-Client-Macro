#include <windows.h>
#include "AgentPipeServer.h"

static prt::AgentPipeServer* g_server = nullptr;

BOOL APIENTRY DllMain(HMODULE hModule, DWORD ul_reason_for_call, LPVOID lpReserved) {
    switch (ul_reason_for_call) {
    case DLL_PROCESS_ATTACH:
        DisableThreadLibraryCalls(hModule);
        g_server = new prt::AgentPipeServer();
        g_server->Start();
        break;
    case DLL_PROCESS_DETACH:
        if (g_server) {
            g_server->Stop();
            delete g_server;
            g_server = nullptr;
        }
        break;
    }
    return TRUE;
}
