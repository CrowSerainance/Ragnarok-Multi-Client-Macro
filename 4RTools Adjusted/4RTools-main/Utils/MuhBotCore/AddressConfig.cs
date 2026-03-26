using Newtonsoft.Json;

namespace _4RTools.Utils.MuhBotCore;

/// <summary>
/// Memory offsets (hex). Defaults from Ragexe/ragna4th; user updates for Muh.exe via scanner or manual edit.
/// </summary>
public class AddressConfig
{
    /// <summary>
    /// Mouse position memory offsets. Default 0 = PostMessage-only mode (safe for any server).
    /// Set to actual offsets (e.g. 0xB47F60/64 for Ragna4th) to enable memory-write mouse positioning.
    /// </summary>
    [JsonProperty("mousePosX")]
    public int MousePosX { get; set; } = 0;

    [JsonProperty("mousePosY")]
    public int MousePosY { get; set; } = 0;

    [JsonProperty("chatBarEnabled")]
    public int ChatBarEnabled { get; set; } = 0xB81390;

    [JsonProperty("worldBaseIntermed")]
    public int WorldBaseIntermed { get; set; } = 0xB3D1D4;

    [JsonProperty("playerName")]
    public int PlayerName { get; set; } = 0xDD43E8;

    [JsonProperty("playerCurrentHp")]
    public int PlayerCurrentHp { get; set; } = 0xDD1A04;

    [JsonProperty("playerMaxHp")]
    public int PlayerMaxHp { get; set; } = 0xDD1A08;

    [JsonProperty("playerCurrentSp")]
    public int PlayerCurrentSp { get; set; } = 0xDD1A0C;

    [JsonProperty("playerMaxSp")]
    public int PlayerMaxSp { get; set; } = 0xDD1A10;

    [JsonProperty("playerCurrentWeight")]
    public int PlayerCurrentWeight { get; set; } = 0;

    [JsonProperty("playerMaxWeight")]
    public int PlayerMaxWeight { get; set; } = 0;

    [JsonProperty("playerCoordinateX")]
    public int PlayerCoordinateX { get; set; } = 0xDBA5A0;

    [JsonProperty("playerCoordinateY")]
    public int PlayerCoordinateY { get; set; } = 0xDBA5A4;

    [JsonProperty("mapName")]
    public int MapName { get; set; } = 0xB3D1D8;

    [JsonProperty("worldBaseOffset")]
    public int WorldBaseOffset { get; set; } = 0xCC;

    [JsonProperty("playerBaseOffset")]
    public int PlayerBaseOffset { get; set; } = 0x2C;

    [JsonProperty("entityListOffset")]
    public int EntityListOffset { get; set; } = 0x10;

    [JsonProperty("entityNodeNext")]
    public int EntityNodeNext { get; set; } = 0x00;

    [JsonProperty("entityNodePrev")]
    public int EntityNodePrev { get; set; } = 0x04;

    [JsonProperty("entityNodeEntityPtr")]
    public int EntityNodeEntityPtr { get; set; } = 0x08;

    [JsonProperty("entityId")]
    public int EntityId { get; set; } = 0x10C;

    [JsonProperty("entityType")]
    public int EntityType { get; set; } = 0;

    [JsonProperty("entityWorldX")]
    public int EntityWorldX { get; set; } = 0x16C;

    [JsonProperty("entityWorldY")]
    public int EntityWorldY { get; set; } = 0x170;

    [JsonProperty("entityScreenX")]
    public int EntityScreenX { get; set; } = 0xAC;

    [JsonProperty("entityScreenY")]
    public int EntityScreenY { get; set; } = 0xB0;

    [JsonProperty("entityName")]
    public int EntityName { get; set; } = 0x30;

    [JsonProperty("entityCurrentHp")]
    public int EntityCurrentHp { get; set; } = 0;

    [JsonProperty("entityMaxHp")]
    public int EntityMaxHp { get; set; } = 0;

    [JsonProperty("playerStateOffset")]
    public int PlayerStateOffset { get; set; } = 0x70;

    [JsonProperty("playerScreenX")]
    public int PlayerScreenX { get; set; } = 0xAC;

    [JsonProperty("playerScreenY")]
    public int PlayerScreenY { get; set; } = 0xB0;

    [JsonProperty("viewOffset")]
    public int ViewOffset { get; set; } = 0xD0;

    [JsonProperty("cameraAngleHorizontal")]
    public int CameraAngleHorizontal { get; set; } = 0x48;

    [JsonProperty("cameraAngleVertical")]
    public int CameraAngleVertical { get; set; } = 0x44;

    [JsonProperty("cameraZoom")]
    public int CameraZoom { get; set; } = 0x4C;

    [JsonProperty("isTalkingToNpc")]
    public int IsTalkingToNpc { get; set; } = 0x258;

    [JsonProperty("inventoryWindowOpen")]
    public int InventoryWindowOpen { get; set; } = 0;

    [JsonProperty("storageWindowOpen")]
    public int StorageWindowOpen { get; set; } = 0;

    [JsonProperty("cartWindowOpen")]
    public int CartWindowOpen { get; set; } = 0;

    [JsonProperty("equipmentWindowOpen")]
    public int EquipmentWindowOpen { get; set; } = 0;

    [JsonProperty("skillWindowOpen")]
    public int SkillWindowOpen { get; set; } = 0;

    [JsonProperty("partyCount")]
    public int PartyCount { get; set; } = 0;

    [JsonProperty("partyListBase")]
    public int PartyListBase { get; set; } = 0;

    [JsonProperty("partyEntryStride")]
    public int PartyEntryStride { get; set; } = 0;

    [JsonProperty("partyMemberIdOffset")]
    public int PartyMemberIdOffset { get; set; } = 0;

    [JsonProperty("partyMemberNameOffset")]
    public int PartyMemberNameOffset { get; set; } = 0;

    [JsonProperty("partyMemberMapOffset")]
    public int PartyMemberMapOffset { get; set; } = 0;

    [JsonProperty("partyMemberXOffset")]
    public int PartyMemberXOffset { get; set; } = 0;

    [JsonProperty("partyMemberYOffset")]
    public int PartyMemberYOffset { get; set; } = 0;

    [JsonProperty("partyMemberOnlineOffset")]
    public int PartyMemberOnlineOffset { get; set; } = 0;

    /// <summary>
    /// Offset from HP base to buff array (stable across Ragexe). Used by scanner.
    /// </summary>
    [JsonProperty("buffArrayOffsetFromHpBase")]
    public int BuffArrayOffsetFromHpBase { get; set; } = 0x474;
}
