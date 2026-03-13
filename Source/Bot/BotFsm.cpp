#include "BotFsm.h"
#include <cmath>
#include <algorithm>

namespace prt {

BotFsm::BotFsm() {}

BotState BotFsm::DetermineState(const GameState& state, const ClientProfile& profile) {
    // 1. Safety first: check HP
    if (state.player.maxHp > 0) {
        float hpPct = (float)state.player.currentHp / state.player.maxHp;
        if (hpPct < 0.3f) return BotState::Recovering;
    }

    // 2. Logic based on entities
    bool hasTarget = false;
    const Entity* closestMob = nullptr;
    float minDist = 9999.0f;

    for (const auto& ent : state.entities) {
        if (ent.isMonster && ent.currentHp > 0) {
            float dist = std::sqrt(std::pow(ent.worldX - state.player.worldX, 2) + 
                                   std::pow(ent.worldY - state.player.worldY, 2));
            if (dist < minDist) {
                minDist = dist;
                closestMob = &ent;
            }
        }
    }

    if (closestMob) {
        if (minDist <= 2.0f) return BotState::Attacking;
        else return BotState::Approaching;
    }

    return BotState::Searching;
}

std::string BotFsm::GetStateName(BotState state) {
    switch (state) {
        case BotState::Idle: return "Idle";
        case BotState::Searching: return "Searching";
        case BotState::Approaching: return "Approaching";
        case BotState::Attacking: return "Attacking";
        case BotState::Looting: return "Looting";
        case BotState::Recovering: return "Recovering";
        default: return "Unknown";
    }
}

} // namespace prt
