#include "MemoryReader.h"
#include <winioctl.h>
#include <vector>

#define IOCTL_READ_MEMORY CTL_CODE(FILE_DEVICE_UNKNOWN, 0x800, METHOD_BUFFERED, FILE_ANY_ACCESS)
#define IOCTL_WRITE_MEMORY CTL_CODE(FILE_DEVICE_UNKNOWN, 0x801, METHOD_BUFFERED, FILE_ANY_ACCESS)

namespace prt {

#pragma pack(push, 1)
struct READ_MEM_REQ {
    uint32_t ProcessId;
    uint64_t TargetAddress;
    uint64_t Size;
};

struct WRITE_MEM_REQ {
    uint32_t ProcessId;
    uint64_t TargetAddress;
    uint64_t Size;
};
#pragma pack(pop)

MemoryReader::MemoryReader()
    : _hProcess(GetCurrentProcess()),
      _baseAddress((uintptr_t)GetModuleHandle(NULL)),
      _pid(GetCurrentProcessId()),
      _hDriver(INVALID_HANDLE_VALUE) {}

MemoryReader::MemoryReader(HANDLE hProcess, uintptr_t baseAddress, uint32_t pid, HANDLE hDriver)
    : _hProcess(hProcess), _baseAddress(baseAddress), _pid(pid), _hDriver(hDriver) {}

MemoryReader::~MemoryReader() {}

bool MemoryReader::ReadBytes(uintptr_t address, void* buffer, size_t size) {
    if (_hDriver != INVALID_HANDLE_VALUE) {
        READ_MEM_REQ req = { _pid, (uint64_t)address, (uint64_t)size };
        DWORD bytesReturned = 0;
        return DeviceIoControl(_hDriver, IOCTL_READ_MEMORY, &req, sizeof(req), buffer, (DWORD)size, &bytesReturned, NULL);
    }
    SIZE_T bytesRead = 0;
    return ReadProcessMemory(_hProcess, (LPCVOID)address, buffer, size, &bytesRead) && bytesRead == size;
}

uint32_t MemoryReader::ReadUInt32(uintptr_t address) {
    return Read<uint32_t>(address);
}

int32_t MemoryReader::ReadInt32(uintptr_t address) {
    return Read<int32_t>(address);
}

float MemoryReader::ReadFloat(uintptr_t address) {
    return Read<float>(address);
}

uintptr_t MemoryReader::ReadPointer(uintptr_t address, int offset) {
    return (uintptr_t)Read<uint32_t>(address + offset);
}

uintptr_t MemoryReader::ResolvePointerChain(uintptr_t baseAddr, const std::vector<int>& offsets, int finalOffset) {
    uintptr_t addr = baseAddr;
    for (int offset : offsets) {
        addr = ReadPointer(addr + offset);
        if (addr == 0) return 0;
    }
    return addr + finalOffset;
}

std::string MemoryReader::ReadString(uintptr_t address, size_t maxLength) {
    std::vector<char> buf(maxLength);
    if (!ReadBytes(address, buf.data(), maxLength)) return "";
    buf.push_back('\0');
    return std::string(buf.data());
}

bool MemoryReader::WriteBytes(uintptr_t address, const void* buffer, size_t size) {
    if (_hDriver != INVALID_HANDLE_VALUE) {
        std::vector<uint8_t> ioBuffer(sizeof(WRITE_MEM_REQ) + size);
        auto* req = reinterpret_cast<WRITE_MEM_REQ*>(ioBuffer.data());
        req->ProcessId = _pid;
        req->TargetAddress = (uint64_t)address;
        req->Size = (uint64_t)size;
        memcpy(ioBuffer.data() + sizeof(WRITE_MEM_REQ), buffer, size);

        DWORD bytesReturned = 0;
        return DeviceIoControl(_hDriver, IOCTL_WRITE_MEMORY, ioBuffer.data(), (DWORD)ioBuffer.size(), NULL, 0, &bytesReturned, NULL);
    }
    SIZE_T bytesWritten = 0;
    return WriteProcessMemory(_hProcess, (LPVOID)address, buffer, size, &bytesWritten) && bytesWritten == size;
}

} // namespace prt
