#include "ConfigService.h"
#include "../vendor/nlohmann/json.hpp"
#include <fstream>

using json = nlohmann::json;

namespace prt {

void from_json(const json& j, AddressConfig& c) {
    c.mousePosX = j.value("mousePosX", 0xB47F60);
    c.mousePosY = j.value("mousePosY", 0xB47F64);
    // ... add more as needed
}

void from_json(const json& j, MacroBinding& b) {
    b.id = j.value("id", "");
    b.name = j.value("name", "");
    b.isEnabled = j.value("isEnabled", true);
    b.triggerHotkey = j.value("triggerHotkey", "");
    b.inputKey = j.value("inputKey", "");
}

void from_json(const json& j, ClientProfile& p) {
    p.id = j.value("id", "");
    p.displayName = j.value("displayName", "");
    p.isEnabled = j.value("isEnabled", true);
    if (j.contains("bindings")) {
        for (const auto& bj : j["bindings"]) {
            p.bindings.push_back(bj.get<MacroBinding>());
        }
    }
}

void from_json(const json& j, AppConfig& c) {
    c.version = j.value("version", 1);
    c.botEnabled = j.value("botEnabled", false);
    if (j.contains("addresses")) c.addresses = j["addresses"].get<AddressConfig>();
    if (j.contains("clientProfiles")) {
        for (const auto& pj : j["clientProfiles"]) {
            c.clientProfiles.push_back(pj.get<ClientProfile>());
        }
    }
}

bool ConfigService::Load(AppConfig& config, const std::string& path) {
    std::ifstream f(path);
    if (!f.is_open()) return false;
    
    try {
        json j;
        f >> j;
        config = j.get<AppConfig>();
        return true;
    } catch (...) {
        return false;
    }
}

bool ConfigService::Save(const AppConfig& config, const std::string& path) {
    // Implement serialization if needed...
    return false;
}

} // namespace prt
