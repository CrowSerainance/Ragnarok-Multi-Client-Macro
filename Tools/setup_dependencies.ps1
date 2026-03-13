$imguiVersion = "v1.91.9"
$baseUrl = "https://raw.githubusercontent.com/ocornut/imgui/$imguiVersion"
$imguiDir = "vendor/imgui"

if (!(Test-Path $imguiDir)) { New-Item -ItemType Directory -Path $imguiDir -Force }

$files = @(
    "imgui.h", "imgui.cpp", "imgui_internal.h", "imgui_widgets.cpp", 
    "imgui_draw.cpp", "imgui_tables.cpp", "imstb_rectpack.h", 
    "imstb_textedit.h", "imstb_truetype.h", "imconfig.h"
)

foreach ($file in $files) {
    Write-Host "Downloading $file..."
    Invoke-WebRequest -Uri "$baseUrl/$file" -OutFile "$imguiDir/$file"
}

$backendDir = "$imguiDir/backends"
if (!(Test-Path $backendDir)) { New-Item -ItemType Directory -Path $backendDir -Force }

$backendFiles = @(
    "imgui_impl_win32.h", "imgui_impl_win32.cpp", 
    "imgui_impl_dx11.h", "imgui_impl_dx11.cpp"
)

foreach ($file in $backendFiles) {
    Write-Host "Downloading backend $file..."
    Invoke-WebRequest -Uri "$baseUrl/backends/$file" -OutFile (Join-Path $backendDir $file)
}

# Download JSON
$jsonDir = Join-Path $root "vendor/nlohmann"
if (!(Test-Path $jsonDir)) { New-Item -ItemType Directory -Path $jsonDir -Force }
Write-Host "Downloading nlohmann/json..."
Invoke-WebRequest -Uri "https://raw.githubusercontent.com/nlohmann/json/v3.12.0/json.hpp" -OutFile (Join-Path $jsonDir "json.hpp")
