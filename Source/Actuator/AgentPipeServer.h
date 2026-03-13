#pragma once
#include <windows.h>
#include <string>
#include <thread>
#include <atomic>
#include <memory>
#include "InputInjector.h"

namespace prt {

class AgentPipeServer {
public:
    AgentPipeServer();
    ~AgentPipeServer();

    void Start();
    void Stop();

private:
    std::atomic<bool> _running{false};
    std::thread _serverThread;
    std::unique_ptr<InputInjector> _injector;

    void RunServer();
    void ProcessMessage(HANDLE hPipe);
};

} // namespace prt
