#include "VirtualKeyMap.h"
#include <algorithm>
#include <windows.h>

namespace prt {

static std::map<std::string, int> KeyTable = {
    {"F1", VK_F1}, {"F2", VK_F2}, {"F3", VK_F3}, {"F4", VK_F4},
    {"F5", VK_F5}, {"F6", VK_F6}, {"F7", VK_F7}, {"F8", VK_F8},
    {"F9", VK_F9}, {"F10", VK_F10}, {"F11", VK_F11}, {"F12", VK_F12},
    {"1", '1'}, {"2", '2'}, {"3", '3'}, {"4", '4'}, {"5", '5'},
    {"6", '6'}, {"7", '7'}, {"8", '8'}, {"9", '9'}, {"0", '0'},
    {"Q", 'Q'}, {"W", 'W'}, {"E", 'E'}, {"R", 'R'},
    {"A", 'A'}, {"S", 'S'}, {"D", 'D'}, {"F", 'F'}
};

bool TryGetVirtualKey(const std::string& keyName, int& vk) {
    auto it = KeyTable.find(keyName);
    if (it != KeyTable.end()) {
        vk = it->second;
        return true;
    }
    return false;
}

std::string NormalizeHotkey(const std::string& hotkey) {
    std::string s = hotkey;
    std::transform(s.begin(), s.end(), s.begin(), ::toupper);
    return s;
}

} // namespace prt
