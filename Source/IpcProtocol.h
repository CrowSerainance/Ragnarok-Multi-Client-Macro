#pragma once
#include <windows.h>
#include <string>
#include <vector>
#include <cstdint>
#include "Types.h"

namespace prt {
namespace ipc {

static const uint32_t CMD_HANDSHAKE      = 0x01;
static const uint32_t CMD_EXECUTE_BINDING = 0x02;
static const uint32_t CMD_CAPTURE_PREVIEW = 0x03;
static const uint32_t CMD_PING           = 0x04;
static const uint32_t CMD_GET_GAME_STATE = 0x05;
static const uint32_t CMD_EXECUTE_MACRO  = 0x06;
static const uint32_t CMD_EXECUTE_LUA    = 0x07;
static const uint32_t CMD_SYNC_AUTOPOT   = 0x10;
static const uint32_t CMD_SYNC_AUTOBUFF  = 0x11;
static const uint32_t CMD_SYNC_SPAMMER   = 0x12;
static const uint32_t CMD_SYNC_RECOVERY  = 0x13;
static const uint32_t CMD_RESULT         = 0xFF;

#pragma pack(push, 1)
struct Header {
    uint32_t commandId;
    uint32_t payloadLength;
};

enum class TargetDirection : uint8_t {
    Up = 0,
    Down = 1,
    Left = 2,
    Right = 3
};

struct ExecuteMacroHeader {
    uint32_t macroId;
    uint16_t virtualKey;
    uint8_t  targetDirection; // 0=Up, 1=Down, 2=Left, 3=Right
    uint8_t  targetDistance;  // Cell radius: 5, 8, or 10
};

struct ExecuteBindingHeader {
    uint16_t vk;
    uint16_t scanCode;
    uint32_t postDelayMs;
    uint32_t interClickMs;
    uint32_t mousePosXOffset;
    uint32_t mousePosYOffset;
    uint32_t pointCount;
};

struct ClickPoint {
    int x;
    int y;
};

// ---- Sync Payloads ----

struct SyncAutopotPayload {
    bool enabled;
    uint16_t hpKey;
    int hpThreshold;
    uint16_t spKey;
    int spThreshold;
    uint16_t yggKey;
    int yggThreshold;
    int delayMs;
};

struct SyncBuffEntry {
    uint32_t statusId;
    uint16_t key;
    bool enabled;
};

struct SyncAutobuffPayload {
    bool enabled;
    uint32_t count;
    // Followed by count * SyncBuffEntry
};

struct SyncSpammerEntry {
    uint16_t key;
    int intervalMs;
    bool enabled;
};

struct SyncSpammerPayload {
    bool enabled;
    uint32_t count;
    // Followed by count * SyncSpammerEntry
};

struct SyncRecoveryEntry {
    uint32_t statusId;
    uint16_t key;
    bool enabled;
};

struct SyncRecoveryPayload {
    bool enabled;
    uint32_t count;
    // Followed by count * SyncRecoveryEntry
};
#pragma pack(pop)

inline std::string MakePipeName(uint32_t pid) {
    return "\\\\.\\pipe\\PRT_Agent_" + std::to_string(pid);
}

inline std::vector<uint8_t> Pack(uint32_t cmdId, const void* payload, uint32_t length) {
    std::vector<uint8_t> buf(sizeof(Header) + length);
    Header* hdr = reinterpret_cast<Header*>(buf.data());
    hdr->commandId = cmdId;
    hdr->payloadLength = length;
    if (length > 0 && payload) {
        memcpy(buf.data() + sizeof(Header), payload, length);
    }
    return buf;
}

} // namespace ipc
} // namespace prt
