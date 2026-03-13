#pragma once
#include <string>
#include <map>

namespace prt {

bool TryGetVirtualKey(const std::string& keyName, int& vk);
std::string NormalizeHotkey(const std::string& hotkey);

} // namespace prt
