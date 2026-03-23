#include "AgentPipeServer.h"
#include "../IpcProtocol.h"
#include <chrono>
#include <cstdint>
#include <vector>

namespace prt {

namespace {

template <typename T>
bool ReadStruct(const std::vector<uint8_t>& payload, size_t& offset, T& value) {
    if (offset + sizeof(T) > payload.size()) {
        return false;
    }

    value = *reinterpret_cast<const T*>(payload.data() + offset);
    offset += sizeof(T);
    return true;
}

bool ReadKeyList(const std::vector<uint8_t>& payload, size_t& offset, uint32_t count, std::vector<AtkDefKeyEntry>& output) {
    output.clear();
    output.reserve(count);

    for (uint32_t i = 0; i < count; ++i) {
        uint16_t key = 0;
        if (!ReadStruct(payload, offset, key)) {
            output.clear();
            return false;
        }

        AtkDefKeyEntry entry;
        entry.key = key;
        output.push_back(entry);
    }

    return true;
}

bool ReadMacroPayload(const std::vector<uint8_t>& payload, MacroSongConfig& config) {
    size_t offset = 0;
    ipc::SyncMacroPayload header{};
    if (!ReadStruct(payload, offset, header)) {
        return false;
    }

    config.enabled = header.enabled;
    config.lanes.clear();
    config.lanes.reserve(header.laneCount);

    for (uint32_t laneIndex = 0; laneIndex < header.laneCount; ++laneIndex) {
        ipc::SyncMacroLaneHeader laneHeader{};
        if (!ReadStruct(payload, offset, laneHeader)) {
            return false;
        }

        MacroChainLane lane;
        lane.triggerKey = laneHeader.triggerKey;
        lane.daggerKey = laneHeader.daggerKey;
        lane.instrumentKey = laneHeader.instrumentKey;
        lane.delayMs = laneHeader.delayMs;
        lane.infinityLoop = laneHeader.infinityLoop;
        lane.entries.reserve(laneHeader.entryCount);

        for (uint32_t entryIndex = 0; entryIndex < laneHeader.entryCount; ++entryIndex) {
            ipc::SyncMacroChainEntry entryPayload{};
            if (!ReadStruct(payload, offset, entryPayload)) {
                return false;
            }

            MacroChainEntry entry;
            entry.key = entryPayload.key;
            entry.delayMs = entryPayload.delayMs;
            entry.hasClick = entryPayload.hasClick;
            lane.entries.push_back(entry);
        }

        config.lanes.push_back(std::move(lane));
    }

    return true;
}

bool ReadMacroPayload(const std::vector<uint8_t>& payload, MacroSwitchConfig& config) {
    MacroSongConfig temp;
    if (!ReadMacroPayload(payload, temp)) {
        return false;
    }

    config.enabled = temp.enabled;
    config.lanes.assign(temp.lanes.begin(), temp.lanes.end());
    return true;
}

void ApplyBuffPayload(const std::vector<uint8_t>& payload, AutobuffConfig& config) {
    if (payload.size() < sizeof(ipc::SyncAutobuffPayload)) {
        return;
    }

    auto* header = reinterpret_cast<const ipc::SyncAutobuffPayload*>(payload.data());
    size_t required = sizeof(ipc::SyncAutobuffPayload) + (sizeof(ipc::SyncBuffEntry) * header->count);
    if (payload.size() < required) {
        return;
    }

    config.enabled = header->enabled;
    config.buffs.clear();
    auto* entries = reinterpret_cast<const ipc::SyncBuffEntry*>(payload.data() + sizeof(ipc::SyncAutobuffPayload));
    for (uint32_t i = 0; i < header->count; ++i) {
        BuffConfig buff;
        buff.statusId = entries[i].statusId;
        buff.key = entries[i].key;
        buff.enabled = entries[i].enabled;
        config.buffs.push_back(buff);
    }
}

void ApplyBuffPayload(const std::vector<uint8_t>& payload, AutobuffSkillsConfig& config) {
    AutobuffConfig temp;
    ApplyBuffPayload(payload, temp);
    config.enabled = temp.enabled;
    config.buffs.assign(temp.buffs.begin(), temp.buffs.end());
}

void ApplyBuffPayload(const std::vector<uint8_t>& payload, AutobuffItemsConfig& config) {
    AutobuffConfig temp;
    ApplyBuffPayload(payload, temp);
    config.enabled = temp.enabled;
    config.buffs.assign(temp.buffs.begin(), temp.buffs.end());
}

void ApplyRecoveryPayload(const std::vector<uint8_t>& payload, StatusRecoveryConfig& config) {
    if (payload.size() < sizeof(ipc::SyncRecoveryPayload)) {
        return;
    }

    auto* header = reinterpret_cast<const ipc::SyncRecoveryPayload*>(payload.data());
    size_t required = sizeof(ipc::SyncRecoveryPayload) + (sizeof(ipc::SyncRecoveryEntry) * header->count);
    if (payload.size() < required) {
        return;
    }

    config.enabled = header->enabled;
    config.recoveries.clear();
    auto* entries = reinterpret_cast<const ipc::SyncRecoveryEntry*>(payload.data() + sizeof(ipc::SyncRecoveryPayload));
    for (uint32_t i = 0; i < header->count; ++i) {
        RecoveryConfig recovery;
        recovery.statusId = entries[i].statusId;
        recovery.key = entries[i].key;
        recovery.enabled = entries[i].enabled;
        config.recoveries.push_back(recovery);
    }
}

void ApplyRecoveryPayload(const std::vector<uint8_t>& payload, DebuffRecoveryConfig& config) {
    if (payload.size() < sizeof(ipc::SyncRecoveryPayload)) {
        return;
    }

    auto* header = reinterpret_cast<const ipc::SyncRecoveryPayload*>(payload.data());
    size_t required = sizeof(ipc::SyncRecoveryPayload) + (sizeof(ipc::SyncRecoveryEntry) * header->count);
    if (payload.size() < required) {
        return;
    }

    config.enabled = header->enabled;
    config.autoStand = header->autoStand;
    config.groupStatusKey = header->groupStatusKey;
    config.groupNewStatusKey = header->groupNewStatusKey;
    config.recoveries.clear();

    auto* entries = reinterpret_cast<const ipc::SyncRecoveryEntry*>(payload.data() + sizeof(ipc::SyncRecoveryPayload));
    for (uint32_t i = 0; i < header->count; ++i) {
        RecoveryConfig recovery;
        recovery.statusId = entries[i].statusId;
        recovery.key = entries[i].key;
        recovery.enabled = entries[i].enabled;
        config.recoveries.push_back(recovery);
    }
}

} // namespace

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
            4096, 4096, 0, NULL);

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
    ipc::Header header{};
    DWORD bytesRead = 0;

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
            auto* bind = reinterpret_cast<const ipc::ExecuteBindingHeader*>(payload.data());
            _injector->SendKey(bind->vk, bind->scanCode, bind->postDelayMs);
            break;
        }
        case ipc::CMD_SYNC_AUTOPOT: {
            if (payload.size() < sizeof(ipc::SyncAutopotPayload)) break;
            auto* p = reinterpret_cast<const ipc::SyncAutopotPayload*>(payload.data());
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
        case ipc::CMD_SYNC_AUTOBUFF:
            ApplyBuffPayload(payload, _profile.autobuff);
            break;
        case ipc::CMD_SYNC_AUTOBUFF_SKILLS:
            ApplyBuffPayload(payload, _profile.autobuffSkills);
            break;
        case ipc::CMD_SYNC_AUTOBUFF_ITEMS:
            ApplyBuffPayload(payload, _profile.autobuffItems);
            break;
        case ipc::CMD_SYNC_SPAMMER: {
            if (payload.size() < sizeof(ipc::SyncSpammerPayload)) break;
            auto* p = reinterpret_cast<const ipc::SyncSpammerPayload*>(payload.data());
            size_t required = sizeof(ipc::SyncSpammerPayload) + (sizeof(ipc::SyncSpammerEntry) * p->count);
            if (payload.size() < required) break;

            _profile.spammer.enabled = p->enabled;
            _profile.spammer.keys.clear();
            auto* entries = reinterpret_cast<const ipc::SyncSpammerEntry*>(payload.data() + sizeof(ipc::SyncSpammerPayload));
            for (uint32_t i = 0; i < p->count; ++i) {
                SpammerKey spammer;
                spammer.key = entries[i].key;
                spammer.intervalMs = entries[i].intervalMs;
                spammer.enabled = entries[i].enabled;
                _profile.spammer.keys.push_back(spammer);
            }
            break;
        }
        case ipc::CMD_SYNC_RECOVERY:
            ApplyRecoveryPayload(payload, _profile.recovery);
            break;
        case ipc::CMD_SYNC_DEBUFF_RECOVERY:
            ApplyRecoveryPayload(payload, _profile.debuffRecovery);
            break;
        case ipc::CMD_SYNC_SKILL_TIMER: {
            if (payload.size() < sizeof(ipc::SyncSkillTimerPayload)) break;
            auto* p = reinterpret_cast<const ipc::SyncSkillTimerPayload*>(payload.data());
            _profile.skillTimers.enabled = p->enabled;
            _profile.skillTimers.timer1 = { p->timer1.key, p->timer1.delaySeconds, p->timer1.enabled };
            _profile.skillTimers.timer2 = { p->timer2.key, p->timer2.delaySeconds, p->timer2.enabled };
            _profile.skillTimers.timer3 = { p->timer3.key, p->timer3.delaySeconds, p->timer3.enabled };
            break;
        }
        case ipc::CMD_SYNC_ATK_DEF: {
            size_t offset = 0;
            ipc::SyncAtkDefPayload p{};
            if (!ReadStruct(payload, offset, p)) break;

            _profile.atkDefMode.enabled = p.enabled;
            _profile.atkDefMode.spammerKey = p.spammerKey;
            _profile.atkDefMode.spammerWithClick = p.spammerWithClick;
            _profile.atkDefMode.spammerDelay = p.spammerDelay;
            _profile.atkDefMode.switchDelay = p.switchDelay;

            if (!ReadKeyList(payload, offset, p.atkCount, _profile.atkDefMode.atkKeys)) break;
            if (!ReadKeyList(payload, offset, p.defCount, _profile.atkDefMode.defKeys)) break;
            break;
        }
        case ipc::CMD_SYNC_MACRO_SONG:
            ReadMacroPayload(payload, _profile.macroSongs);
            break;
        case ipc::CMD_SYNC_MACRO_SWITCH:
            ReadMacroPayload(payload, _profile.macroSwitch);
            break;
        case ipc::CMD_PING: {
            bool ok = true;
            auto ack = ipc::Pack(ipc::CMD_RESULT, &ok, sizeof(ok));
            DWORD bytesWritten = 0;
            WriteFile(hPipe, ack.data(), (DWORD)ack.size(), &bytesWritten, NULL);
            break;
        }
        default:
            break;
    }
}

} // namespace prt
