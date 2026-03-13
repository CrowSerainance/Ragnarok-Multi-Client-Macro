param(
    [string]$Configuration = "Release",
    [string]$Runtime = "win-x86"
)

$ErrorActionPreference = "Stop"

$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$project = Join-Path $root "PersonalRagnarokTool\PersonalRagnarokTool.csproj"
$output = Join-Path $root "Publish\$Runtime"

dotnet publish $project -c $Configuration -r $Runtime --self-contained true -o $output
Write-Host "Published to $output"
