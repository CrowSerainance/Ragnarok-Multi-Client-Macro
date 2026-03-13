#pragma once
#include <string>
#include "Types.h"

namespace prt {

class ConfigService {
public:
    static bool Load(AppConfig& config, const std::string& path);
    static bool Save(const AppConfig& config, const std::string& path);
};

} // namespace prt
