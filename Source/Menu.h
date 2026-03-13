#pragma once
#include <windows.h>
#include <vector>
#include "Types.h"

namespace prt {

class Menu {
public:
    static void Render(AppConfig& config);
    static bool IsOpen();
    static void Toggle();

private:
    static bool _isOpen;
};

} // namespace prt
