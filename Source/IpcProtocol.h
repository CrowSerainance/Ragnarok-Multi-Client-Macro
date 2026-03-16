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
