#pragma once
#include <windows.h>
#include <vector>
#include <cstdint>

namespace prt {

class InputInjector {
public:
    InputInjector();
    
    // Key injection
    void SendKey(uint16_t vk, uint16_t scanCode, uint32_t delayMs);
    
    // Mouse click injection
    void SendClick(int x, int y, uint32_t mousePosXOffset, uint32_t mousePosYOffset);

private:
    HWND _gameHwnd = NULL;
    uintptr_t _baseAddress = 0;

    void EnsureInitialized();
    HWND FindGameWindow();
};

} // namespace prt
