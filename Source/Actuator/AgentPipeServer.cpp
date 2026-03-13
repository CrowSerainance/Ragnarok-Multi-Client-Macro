#include "AgentPipeServer.h"
#include "../Core/IpcProtocol.h"
#include <iostream>
#include <vector>

namespace prt {

AgentPipeServer::AgentPipeServer() {
    _injector = std::make_unique<InputInjector>();
}

AgentPipeServer::~AgentPipeServer() {
    Stop();
}

void AgentPipeServer::Start() {
    if (_running) return;
    _running = true;
    _serverThread = std::thread(&AgentPipeServer::RunServer, this);
}

void AgentPipeServer::Stop() {
    _running = false;
    // Note: In a real implementation, we'd use a more robust way to break the ConnectNamedPipe block
    // like a dummy client connection or using overlapped I/O.
    if (_serverThread.joinable()) {
        _serverThread.detach(); 
    }
}

void AgentPipeServer::RunServer() {
    std::string pipeName = ipc::MakePipeName(GetCurrentProcessId());
    
    while (_running) {
        HANDLE hPipe = CreateNamedPipeA(
            pipeName.c_str(),
            PIPE_ACCESS_DUPLEX,
            PIPE_TYPE_MESSAGE | PIPE_READMODE_MESSAGE | PIPE_WAIT,
            PIPE_UNLIMITED_INSTANCES,
            1024, 1024, 0, NULL);

        if (hPipe == INVALID_HANDLE_VALUE) {
            std::this_thread::sleep_for(std::chrono::seconds(1));
            continue;
        }

        if (ConnectNamedPipe(hPipe, NULL) || GetLastError() == ERROR_PIPE_CONNECTED) {
            ProcessMessage(hPipe);
        }

        CloseHandle(hPipe);
    }
}

void AgentPipeServer::ProcessMessage(HANDLE hPipe) {
    ipc::Header header;
    DWORD bytesRead;

    if (!ReadFile(hPipe, &header, sizeof(header), &bytesRead, NULL) || bytesRead != sizeof(header)) {
        return;
    }

    std::vector<uint8_t> payload(header.payloadLength);
    if (header.payloadLength > 0) {
        if (!ReadFile(hPipe, payload.data(), (DWORD)payload.size(), &bytesRead, NULL) || bytesRead != header.payloadLength) {
            return;
        }
    }

    switch (header.commandId) {
        case ipc::CMD_EXECUTE_BINDING: {
            if (payload.size() < sizeof(ipc::ExecuteBindingHeader)) break;
            auto* bind = reinterpret_cast<ipc::ExecuteBindingHeader*>(payload.data());
            
            // Execute Key
            _injector->SendKey(bind->vk, bind->scanCode, bind->postDelayMs);

            // Execute Clicks
            if (bind->pointCount > 0 && payload.size() >= sizeof(ipc::ExecuteBindingHeader) + (bind->pointCount * sizeof(ipc::ClickPoint))) {
                auto* points = reinterpret_cast<ipc::ClickPoint*>(payload.data() + sizeof(ipc::ExecuteBindingHeader));
                for (uint32_t i = 0; i < bind->pointCount; i++) {
                    _injector->SendClick(points[i].x, points[i].y, bind->mousePosXOffset, bind->mousePosYOffset);
                    if (bind->interClickMs > 0 && i < bind->pointCount - 1) {
                        std::this_thread::sleep_for(std::chrono::milliseconds(bind->interClickMs));
                    }
                }
            }
            break;
        }
        case ipc::CMD_EXECUTE_MACRO: {
            if (payload.size() < sizeof(ipc::ExecuteMacroHeader)) break;
            auto* macroCmd = reinterpret_cast<ipc::ExecuteMacroHeader*>(payload.data());
            
            // Background Execution Constraint: Strictly internal
            // Targeting Formula: X cells from center (Up, Down, Left, Right)
            
            // 1. Get Game Window Dimensions
            RECT rect;
            HWND hwnd = GetTopWindow(NULL); // Simplified: should use cached HWND
            GetClientRect(hwnd, &rect);
            int centerX = (rect.right - rect.left) / 2;
            int centerY = (rect.bottom - rect.top) / 2;

            // 2. Calculate Offset (Assuming ~30px per cell at default zoom)
            const int PIXELS_PER_CELL = 32; 
            int targetX = centerX;
            int targetY = centerY;

            switch (static_cast<ipc::TargetDirection>(macroCmd->targetDirection)) {
                case ipc::TargetDirection::Up:    targetY -= (macroCmd->targetDistance * PIXELS_PER_CELL); break;
                case ipc::TargetDirection::Down:  targetY += (macroCmd->targetDistance * PIXELS_PER_CELL); break;
                case ipc::TargetDirection::Left:  targetX -= (macroCmd->targetDistance * PIXELS_PER_CELL); break;
                case ipc::TargetDirection::Right: targetX += (macroCmd->targetDistance * PIXELS_PER_CELL); break;
            }

            // 3. Execute Internal Action
            // Directly invoke the game's internal skill cast on targetX, targetY 
            // without bringing the window to foreground or sending OS messages.
            std::cout << "[AgentPipeServer] Silent Skill Cast: ID=" << macroCmd->macroId 
                      << " Dir=" << (int)macroCmd->targetDirection 
                      << " Dist=" << (int)macroCmd->targetDistance 
                      << " @ ScreenPos(" << targetX << "," << targetY << ")" << std::endl;
            break;
        }
        case ipc::CMD_EXECUTE_LUA: {
            // Payload is the raw Lua string
            std::string script(reinterpret_cast<char*>(payload.data()), payload.size());
            
            // Background Execution Constraint: Strictly internal
            // Bridge to the internal Lua engine state (e.g., luaL_dostring)
            std::cout << "[AgentPipeServer] Executing internal Lua Script: " << script << std::endl;
            // ExecuteGameLuaScript(script);
            break;
        }
        case ipc::CMD_PING: {
            bool ok = true;
            auto ack = ipc::Pack(ipc::CMD_RESULT, &ok, sizeof(ok));
            DWORD bytesWritten;
            WriteFile(hPipe, ack.data(), (DWORD)ack.size(), &bytesWritten, NULL);
            break;
        }
    }
}

} // namespace prt
