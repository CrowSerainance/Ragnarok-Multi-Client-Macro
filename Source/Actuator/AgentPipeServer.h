#pragma once
#include <windows.h>
#include <string>
#include <thread>
#include <memory>
#include <atomic>
#include "../Core/Types.h"
#include "InputInjector.h"

namespace prt {

class AgentPipeServer {
public:
    AgentPipeServer(ClientProfile& profile);
    ~AgentPipeServer();

    void Start();
    void Stop();

private:
    std::atomic<bool> _running{false};
    std::thread _serverThread;
    std::unique_ptr<InputInjector> _injector;
    ClientProfile& _profile;

    void RunServer();
    void ProcessMessage(HANDLE hPipe);
};

} // namespace prt
