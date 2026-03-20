#include "FeatureManager.h"

namespace prt {

FeatureManager::FeatureManager(std::shared_ptr<InputInjector> injector)
    : _injector(injector) {
    _lastAutopotCheck = std::chrono::steady_clock::now();
    _lastAutobuffCheck = std::chrono::steady_clock::now();
    _lastRecoveryCheck = std::chrono::steady_clock::now();
}

void FeatureManager::Update(const GameState& state, const ClientProfile& profile) {
    if (!profile.isEnabled) return;

    ProcessAutopot(state.player, profile.autopot);
    ProcessAutobuff(state.player, profile.autobuff);
    ProcessSpammer(profile.spammer);
    ProcessRecovery(state.player, profile.recovery);
}

void FeatureManager::ProcessAutopot(const PlayerState& player, const AutopotConfig& config) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    if (std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastAutopotCheck).count() < config.delayMs) {
        return;
    }
    _lastAutopotCheck = now;

    // Yggdrasil (Special Check)
    if (config.yggKey != 0 && config.yggThreshold > 0) {
        if (player.currentHp * 100 / player.maxHp < config.yggThreshold ||
            player.currentSp * 100 / player.maxSp < config.yggThreshold) {
            _injector->SendKey(config.yggKey, 0, 0);
            return; // Don't use other pots if Ygg was used
        }
    }

    // HP Pot
    if (config.hpKey != 0 && player.currentHp * 100 / player.maxHp < config.hpThreshold) {
        _injector->SendKey(config.hpKey, 0, 0);
    }

    // SP Pot
    if (config.spKey != 0 && player.currentSp * 100 / player.maxSp < config.spThreshold) {
        _injector->SendKey(config.spKey, 0, 0);
    }
}

void FeatureManager::ProcessAutobuff(const PlayerState& player, const AutobuffConfig& config) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    if (std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastAutobuffCheck).count() < 1000) {
        return;
    }
    _lastAutobuffCheck = now;

    for (const auto& buff : config.buffs) {
        if (buff.enabled && buff.key != 0 && buff.statusId != 0) {
            if (!IsStatusActive(player, buff.statusId)) {
                _injector->SendKey(buff.key, 0, 100); // 100ms delay between buffs
            }
        }
    }
}

void FeatureManager::ProcessSpammer(const SpammerConfig& config) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    for (const auto& spam : config.keys) {
        if (spam.enabled && spam.key != 0) {
            if (std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastSpamTime[spam.key]).count() >= spam.intervalMs) {
                _injector->SendKey(spam.key, 0, 0);
                _lastSpamTime[spam.key] = now;
            }
        }
    }
}

void FeatureManager::ProcessRecovery(const PlayerState& player, const StatusRecoveryConfig& config) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    if (std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastRecoveryCheck).count() < 500) {
        return;
    }
    _lastRecoveryCheck = now;

    for (const auto& rec : config.recoveries) {
        if (rec.enabled && rec.key != 0 && rec.statusId != 0) {
            if (IsStatusActive(player, rec.statusId)) {
                _injector->SendKey(rec.key, 0, 0);
            }
        }
    }
}

bool FeatureManager::IsStatusActive(const PlayerState& player, uint32_t statusId) {
    for (int i = 0; i < 100; ++i) {
        if (player.statusBuffer[i] == statusId) return true;
    }
    return false;
}

} // namespace prt
