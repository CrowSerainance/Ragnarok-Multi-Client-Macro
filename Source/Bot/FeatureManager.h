#pragma once
#include <memory>
#include <chrono>
#include "../Core/Types.h"
#include "../Actuator/InputInjector.h"

namespace prt {

class FeatureManager {
public:
    FeatureManager(std::shared_ptr<InputInjector> injector);

    void Update(const GameState& state, const ClientProfile& profile);

private:
    std::shared_ptr<InputInjector> _injector;
    
    // Timers for each feature to control frequency
    std::chrono::steady_clock::time_point _lastAutopotCheck;
    std::chrono::steady_clock::time_point _lastAutobuffCheck;
    std::chrono::steady_clock::time_point _lastRecoveryCheck;
    std::map<uint16_t, std::chrono::steady_clock::time_point> _lastSpamTime;

    void ProcessAutopot(const PlayerState& player, const AutopotConfig& config);
    void ProcessAutobuff(const PlayerState& player, const AutobuffConfig& config);
    void ProcessSpammer(const SpammerConfig& config);
    void ProcessRecovery(const PlayerState& player, const StatusRecoveryConfig& config);

    bool IsStatusActive(const PlayerState& player, uint32_t statusId);
};

} // namespace prt
