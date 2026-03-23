#include "InputInjector.h"
#include <chrono>
#include <cstring>
#include <thread>

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
    HWND hwnd = GetTopWindow(NULL);
    DWORD pid = GetCurrentProcessId();
    while (hwnd) {
        DWORD windowPid = 0;
        GetWindowThreadProcessId(hwnd, &windowPid);
        if (windowPid == pid) {
            char className[256] = {};
            GetClassNameA(hwnd, className, sizeof(className));
            if (strcmp(className, "Ragnarok") == 0) return hwnd;
            if (strcmp(className, "GrfEditor") != 0 && GetWindowTextLengthA(hwnd) > 0) return hwnd;
        }
        hwnd = GetNextWindow(hwnd, GW_HWNDNEXT);
    }
    return GetForegroundWindow();
}

HWND InputInjector::ResolveInputWindow() const {
    if (_gameHwnd != NULL && IsWindow(_gameHwnd)) {
        return _gameHwnd;
    }

    return GetForegroundWindow();
}

void InputInjector::FocusGameWindow(HWND hwnd) const {
    if (hwnd == NULL || !IsWindow(hwnd)) {
        return;
    }

    if (IsIconic(hwnd)) {
        ShowWindow(hwnd, SW_RESTORE);
    }

    SetForegroundWindow(hwnd);
    BringWindowToTop(hwnd);
    SetActiveWindow(hwnd);
    SetFocus(hwnd);
    std::this_thread::sleep_for(std::chrono::milliseconds(15));
}

void InputInjector::SendKey(uint16_t vk, uint16_t scanCode, uint32_t delayMs) {
    EnsureInitialized();
    HWND hwnd = ResolveInputWindow();
    if (hwnd == NULL || vk == 0) return;

    if (scanCode == 0) {
        scanCode = (uint16_t)MapVirtualKeyW(vk, MAPVK_VK_TO_VSC);
    }

    HWND previousForeground = GetForegroundWindow();
    FocusGameWindow(hwnd);

    INPUT inputs[2] = {};
    inputs[0].type = INPUT_KEYBOARD;
    inputs[0].ki.wVk = vk;
    inputs[0].ki.wScan = scanCode;

    inputs[1].type = INPUT_KEYBOARD;
    inputs[1].ki.wVk = vk;
    inputs[1].ki.wScan = scanCode;
    inputs[1].ki.dwFlags = KEYEVENTF_KEYUP;

    SendInput(1, &inputs[0], sizeof(INPUT));
    std::this_thread::sleep_for(std::chrono::milliseconds(25));
    SendInput(1, &inputs[1], sizeof(INPUT));

    if (delayMs > 0) {
        std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));
    }

    if (previousForeground != NULL && previousForeground != hwnd && IsWindow(previousForeground)) {
        SetForegroundWindow(previousForeground);
    }
}

void InputInjector::SendClick(int x, int y, uint32_t mousePosXOffset, uint32_t mousePosYOffset) {
    EnsureInitialized();
    HWND hwnd = ResolveInputWindow();
    if (hwnd == NULL) return;

    if (mousePosXOffset > 0 && mousePosYOffset > 0) {
        *(int32_t*)(_baseAddress + mousePosXOffset) = x;
        *(int32_t*)(_baseAddress + mousePosYOffset) = y;
    }

    POINT clientPoint = { x, y };
    if (!ClientToScreen(hwnd, &clientPoint)) {
        return;
    }

    POINT previousCursor = {};
    GetCursorPos(&previousCursor);

    HWND previousForeground = GetForegroundWindow();
    FocusGameWindow(hwnd);
    SetCursorPos(clientPoint.x, clientPoint.y);
    std::this_thread::sleep_for(std::chrono::milliseconds(10));

    INPUT inputs[2] = {};
    inputs[0].type = INPUT_MOUSE;
    inputs[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;

    inputs[1].type = INPUT_MOUSE;
    inputs[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;

    SendInput(1, &inputs[0], sizeof(INPUT));
    std::this_thread::sleep_for(std::chrono::milliseconds(30));
    SendInput(1, &inputs[1], sizeof(INPUT));

    SetCursorPos(previousCursor.x, previousCursor.y);
    if (previousForeground != NULL && previousForeground != hwnd && IsWindow(previousForeground)) {
        SetForegroundWindow(previousForeground);
    }
}

} // namespace prt
