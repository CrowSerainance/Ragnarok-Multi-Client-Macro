#pragma once
#include <windows.h>
#include <string>
#include <vector>

namespace prt {

class MemoryReader {
public:
    MemoryReader();
    MemoryReader(HANDLE hProcess, uintptr_t baseAddress, uint32_t pid, HANDLE hDriver = INVALID_HANDLE_VALUE);
    ~MemoryReader();

    bool ReadBytes(uintptr_t address, void* buffer, size_t size);
    
    template<typename T>
    T Read(uintptr_t address) {
        T val = {};
        ReadBytes(address, &val, sizeof(T));
        return val;
    }

    uint32_t ReadUInt32(uintptr_t address);
    int32_t ReadInt32(uintptr_t address);
    float ReadFloat(uintptr_t address);
    uintptr_t ReadPointer(uintptr_t address, int offset = 0);
    uintptr_t ResolvePointerChain(uintptr_t baseAddr, const std::vector<int>& offsets, int finalOffset = 0);
    std::string ReadString(uintptr_t address, size_t maxLength = 64);

    bool WriteBytes(uintptr_t address, const void* buffer, size_t size);
    
    template<typename T>
    bool Write(uintptr_t address, const T& value) {
        return WriteBytes(address, &value, sizeof(T));
    }

    uintptr_t GetBaseAddress() const { return _baseAddress; }

private:
    HANDLE _hProcess;
    uintptr_t _baseAddress;
    uint32_t _pid;
    HANDLE _hDriver;
};

} // namespace prt
