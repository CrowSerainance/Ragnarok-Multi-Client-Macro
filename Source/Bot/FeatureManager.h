#pragma once
#include <memory>
#include <chrono>
#include "../Types.h"
#include "../Actuator/InputInjector.h"

namespace prt {

class FeatureManager {
public:
    FeatureManager(std::shared_ptr<InputInjector> injector);

    void Update(const GameState& state, const ClientProfile& profile);

private:
    std::shared_ptr<InputInjector> _injector;
    
    std::chrono::steady_clock::time_point _lastAutopotCheck;
    std::chrono::steady_clock::time_point _lastAutobuffCheck;
    std::chrono::steady_clock::time_point _lastAutobuffSkillsCheck;
    std::chrono::steady_clock::time_point _lastAutobuffItemsCheck;
    std::chrono::steady_clock::time_point _lastRecoveryCheck;
    std::chrono::steady_clock::time_point _lastDebuffRecoveryCheck;
    std::map<uint16_t, std::chrono::steady_clock::time_point> _lastSpamTime;
    std::map<uint16_t, std::chrono::steady_clock::time_point> _lastSkillTimerTime;

    void ProcessAutopot(const PlayerState& player, const AutopotConfig& config);
    void ProcessAutobuff(const PlayerState& player, const AutobuffConfig& config, std::chrono::steady_clock::time_point& lastCheck);
    void ProcessSpammer(const SpammerConfig& config);
    void ProcessRecovery(const PlayerState& player, const StatusRecoveryConfig& config, std::chrono::steady_clock::time_point& lastCheck);
    void ProcessDebuffRecovery(const PlayerState& player, const DebuffRecoveryConfig& config);
    void ProcessSkillTimers(const SkillTimerConfig& config);

    bool IsStatusActive(const PlayerState& player, uint32_t statusId);
};

} // namespace prt
