#include "GameReader.h"
#include <algorithm>

namespace prt {

GameReader::GameReader(std::shared_ptr<MemoryReader> reader, const AddressConfig& addr)
    : _reader(reader), _addr(addr) {}

bool GameReader::TryRead(GameState& state) {
    if (!_reader) return false;

    uintptr_t baseAddr = _reader->GetBaseAddress();

    // Player stats
    state.player.currentHp = _reader->ReadInt32(baseAddr + _addr.playerCurrentHp);
    state.player.maxHp = _reader->ReadInt32(baseAddr + _addr.playerMaxHp);
    state.player.currentSp = _reader->ReadInt32(baseAddr + _addr.playerCurrentSp);
    state.player.maxSp = _reader->ReadInt32(baseAddr + _addr.playerMaxSp);

    // Identity and location
    state.player.worldX = _reader->ReadInt32(baseAddr + _addr.playerCoordinateX);
    state.player.worldY = _reader->ReadInt32(baseAddr + _addr.playerCoordinateY);
    state.player.name = _reader->ReadString(baseAddr + _addr.playerName, 24);
    
    std::string mapRaw = _reader->ReadString(baseAddr + _addr.mapName, 64);
    size_t rswPos = mapRaw.find(".rsw");
    if (rswPos != std::string::npos) mapRaw = mapRaw.substr(0, rswPos);
    state.player.mapName = mapRaw;

    if (_addr.chatBarEnabled != 0)
        state.player.isChatBarOpen = _reader->ReadUInt32(baseAddr + _addr.chatBarEnabled) != 0;

    // Buffs/Status Buffer (100 uint32_t starting at offset 0x474 from playerCurrentHp)
    uintptr_t statusBase = baseAddr + _addr.playerCurrentHp + 0x474;
    for (int i = 0; i < 100; ++i) {
        state.player.statusBuffer[i] = _reader->ReadUInt32(statusBase + (i * sizeof(uint32_t)));
    }

    // Entities
    state.entities = ReadEntities(baseAddr);

    return true;
}

std::vector<Entity> GameReader::ReadEntities(uintptr_t baseAddr) {
    std::vector<Entity> entities;
    
    uintptr_t wb = _reader->ReadPointer(baseAddr + _addr.worldBaseIntermed);
    if (wb == 0) return entities;

    uintptr_t wb2 = _reader->ReadPointer(wb + _addr.worldBaseOffset);
    if (wb2 == 0) wb2 = wb;

    uintptr_t listHead = wb2 + _addr.entityListOffset;
    uintptr_t first = _reader->ReadPointer(listHead + _addr.entityNodeNext);
    uintptr_t last = _reader->ReadPointer(listHead + _addr.entityNodePrev);

    if (first == 0 || last == 0) return entities;

    uintptr_t current = first;
    int count = 0;
    while (current != 0 && count < 500) {
        uintptr_t entityPtr = _reader->ReadPointer(current + _addr.entityNodeEntityPtr);
        if (entityPtr != 0) {
            Entity ent;
            if (TryReadEntity(entityPtr, ent)) {
                ent.baseAddress = entityPtr;
                entities.push_back(ent);
            }
        }

        if (current == last) break;
        current = _reader->ReadPointer(current + _addr.entityNodeNext);
        count++;
    }

    return entities;
}

bool GameReader::TryReadEntity(uintptr_t entityBase, Entity& entity) {
    entity.id = _reader->ReadInt32(entityBase + _addr.entityId);
    entity.worldX = _reader->ReadInt32(entityBase + _addr.entityWorldX);
    entity.worldY = _reader->ReadInt32(entityBase + _addr.entityWorldY);
    entity.screenX = _reader->ReadInt32(entityBase + _addr.entityScreenX);
    entity.screenY = _reader->ReadInt32(entityBase + _addr.entityScreenY);
    entity.name = _reader->ReadString(entityBase + _addr.entityName, 24);

    // Quick checks for valid entities
    if (entity.id < 0 || entity.id > 1000000) return false;
    if (entity.worldX < 0 || entity.worldX > 1000 || entity.worldY < 0 || entity.worldY > 1000) return false;

    // Categorization (simplified)
    if (entity.id > 1000 && entity.id < 4000) entity.isMonster = true;
    else if (entity.id >= 4000) entity.isNpc = true;
    else if (entity.id > 0 && entity.id < 1000) entity.isPlayer = true;

    return true;
}

} // namespace prt
