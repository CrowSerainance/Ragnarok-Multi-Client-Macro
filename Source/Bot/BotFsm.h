#pragma once
#include <string>
#include "../Core/Types.h"

namespace prt {

enum class BotState {
    Idle,
    Searching,
    Approaching,
    Attacking,
    Looting,
    Recovering,
};

class BotFsm {
public:
    BotFsm();

    BotState DetermineState(const GameState& state, const ClientProfile& profile);
    
    std::string GetStateName(BotState state);

private:
    BotState _currentState = BotState::Idle;
    int _targetId = 0;
};

} // namespace prt
