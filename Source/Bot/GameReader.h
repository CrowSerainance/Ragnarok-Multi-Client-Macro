#pragma once
#include <vector>
#include <memory>
#include "../Core/Types.h"
#include "../Core/MemoryReader.h"

namespace prt {

class GameReader {
public:
    GameReader(std::shared_ptr<MemoryReader> reader, const AddressConfig& addr);

    bool TryRead(GameState& state);

private:
    std::shared_ptr<MemoryReader> _reader;
    AddressConfig _addr;

    std::vector<Entity> ReadEntities(uintptr_t baseAddr);
    bool TryReadEntity(uintptr_t entityBase, Entity& entity);
};

} // namespace prt
