#include "AgentPipeServer.h"
#include "../Core/IpcProtocol.h"
#include <iostream>
#include <vector>

namespace prt {

AgentPipeServer::AgentPipeServer(ClientProfile& profile) : _profile(profile) {
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
            _injector->SendKey(bind->vk, bind->scanCode, bind->postDelayMs);
            break;
        }
        case ipc::CMD_SYNC_AUTOPOT: {
            if (payload.size() < sizeof(ipc::SyncAutopotPayload)) break;
            auto* p = reinterpret_cast<ipc::SyncAutopotPayload*>(payload.data());
            _profile.autopot.enabled = p->enabled;
            _profile.autopot.hpKey = p->hpKey;
            _profile.autopot.hpThreshold = p->hpThreshold;
            _profile.autopot.spKey = p->spKey;
            _profile.autopot.spThreshold = p->spThreshold;
            _profile.autopot.yggKey = p->yggKey;
            _profile.autopot.yggThreshold = p->yggThreshold;
            _profile.autopot.delayMs = p->delayMs;
            break;
        }
        case ipc::CMD_SYNC_AUTOBUFF: {
            if (payload.size() < sizeof(ipc::SyncAutobuffPayload)) break;
            auto* p = reinterpret_cast<ipc::SyncAutobuffPayload*>(payload.data());
            _profile.autobuff.enabled = p->enabled;
            _profile.autobuff.buffs.clear();
            auto* entries = reinterpret_cast<ipc::SyncBuffEntry*>(payload.data() + sizeof(ipc::SyncAutobuffPayload));
            for (uint32_t i = 0; i < p->count; ++i) {
                BuffConfig b;
                b.statusId = entries[i].statusId;
                b.key = entries[i].key;
                b.enabled = entries[i].enabled;
                _profile.autobuff.buffs.push_back(b);
            }
            break;
        }
        case ipc::CMD_SYNC_SPAMMER: {
            if (payload.size() < sizeof(ipc::SyncSpammerPayload)) break;
            auto* p = reinterpret_cast<ipc::SyncSpammerPayload*>(payload.data());
            _profile.spammer.enabled = p->enabled;
            _profile.spammer.keys.clear();
            auto* entries = reinterpret_cast<ipc::SyncSpammerEntry*>(payload.data() + sizeof(ipc::SyncSpammerPayload));
            for (uint32_t i = 0; i < p->count; ++i) {
                SpammerKey s;
                s.key = entries[i].key;
                s.intervalMs = entries[i].intervalMs;
                s.enabled = entries[i].enabled;
                _profile.spammer.keys.push_back(s);
            }
            break;
        }
        case ipc::CMD_SYNC_RECOVERY: {
            if (payload.size() < sizeof(ipc::SyncRecoveryPayload)) break;
            auto* p = reinterpret_cast<ipc::SyncRecoveryPayload*>(payload.data());
            _profile.recovery.enabled = p->enabled;
            _profile.recovery.recoveries.clear();
            auto* entries = reinterpret_cast<ipc::SyncRecoveryEntry*>(payload.data() + sizeof(ipc::SyncRecoveryPayload));
            for (uint32_t i = 0; i < p->count; ++i) {
                RecoveryConfig r;
                r.statusId = entries[i].statusId;
                r.key = entries[i].key;
                r.enabled = entries[i].enabled;
                _profile.recovery.recoveries.push_back(r);
            }
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
