#include "Menu.h"
#include "../vendor/imgui/imgui.h"

namespace prt {

bool Menu::_isOpen = true;

bool Menu::IsOpen() { return _isOpen; }
void Menu::Toggle() { _isOpen = !_isOpen; }

void Menu::Render(AppConfig& config) {
    ImGui::SetNextWindowSize(ImVec2(600, 400), ImGuiCond_FirstUseEver);
    if (ImGui::Begin("Personal Ragnarok Tool - Internal", &_isOpen, ImGuiWindowFlags_MenuBar)) {
        if (ImGui::BeginMenuBar()) {
            if (ImGui::BeginMenu("File")) {
                if (ImGui::MenuItem("Save Config")) {
                    // ConfigService::Save(...)
                }
                ImGui::EndMenu();
            }
            ImGui::EndMenuBar();
        }

        ImGui::Checkbox("Enable Bot", &config.botEnabled);
        ImGui::Separator();

        if (ImGui::BeginTabBar("Tabs")) {
            if (ImGui::BeginTabItem("Profiles")) {
                for (auto& profile : config.clientProfiles) {
                    if (ImGui::CollapsingHeader(profile.displayName.c_str())) {
                        ImGui::Checkbox("Enabled", &profile.isEnabled);
                        // Add more profile settings here
                    }
                }
                ImGui::EndTabItem();
            }
            if (ImGui::BeginTabItem("Bot Debug")) {
                ImGui::Text("Player Position: (%d, %d)", 0, 0); // Fill with actual data
                ImGui::EndTabItem();
            }
            ImGui::EndTabBar();
        }
    }
    ImGui::End();
}

} // namespace prt
