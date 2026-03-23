#include "FeatureManager.h"

namespace prt {

FeatureManager::FeatureManager(std::shared_ptr<InputInjector> injector)
    : _injector(std::move(injector)) {
    auto now = std::chrono::steady_clock::now();
    _lastAutopotCheck = now;
    _lastAutobuffCheck = now;
    _lastAutobuffSkillsCheck = now;
    _lastAutobuffItemsCheck = now;
    _lastRecoveryCheck = now;
    _lastDebuffRecoveryCheck = now;
}

void FeatureManager::Update(const GameState& state, const ClientProfile& profile) {
    if (!profile.isEnabled || !_injector) return;

    ProcessAutopot(state.player, profile.autopot);
    ProcessAutobuff(state.player, profile.autobuff, _lastAutobuffCheck);
    ProcessAutobuff(state.player, { profile.autobuffSkills.enabled, profile.autobuffSkills.buffs }, _lastAutobuffSkillsCheck);
    ProcessAutobuff(state.player, { profile.autobuffItems.enabled, profile.autobuffItems.buffs }, _lastAutobuffItemsCheck);
    ProcessSpammer(profile.spammer);
    ProcessRecovery(state.player, profile.recovery, _lastRecoveryCheck);
    ProcessDebuffRecovery(state.player, profile.debuffRecovery);
    ProcessSkillTimers(profile.skillTimers);
}

void FeatureManager::ProcessAutopot(const PlayerState& player, const AutopotConfig& config) {
    if (!config.enabled || player.maxHp <= 0 || player.maxSp <= 0) return;

    auto now = std::chrono::steady_clock::now();
    if (std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastAutopotCheck).count() < config.delayMs) {
        return;
    }
    _lastAutopotCheck = now;

    if (config.yggKey != 0 && config.yggThreshold > 0) {
        if ((player.currentHp * 100 / player.maxHp) < config.yggThreshold ||
            (player.currentSp * 100 / player.maxSp) < config.yggThreshold) {
            _injector->SendKey(config.yggKey, 0, 0);
            return;
        }
    }

    if (config.hpKey != 0 && (player.currentHp * 100 / player.maxHp) < config.hpThreshold) {
        _injector->SendKey(config.hpKey, 0, 0);
    }

    if (config.spKey != 0 && (player.currentSp * 100 / player.maxSp) < config.spThreshold) {
        _injector->SendKey(config.spKey, 0, 0);
    }
}

void FeatureManager::ProcessAutobuff(const PlayerState& player, const AutobuffConfig& config, std::chrono::steady_clock::time_point& lastCheck) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    if (std::chrono::duration_cast<std::chrono::milliseconds>(now - lastCheck).count() < 1000) {
        return;
    }
    lastCheck = now;

    for (const auto& buff : config.buffs) {
        if (buff.enabled && buff.key != 0 && buff.statusId != 0 && !IsStatusActive(player, buff.statusId)) {
            _injector->SendKey(buff.key, 0, 100);
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

void FeatureManager::ProcessRecovery(const PlayerState& player, const StatusRecoveryConfig& config, std::chrono::steady_clock::time_point& lastCheck) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    if (std::chrono::duration_cast<std::chrono::milliseconds>(now - lastCheck).count() < 500) {
        return;
    }
    lastCheck = now;

    for (const auto& rec : config.recoveries) {
        if (rec.enabled && rec.key != 0 && rec.statusId != 0 && IsStatusActive(player, rec.statusId)) {
            _injector->SendKey(rec.key, 0, 0);
        }
    }
}

void FeatureManager::ProcessDebuffRecovery(const PlayerState& player, const DebuffRecoveryConfig& config) {
    if (!config.enabled) return;

    StatusRecoveryConfig recoveryConfig;
    recoveryConfig.enabled = config.enabled;
    recoveryConfig.recoveries = config.recoveries;
    ProcessRecovery(player, recoveryConfig, _lastDebuffRecoveryCheck);

    bool anyDebuff = false;
    for (const auto& rec : config.recoveries) {
        if (rec.enabled && rec.statusId != 0 && IsStatusActive(player, rec.statusId)) {
            anyDebuff = true;
            break;
        }
    }

    if (!anyDebuff) return;

    if (config.groupStatusKey != 0) {
        _injector->SendKey(config.groupStatusKey, 0, 50);
    }

    if (config.groupNewStatusKey != 0) {
        _injector->SendKey(config.groupNewStatusKey, 0, 50);
    }
}

void FeatureManager::ProcessSkillTimers(const SkillTimerConfig& config) {
    if (!config.enabled) return;

    auto now = std::chrono::steady_clock::now();
    const SkillTimerEntry entries[] = { config.timer1, config.timer2, config.timer3 };
    for (const auto& entry : entries) {
        if (!entry.enabled || entry.key == 0) continue;

        int delayMs = entry.delaySeconds * 1000;
        if (std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastSkillTimerTime[entry.key]).count() >= delayMs) {
            _injector->SendKey(entry.key, 0, 0);
            _lastSkillTimerTime[entry.key] = now;
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
