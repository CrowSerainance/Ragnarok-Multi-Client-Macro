#pragma once
#include <string>
#include <vector>
#include <cstdint>
#include <map>

namespace prt {

// ---- Geometry ----

struct PixelPoint {
    int x = 0;
    int y = 0;
};

// ---- Binding ----

struct MacroBinding {
    std::string id;
    std::string clientProfileId;
    std::string name;
    bool isEnabled          = true;
    std::string triggerHotkey;
    std::string inputKey;
    int cellRadius          = 5;
    int postInputDelayMs    = 100;
    int interClickDelayMs   = 50;
    int clickCount          = 1;
};

// ---- Client Window Ref ----

struct ClientWindowRef {
    int64_t windowHandle = 0;
    int processId        = 0;
    std::string processName;
    std::string windowTitle;
    int clientWidth      = 0;
    int clientHeight     = 0;
};

// ---- Bot Config & Addresses ----

struct AddressConfig {
    uint32_t mousePosX = 0xB47F60;
    uint32_t mousePosY = 0xB47F64;
    uint32_t chatBarEnabled = 0xB81390;
    uint32_t worldBaseIntermed = 0xB3D1D4;
    uint32_t playerName = 0xDD43E8;
    uint32_t playerCurrentHp = 0xDD1A04;
    uint32_t playerMaxHp = 0xDD1A08;
    uint32_t playerCurrentSp = 0xDD1A0C;
    uint32_t playerMaxSp = 0xDD1A10;
    uint32_t playerCoordinateX = 0xDBA5A0;
    uint32_t playerCoordinateY = 0xDBA5A4;
    uint32_t mapName = 0xB3D1D8;
    uint32_t worldBaseOffset = 0xCC;
    uint32_t playerBaseOffset = 0x2C;
    uint32_t entityListOffset = 0x10;
    uint32_t entityNodeNext = 0x00;
    uint32_t entityNodePrev = 0x04;
    uint32_t entityNodeEntityPtr = 0x08;
    uint32_t entityId = 0x10C;
    uint32_t entityType = 0;
    uint32_t entityWorldX = 0x16C;
    uint32_t entityWorldY = 0x170;
    uint32_t entityScreenX = 0xAC;
    uint32_t entityScreenY = 0xB0;
    uint32_t entityName = 0x30;
};

// ---- Game State ----

struct Entity {
    int id = 0;
    int worldX = 0;
    int worldY = 0;
    int screenX = 0;
    int screenY = 0;
    std::string name;
    int currentHp = 0;
    int maxHp = 0;
    bool isMonster = false;
    bool isPlayer = false;
    bool isNpc = false;
    uintptr_t baseAddress = 0;
};

struct PlayerState {
    int currentHp = 0;
    int maxHp = 1;
    int currentSp = 0;
    int maxSp = 1;
    int worldX = 0;
    int worldY = 0;
    std::string name;
    std::string mapName;
    bool isChatBarOpen = false;
    uint32_t statusBuffer[100] = {0};
};

struct GameState {
    PlayerState player;
    std::vector<Entity> entities;
};

// ---- Feature Configs ----

struct AutopotConfig {
    bool enabled = false;
    uint16_t hpKey = 0;
    int hpThreshold = 50;
    uint16_t spKey = 0;
    int spThreshold = 30;
    uint16_t yggKey = 0;
    int yggThreshold = 20;
    int delayMs = 50;
};

struct BuffConfig {
    uint32_t statusId = 0;
    uint16_t key = 0;
    bool enabled = true;
};

struct AutobuffConfig {
    bool enabled = false;
    std::vector<BuffConfig> buffs;
};

struct SpammerKey {
    uint16_t key = 0;
    int intervalMs = 100;
    bool enabled = true;
};

struct SpammerConfig {
    bool enabled = false;
    std::vector<SpammerKey> keys;
};

struct RecoveryConfig {
    uint32_t statusId = 0;
    uint16_t key = 0;
    bool enabled = true;
};

struct StatusRecoveryConfig {
    bool enabled = false;
    std::vector<RecoveryConfig> recoveries;
};

// ---- Client Profile ----

struct ClientProfile {
    std::string id;
    std::string displayName;
    bool isEnabled = true;
    ClientWindowRef boundWindow;
    bool hasBoundWindow = false;
    std::vector<MacroBinding> bindings;
    
    // 4R Features
    AutopotConfig autopot;
    AutobuffConfig autobuff;
    SpammerConfig spammer;
    StatusRecoveryConfig recovery;

    // Runtime
    std::string runtimeStatusLabel;
    std::string runtimeStatusDetail;
    bool hasLiveWindow = false;
};

// ---- App Config ----

struct AppConfig {
    int version                 = 1;
    std::string lastSavedUtc;
    bool botEnabled             = false;
    AddressConfig addresses;
    std::vector<ClientProfile> clientProfiles;
};

// ---- Session Access ----

enum class SessionAccessMode {
    UserMode,
    BorrowedHandle,
    InjectedAgent,
    KernelBacked,
};

// ---- IPC ----

enum class AgentCommandId : uint32_t {
    Handshake      = 0x01,
    ExecuteBinding = 0x02,
    CapturePreview = 0x03,
    Ping           = 0x04,
    GetGameState   = 0x05,
    SyncAutopot    = 0x06,
    SyncAutobuff   = 0x07,
    SyncSpammer    = 0x08,
    SyncRecovery   = 0x09,
    Result         = 0xFF,
};

struct AgentResult {
    bool success = false;
    std::string message;
};

} // namespace prt
