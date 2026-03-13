cmake_minimum_required(VERSION 3.15)
project(PersonalRagnarokTool)

set(CMAKE_CXX_STANDARD 20)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Unified Internal DLL
add_library(PersonalRagnarokTool SHARED
    Source/InternalMain.cpp
    Source/Dx11Hook.cpp
    Source/Menu.cpp
    Source/ConfigService.cpp
    Source/MemoryReader.cpp
    Source/VirtualKeyMap.cpp
    
    # Bot Logic
    Source/Bot/GameReader.cpp
    Source/Bot/BotFsm.cpp
    
    # Actuator
    Source/Actuator/InputInjector.cpp
    
    # ImGui (Assume downloaded via setup_dependencies.ps1)
    vendor/imgui/imgui.cpp
    vendor/imgui/imgui_draw.cpp
    vendor/imgui/imgui_widgets.cpp
    vendor/imgui/imgui_tables.cpp
    vendor/imgui/backends/imgui_impl_win32.cpp
    vendor/imgui/backends/imgui_impl_dx11.cpp
)

target_include_directories(PersonalRagnarokTool PRIVATE
    Source
    Source/Bot
    Source/Actuator
    vendor
    vendor/imgui
    vendor/imgui/backends
    vendor/nlohmann
)

target_link_libraries(PersonalRagnarokTool PRIVATE
    d3d11
    dxgi
)
