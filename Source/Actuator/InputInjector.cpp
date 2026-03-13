#include "InputInjector.h"
#include <ctime>
#include <thread>
#include <chrono>

namespace prt {

InputInjector::InputInjector() {
    _baseAddress = (uintptr_t)GetModuleHandle(NULL);
}

void InputInjector::EnsureInitialized() {
    if (_gameHwnd == NULL || !IsWindow(_gameHwnd)) {
        _gameHwnd = FindGameWindow();
    }
}

HWND InputInjector::FindGameWindow() {
    // Standard RO window searching
    HWND hwnd = GetTopWindow(NULL);
    DWORD pid = GetCurrentProcessId();
    while (hwnd) {
        DWORD windowPid;
        GetWindowThreadProcessId(hwnd, &windowPid);
        if (windowPid == pid) {
            char className[256];
            GetClassNameA(hwnd, className, sizeof(className));
            if (strcmp(className, "Ragnarok") == 0) return hwnd;
            // Fallback for some clients
            if (strcmp(className, "GrfEditor") != 0 && GetWindowTextLengthA(hwnd) > 0) return hwnd;
        }
        hwnd = GetNextWindow(hwnd, GW_HWNDNEXT);
    }
    return GetForegroundWindow(); // Last resort
}

void InputInjector::SendKey(uint16_t vk, uint16_t scanCode, uint32_t delayMs) {
    EnsureInitialized();
    if (!_gameHwnd) return;

    if (scanCode == 0) scanCode = (uint16_t)MapVirtualKeyW(vk, MAPVK_VK_TO_VSC);

    LPARAM downLp = 0x00000001 | (LPARAM)(scanCode << 16);
    LPARAM upLp   = 0xC0000001 | (LPARAM)(scanCode << 16);

    PostMessageW(_gameHwnd, WM_KEYDOWN, vk, downLp);
    std::this_thread::sleep_for(std::chrono::milliseconds(25 + (rand() % 20)));
    PostMessageW(_gameHwnd, WM_KEYUP, vk, upLp);

    if (delayMs > 0) {
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));
    }
}

void InputInjector::SendClick(int x, int y, uint32_t mousePosXOffset, uint32_t mousePosYOffset) {
    EnsureInitialized();
    if (!_gameHwnd) return;

    // Direct memory write for mouse position (bypasses most anti-cheats)
    if (mousePosXOffset > 0 && mousePosYOffset > 0) {
        *(int32_t*)(_baseAddress + mousePosXOffset) = x;
        *(int32_t*)(_baseAddress + mousePosYOffset) = y;
    }

    LPARAM lp = MAKELPARAM(x, y);

    // Send the click messages
    PostMessageW(_gameHwnd, WM_MOUSEMOVE, 0, lp);
    std::this_thread::sleep_for(std::chrono::milliseconds(10));
    PostMessageW(_gameHwnd, WM_LBUTTONDOWN, MK_LBUTTON, lp);
    std::this_thread::sleep_for(std::chrono::milliseconds(30 + (rand() % 20)));
    PostMessageW(_gameHwnd, WM_LBUTTONUP, 0, lp);
}

} // namespace prt
