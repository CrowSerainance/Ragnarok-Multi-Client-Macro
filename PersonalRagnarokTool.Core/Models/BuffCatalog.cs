namespace PersonalRagnarokTool.Core.Models;

public sealed record BuffDefinition(string Name, uint StatusId, string Category);

public static class BuffCatalog
{
    // ─── SKILL BUFFS ───────────────────────────────────────────

    public static List<BuffDefinition> GetArcherSkills() =>
    [
        new("Concentration", (uint)EffectStatusIDs.CONCENTRATION, "Archer"),
        new("Wind Walk", (uint)EffectStatusIDs.WINDWALK, "Archer"),
        new("True Sight", (uint)EffectStatusIDs.TRUESIGHT, "Archer"),
        new("Unlimited", (uint)EffectStatusIDs.UNLIMIT, "Archer"),
        new("A Poem of Bragi", (uint)EffectStatusIDs.POEMBRAGI, "Archer"),
        new("Windmill Rush", (uint)EffectStatusIDs.RUSH_WINDMILL, "Archer"),
        new("Moonlight Serenade", (uint)EffectStatusIDs.MOONLIT_SERENADE, "Archer"),
        new("Frigg's Song", (uint)EffectStatusIDs.FRIGG_SONG, "Archer"),
        new("Mystic Symphony", (uint)EffectStatusIDs.EFST_MYSTIC_SYMPHONY, "Archer"),
        new("Jawaii Serenade", (uint)EffectStatusIDs.EFST_JAWAII_SERENADE, "Archer"),
        new("Musical Interlude", (uint)EffectStatusIDs.EFST_MUSICAL_INTERLUDE, "Archer"),
        new("Prontera March", (uint)EffectStatusIDs.EFST_PRON_MARCH, "Archer"),
        new("Swing Dance", (uint)EffectStatusIDs.EFST_SWING, "Archer"),
        new("Calamity Gale", (uint)EffectStatusIDs.EFST_CALAMITYGALE, "Archer"),
        new("Fear Breeze", (uint)EffectStatusIDs.EFST_FEARBREEZE, "Archer"),
    ];

    public static List<BuffDefinition> GetSwordmanSkills() =>
    [
        new("Endure", (uint)EffectStatusIDs.ENDURE, "Swordman"),
        new("Auto Berserk", (uint)EffectStatusIDs.AUTOBERSERK, "Swordman"),
        new("Guard", (uint)EffectStatusIDs.AUTOGUARD, "Swordman"),
        new("Shield Reflection", (uint)EffectStatusIDs.REFLECTSHIELD, "Swordman"),
        new("Spear Quicken", (uint)EffectStatusIDs.SPEARQUICKEN, "Swordman"),
        new("Defending Aura", (uint)EffectStatusIDs.DEFENDER, "Swordman"),
        new("Dedication", (uint)EffectStatusIDs.LKCONCENTRATION, "Swordman"),
        new("Frenzy", (uint)EffectStatusIDs.BERSERK, "Swordman"),
        new("Two-Hand Quicken", (uint)EffectStatusIDs.TWOHANDQUICKEN, "Swordman"),
        new("Parry", (uint)EffectStatusIDs.PARRYING, "Swordman"),
        new("Aura Blade", (uint)EffectStatusIDs.AURABLADE, "Swordman"),
        new("Enchant Blade", (uint)EffectStatusIDs.ENCHANT_BLADE, "Swordman"),
        new("Shrink", (uint)EffectStatusIDs.CR_SHRINK, "Swordman"),
        new("Inspiration", (uint)EffectStatusIDs.INSPIRATION, "Swordman"),
        new("Prestige", (uint)EffectStatusIDs.PRESTIGE, "Swordman"),
        new("Shield Spell", (uint)EffectStatusIDs.SHIELDSPELL, "Swordman"),
        new("Vanguard Force", (uint)EffectStatusIDs.FORCEOFVANGUARD, "Swordman"),
        new("Reflect Damage", (uint)EffectStatusIDs.REFLECTDAMAGE, "Swordman"),
        new("Vigor", (uint)EffectStatusIDs.EFST_VIGOR, "Swordman"),
        new("Servant Weapon", (uint)EffectStatusIDs.SERVANTWEAPON, "Swordman"),
        new("Attack Stance", (uint)EffectStatusIDs.EFST_ATTACK_STANCE, "Swordman"),
        new("Guard Stance", (uint)EffectStatusIDs.EFST_GUARD_STANCE, "Swordman"),
        new("Rebound Shield", (uint)EffectStatusIDs.EFST_REBOUND_S, "Swordman"),
        new("Guardian Shield", (uint)EffectStatusIDs.EFST_GUARDIAN_S, "Swordman"),
        new("Holy Shield", (uint)EffectStatusIDs.EFST_HOLY_S, "Swordman"),
        new("Exceed Break", (uint)EffectStatusIDs.EFST_EXEEDBREAK, "Swordman"),
        new("One-Hand Quicken", (uint)EffectStatusIDs.EFST_ONEHANDQUICKEN, "Swordman"),
    ];

    public static List<BuffDefinition> GetMageSkills() =>
    [
        new("Energy Coat", (uint)EffectStatusIDs.ENERGYCOAT, "Mage"),
        new("Sight Blaster", (uint)EffectStatusIDs.SIGHTBLASTER, "Mage"),
        new("Autospell", (uint)EffectStatusIDs.AUTOSPELL, "Mage"),
        new("Double Casting", (uint)EffectStatusIDs.DOUBLECASTING, "Mage"),
        new("Memorize", (uint)EffectStatusIDs.MEMORIZE, "Mage"),
        new("Telekinesis Intense", (uint)EffectStatusIDs.TELEKINESIS_INTENSE, "Mage"),
        new("Amplification", (uint)EffectStatusIDs.MYST_AMPLIFY, "Mage"),
        new("Recognized Spell", (uint)EffectStatusIDs.RECOGNIZEDSPELL, "Mage"),
        new("Climax", (uint)EffectStatusIDs.EFST_CLIMAX, "Mage"),
    ];

    public static List<BuffDefinition> GetMerchantSkills() =>
    [
        new("Crazy Uproar", (uint)EffectStatusIDs.CRAZY_UPROAR, "Merchant"),
        new("Power-Thrust", (uint)EffectStatusIDs.OVERTHRUST, "Merchant"),
        new("Adrenaline Rush", (uint)EffectStatusIDs.ADRENALINE, "Merchant"),
        new("Adv. Adrenaline Rush", (uint)EffectStatusIDs.ADRENALINE2, "Merchant"),
        new("Max Power-Thrust", (uint)EffectStatusIDs.OVERTHRUSTMAX, "Merchant"),
        new("Weapon Perfection", (uint)EffectStatusIDs.WEAPONPERFECT, "Merchant"),
        new("Power Maximize", (uint)EffectStatusIDs.MAXIMIZE, "Merchant"),
        new("Cart Boost", (uint)EffectStatusIDs.CARTBOOST, "Merchant"),
        new("Meltdown", (uint)EffectStatusIDs.MELTDOWN, "Merchant"),
        new("Acceleration", (uint)EffectStatusIDs.ACCELERATION, "Merchant"),
        new("GN Cart Boost", (uint)EffectStatusIDs.GN_CARTBOOST, "Merchant"),
        new("Research Report", (uint)EffectStatusIDs.EFST_RESEARCHREPORT, "Merchant"),
        new("Create Hell Tree", (uint)EffectStatusIDs.EFST_BO_HELL_DUSTY, "Merchant"),
    ];

    public static List<BuffDefinition> GetThiefSkills() =>
    [
        new("Poison React", (uint)EffectStatusIDs.POISONREACT, "Thief"),
        new("Reject Sword", (uint)EffectStatusIDs.SWORDREJECT, "Thief"),
        new("Preserve", (uint)EffectStatusIDs.PRESERVE, "Thief"),
        new("Enchant Deadly Poison", (uint)EffectStatusIDs.EDP, "Thief"),
        new("Weapon Blocking", (uint)EffectStatusIDs.WEAPONBLOCKING, "Thief"),
        new("Dancing Knife", (uint)EffectStatusIDs.EFST_DANCING_KNIFE, "Thief"),
        new("Enchanting Shadow", (uint)EffectStatusIDs.EFST_SHADOW_WEAPON, "Thief"),
        new("Potent Venom", (uint)EffectStatusIDs.EFST_POTENT_VENOM, "Thief"),
        new("Shadow Exceed", (uint)EffectStatusIDs.EFST_SHADOW_EXCEED, "Thief"),
        new("Abyss Slayer", (uint)EffectStatusIDs.EFST_ABYSS_SLAYER, "Thief"),
        new("Abyss Dagger", (uint)EffectStatusIDs.EFST_ABYSS_DAGGER, "Thief"),
    ];

    public static List<BuffDefinition> GetAcolyteSkills() =>
    [
        new("Gloria", (uint)EffectStatusIDs.GLORIA, "Acolyte"),
        new("Magnificat", (uint)EffectStatusIDs.MAGNIFICAT, "Acolyte"),
        new("Angelus", (uint)EffectStatusIDs.ANGELUS, "Acolyte"),
        new("Rising Dragon", (uint)EffectStatusIDs.RAISINGDRAGON, "Acolyte"),
        new("Gentle Touch-Revitalize", (uint)EffectStatusIDs.GENTLETOUCH_REVITALIZE, "Acolyte"),
        new("Gentle Touch-Convert", (uint)EffectStatusIDs.GENTLETOUCH_CHANGE, "Acolyte"),
        new("Fury", (uint)EffectStatusIDs.FURY, "Acolyte"),
        new("Impositio Manus", (uint)EffectStatusIDs.IMPOSITIO, "Acolyte"),
        new("Competentia", (uint)EffectStatusIDs.EFST_COMPETENTIA, "Acolyte"),
        new("Offertorium", (uint)EffectStatusIDs.EFST_OFFERTORIUM, "Acolyte"),
        new("Sincere Faith", (uint)EffectStatusIDs.EFST_SINCERE_FAITH, "Acolyte"),
        new("Firm Faith", (uint)EffectStatusIDs.FIRM_FAITH, "Acolyte"),
        new("Powerful Faith", (uint)EffectStatusIDs.POWERFUL_FAITH, "Acolyte"),
        new("First Faith Power", (uint)EffectStatusIDs.EFST_FIRST_FAITH_POWER, "Acolyte"),
        new("Second Judgement", (uint)EffectStatusIDs.EFST_SECOND_JUDGE, "Acolyte"),
        new("Third Exorcism Flame", (uint)EffectStatusIDs.EFST_THIRD_EXOR_FLAME, "Acolyte"),
    ];

    public static List<BuffDefinition> GetNinjaSkills() =>
    [
        new("Cicada Skin Shed", (uint)EffectStatusIDs.PEEL_CHANGE, "Ninja"),
        new("Ninja Aura", (uint)EffectStatusIDs.AURA_NINJA, "Ninja"),
        new("Izayoi", (uint)EffectStatusIDs.IZAYOI, "Ninja"),
    ];

    public static List<BuffDefinition> GetTaekwonSkills() =>
    [
        new("Mild Wind (Earth)", (uint)EffectStatusIDs.PROPERTYGROUND, "Taekwon"),
        new("Mild Wind (Fire)", (uint)EffectStatusIDs.PROPERTYFIRE, "Taekwon"),
        new("Mild Wind (Water)", (uint)EffectStatusIDs.PROPERTYWATER, "Taekwon"),
        new("Mild Wind (Wind)", (uint)EffectStatusIDs.PROPERTYWIND, "Taekwon"),
        new("Mild Wind (Ghost)", (uint)EffectStatusIDs.PROPERTYTELEKINESIS, "Taekwon"),
        new("Mild Wind (Holy)", (uint)EffectStatusIDs.ASPERSIO, "Taekwon"),
        new("Mild Wind (Shadow)", (uint)EffectStatusIDs.PROPERTYDARK, "Taekwon"),
        new("Solar Heat", (uint)EffectStatusIDs.EFST_SG_SUN_WARM, "Taekwon"),
        new("Solar Protection", (uint)EffectStatusIDs.EFST_SUN_COMFORT, "Taekwon"),
        new("Lunar Heat", (uint)EffectStatusIDs.EFST_SG_MOON_WARM, "Taekwon"),
        new("Lunar Protection", (uint)EffectStatusIDs.EFST_MOON_COMFORT, "Taekwon"),
        new("Stellar Heat", (uint)EffectStatusIDs.EFST_SG_STAR_WARM, "Taekwon"),
        new("Stellar Protection", (uint)EffectStatusIDs.EFST_STAR_COMFORT, "Taekwon"),
        new("Tumbling", (uint)EffectStatusIDs.DODGE_ON, "Taekwon"),
        new("Enchanting Sky", (uint)EffectStatusIDs.EFST_SKY_ENCHANT, "Taekwon"),
        new("Universal Stance", (uint)EffectStatusIDs.EFST_UNIVERSESTANCE, "Taekwon"),
    ];

    public static List<BuffDefinition> GetGunslingerSkills() =>
    [
        new("Gatling Fever", (uint)EffectStatusIDs.GATLINGFEVER, "Gunslinger"),
        new("Madness Canceller", (uint)EffectStatusIDs.MADNESSCANCEL, "Gunslinger"),
        new("Adjustment", (uint)EffectStatusIDs.ADJUSTMENT, "Gunslinger"),
        new("Increase Accuracy", (uint)EffectStatusIDs.ACCURACY, "Gunslinger"),
    ];

    public static List<BuffDefinition> GetSummonerSkills() =>
    [
        new("Bunch of Shrimp", (uint)EffectStatusIDs.EFST_SHRIMP, "Summoner"),
        new("Temporary Communion", (uint)EffectStatusIDs.EFST_TEMPORARY_COMMUNION, "Summoner"),
        new("Marine Festival", (uint)EffectStatusIDs.EFST_MARINE_FESTIVAL, "Summoner"),
        new("Sandy Festival", (uint)EffectStatusIDs.EFST_SANDY_FESTIVAL, "Summoner"),
        new("Colors of Hyunrok Lv 1", (uint)EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_1, "Summoner"),
        new("Colors of Hyunrok Lv 2", (uint)EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_2, "Summoner"),
        new("Colors of Hyunrok Lv 3", (uint)EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_3, "Summoner"),
        new("Colors of Hyunrok Lv 4", (uint)EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_4, "Summoner"),
        new("Colors of Hyunrok Lv 5", (uint)EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_5, "Summoner"),
        new("Colors of Hyunrok Lv 6", (uint)EffectStatusIDs.EFST_COLORS_OF_HYUN_ROK_6, "Summoner"),
    ];

    public static List<BuffDefinition> GetSoulAsceticSkills() =>
    [
        new("Soul of Heaven and Earth", (uint)EffectStatusIDs.EFST_HEAVEN_AND_EARTH, "Soul Ascetic"),
    ];

    public static List<BuffDefinition> GetNightWatchSkills() =>
    [
        new("Hidden Card", (uint)EffectStatusIDs.EFST_HIDDEN_CARD, "Night Watch"),
        new("Intensive Aim", (uint)EffectStatusIDs.EFST_INTENSIVE_AIM, "Night Watch"),
        new("Auto Firing Launcher", (uint)EffectStatusIDs.EFST_AUTO_FIRING_LAUNCHEREFST, "Night Watch"),
        new("Platinum Altar", (uint)EffectStatusIDs.EFST_P_ALTER, "Night Watch"),
        new("Hit Barrel", (uint)EffectStatusIDs.EFST_HEAT_BARREL, "Night Watch"),
        new("Eternal Chain", (uint)EffectStatusIDs.EFST_E_CHAIN, "Night Watch"),
        new("Grenade Fragment Lv 1", (uint)EffectStatusIDs.EFST_GRENADE_FRAGMENT_1, "Night Watch"),
        new("Grenade Fragment Lv 2", (uint)EffectStatusIDs.EFST_GRENADE_FRAGMENT_2, "Night Watch"),
        new("Grenade Fragment Lv 3", (uint)EffectStatusIDs.EFST_GRENADE_FRAGMENT_3, "Night Watch"),
        new("Grenade Fragment Lv 4", (uint)EffectStatusIDs.EFST_GRENADE_FRAGMENT_4, "Night Watch"),
        new("Grenade Fragment Lv 5", (uint)EffectStatusIDs.EFST_GRENADE_FRAGMENT_5, "Night Watch"),
        new("Grenade Fragment Lv 6", (uint)EffectStatusIDs.EFST_GRENADE_FRAGMENT_6, "Night Watch"),
    ];

    public static List<BuffDefinition> GetHyperNoviceSkills() =>
    [
        new("Rule Break", (uint)EffectStatusIDs.EFST_RULEBREAK, "Hyper Novice"),
        new("Breaking Limit", (uint)EffectStatusIDs.EFST_BREAKINGLIMIT, "Hyper Novice"),
    ];

    public static List<BuffDefinition> GetHomunculusBuffs() =>
    [
        new("Pyroclastic", (uint)EffectStatusIDs.EFST_PYROCLASTIC, "Homunculus"),
        new("Homun Last", (uint)EffectStatusIDs.EFST_TEMPERING, "Homunculus"),
    ];

    public static List<BuffDefinition> GetAllSkillBuffs()
    {
        var all = new List<BuffDefinition>();
        all.AddRange(GetArcherSkills());
        all.AddRange(GetSwordmanSkills());
        all.AddRange(GetMageSkills());
        all.AddRange(GetMerchantSkills());
        all.AddRange(GetThiefSkills());
        all.AddRange(GetAcolyteSkills());
        all.AddRange(GetNinjaSkills());
        all.AddRange(GetTaekwonSkills());
        all.AddRange(GetGunslingerSkills());
        all.AddRange(GetSummonerSkills());
        all.AddRange(GetSoulAsceticSkills());
        all.AddRange(GetNightWatchSkills());
        all.AddRange(GetHyperNoviceSkills());
        all.AddRange(GetHomunculusBuffs());
        return all;
    }

    // ─── ITEM / STUFF BUFFS ───────────────────────────────────

    public static List<BuffDefinition> GetPotionBuffs() =>
    [
        new("Concentration Potion", (uint)EffectStatusIDs.CONCENTRATION_POTION, "Potions"),
        new("Awakening Potion", (uint)EffectStatusIDs.AWAKENING_POTION, "Potions"),
        new("Berserk Potion", (uint)EffectStatusIDs.BERSERK_POTION, "Potions"),
        new("Regeneration Potion", (uint)EffectStatusIDs.REGENERATION_POTION, "Potions"),
        new("HP Increase Potion", (uint)EffectStatusIDs.HP_INCREASE_POTION_LARGE, "Potions"),
        new("SP Increase Potion", (uint)EffectStatusIDs.SP_INCREASE_POTION_LARGE, "Potions"),
        new("Red Herb Activator", (uint)EffectStatusIDs.RED_HERB_ACTIVATOR, "Potions"),
        new("Blue Herb Activator", (uint)EffectStatusIDs.BLUE_HERB_ACTIVATOR, "Potions"),
        new("Golden X", (uint)EffectStatusIDs.REF_T_POTION, "Potions"),
        new("Energy Drink", (uint)EffectStatusIDs.ENERGY_DRINK_RESERCH, "Potions"),
        new("Mega Resist Potion", (uint)EffectStatusIDs.TARGET_BLOOD, "Potions"),
        new("Full SwingK Potion", (uint)EffectStatusIDs.FULL_SWINGK, "Potions"),
        new("Mana Plus Potion", (uint)EffectStatusIDs.MANA_PLUS, "Potions"),
        new("Blessing of Tyr", (uint)EffectStatusIDs.BASICHIT, "Potions"),
        new("Limit Power Booster", (uint)EffectStatusIDs.EFST_LIMIT_POWER_BOOSTER, "Potions"),
        new("Infinity Drink", (uint)EffectStatusIDs.EFST_INFINITY_DRINK, "Potions"),
        new("Red Booster", (uint)EffectStatusIDs.RWC_2011_SCROLL, "Potions"),
        new("Sealed Kiel Card", (uint)EffectStatusIDs.EFST_KIEL_CARD, "Potions"),
        new("Physical Fury Potion", (uint)EffectStatusIDs.EFST_DF_FULLSWINGK, "Potions"),
        new("Magic Potion", (uint)EffectStatusIDs.EFST_DRACULA_CARD, "Potions"),
        new("True ASPD Potion", (uint)EffectStatusIDs.EFST_REUSE_LIMIT_ASPD_POTION, "Potions"),
        new("True Life Potion", (uint)EffectStatusIDs.EFST_L_LIFEPOTION, "Potions"),
    ];

    public static List<BuffDefinition> GetElementalBuffs() =>
    [
        new("Converter (Fire)", (uint)EffectStatusIDs.EFST_ATTACK_PROPERTY_FIRE, "Elementals"),
        new("Converter (Wind)", (uint)EffectStatusIDs.EFST_ATTACK_PROPERTY_WIND, "Elementals"),
        new("Converter (Earth)", (uint)EffectStatusIDs.EFST_ATTACK_PROPERTY_GROUND, "Elementals"),
        new("Converter (Water)", (uint)EffectStatusIDs.EFST_ATTACK_PROPERTY_WATER, "Elementals"),
        new("Cursed Water", (uint)EffectStatusIDs.EFST_ATTACK_PROPERTY_DARKNESS, "Elementals"),
        new("Fire Conversor", (uint)EffectStatusIDs.PROPERTYFIRE, "Elementals"),
        new("Wind Conversor", (uint)EffectStatusIDs.PROPERTYWIND, "Elementals"),
        new("Earth Conversor", (uint)EffectStatusIDs.PROPERTYGROUND, "Elementals"),
        new("Water Conversor", (uint)EffectStatusIDs.PROPERTYWATER, "Elementals"),
        new("Aspersio Scroll", (uint)EffectStatusIDs.ASPERSIO, "Elementals"),
        new("Ghost Conversor", (uint)EffectStatusIDs.PROPERTYTELEKINESIS, "Elementals"),
        new("Fireproof Potion", (uint)EffectStatusIDs.RESIST_PROPERTY_FIRE, "Elementals"),
        new("Waterproof Potion", (uint)EffectStatusIDs.RESIST_PROPERTY_WATER, "Elementals"),
        new("Windproof Potion", (uint)EffectStatusIDs.RESIST_PROPERTY_WIND, "Elementals"),
        new("Earthproof Potion", (uint)EffectStatusIDs.RESIST_PROPERTY_GROUND, "Elementals"),
    ];

    public static List<BuffDefinition> GetBoxBuffs() =>
    [
        new("Drowsiness Box", (uint)EffectStatusIDs.DROWSINESS_BOX, "Boxes"),
        new("Resentment Box", (uint)EffectStatusIDs.RESENTMENT_BOX, "Boxes"),
        new("Sunlight Box", (uint)EffectStatusIDs.SUNLIGHT_BOX, "Boxes"),
        new("Box of Gloom", (uint)EffectStatusIDs.CONCENTRATION, "Boxes"),
        new("Box of Thunder", (uint)EffectStatusIDs.BOX_OF_THUNDER, "Boxes"),
        new("Speed Potion / Guyak", (uint)EffectStatusIDs.SPEED_POT, "Boxes"),
        new("Anodyne", (uint)EffectStatusIDs.ENDURE, "Boxes"),
        new("Aloevera", (uint)EffectStatusIDs.PROVOKE, "Boxes"),
        new("Abrasive", (uint)EffectStatusIDs.CRITICALPERCENT, "Boxes"),
        new("Combat Pill", (uint)EffectStatusIDs.COMBAT_PILL, "Boxes"),
        new("Adv. Combat Pill", (uint)EffectStatusIDs.EFST_GM_BATTLE2, "Boxes"),
        new("Celermine Juice", (uint)EffectStatusIDs.ENRICH_CELERMINE_JUICE, "Boxes"),
        new("Guarana Candy", (uint)EffectStatusIDs.SPEED_POT, "Boxes"),
        new("Poison Bottle", (uint)EffectStatusIDs.ASPDPOTIONINFINITY, "Boxes"),
    ];

    public static List<BuffDefinition> GetFoodBuffs() =>
    [
        new("STR Food", (uint)EffectStatusIDs.FOOD_STR, "Foods"),
        new("AGI Food", (uint)EffectStatusIDs.FOOD_AGI, "Foods"),
        new("VIT Food", (uint)EffectStatusIDs.FOOD_VIT, "Foods"),
        new("INT Food", (uint)EffectStatusIDs.FOOD_INT, "Foods"),
        new("DEX Food", (uint)EffectStatusIDs.FOOD_DEX, "Foods"),
        new("LUK Food", (uint)EffectStatusIDs.FOOD_LUK, "Foods"),
        new("3RD STR Food", (uint)EffectStatusIDs.STR_3RD_FOOD, "Foods"),
        new("3RD AGI Food", (uint)EffectStatusIDs.AGI_3RD_FOOD, "Foods"),
        new("3RD VIT Food", (uint)EffectStatusIDs.VIT_3RD_FOOD, "Foods"),
        new("3RD INT Food", (uint)EffectStatusIDs.INT_3RD_FOOD, "Foods"),
        new("3RD DEX Food", (uint)EffectStatusIDs.DEX_3RD_FOOD, "Foods"),
        new("3RD LUK Food", (uint)EffectStatusIDs.LUK_3RD_FOOD, "Foods"),
        new("CASH Food", (uint)EffectStatusIDs.FOOD_VIT_CASH, "Foods"),
        new("Acaraje", (uint)EffectStatusIDs.EFST_ACARAJE, "Foods"),
        new("STR Biscuit Stick", (uint)EffectStatusIDs.STR_Biscuit_Stick, "Foods"),
        new("AGI Biscuit Stick", (uint)EffectStatusIDs.AGI_Biscuit_Stick, "Foods"),
        new("VIT Biscuit Stick", (uint)EffectStatusIDs.VIT_Biscuit_Stick, "Foods"),
        new("INT Biscuit Stick", (uint)EffectStatusIDs.INT_Biscuit_Stick, "Foods"),
        new("DEX Biscuit Stick", (uint)EffectStatusIDs.DEX_Biscuit_Stick, "Foods"),
        new("LUK Biscuit Stick", (uint)EffectStatusIDs.LUK_Biscuit_Stick, "Foods"),
        new("Green Bubble Gum", (uint)EffectStatusIDs.EFST_Bubble_Gum_Green, "Foods"),
        new("Red Bubble Gum", (uint)EffectStatusIDs.EFST_Bubble_Gum_Red, "Foods"),
        new("Yellow Bubble Gum", (uint)EffectStatusIDs.EFST_Bubble_Gum_Yellow, "Foods"),
        new("Orange Bubble Gum", (uint)EffectStatusIDs.EFST_Bubble_Gum_Orange, "Foods"),
        new("Winter Cookie ATK", (uint)EffectStatusIDs.EFST_ATK_POPCORN, "Foods"),
        new("Flora Cookie MATK", (uint)EffectStatusIDs.EFST_MATK_POPCORN, "Foods"),
    ];

    public static List<BuffDefinition> GetScrollBuffs() =>
    [
        new("Eden Scroll", (uint)EffectStatusIDs.EFST_EDEN, "Scrolls"),
        new("Increase Agility Scroll", (uint)EffectStatusIDs.INC_AGI, "Scrolls"),
        new("Bless Scroll", (uint)EffectStatusIDs.BLESSING, "Scrolls"),
        new("Full Chemical Protection", (uint)EffectStatusIDs.PROTECTARMOR, "Scrolls"),
        new("Burn Incense", (uint)EffectStatusIDs.EFST_BURNT_INCENSE, "Scrolls"),
        new("Link Scroll", (uint)EffectStatusIDs.SOULLINK, "Scrolls"),
        new("Monster Transform", (uint)EffectStatusIDs.MONSTER_TRANSFORM, "Scrolls"),
        new("Assumptio", (uint)EffectStatusIDs.ASSUMPTIO, "Scrolls"),
        new("Holy Armor Scroll", (uint)EffectStatusIDs.EFST_ARMOR_PROPERTY, "Scrolls"),
        new("Soul Scroll", (uint)EffectStatusIDs.EFST_SOULSCROLL, "Scrolls"),
        new("Undead Element Scroll", (uint)EffectStatusIDs.EFST_RESIST_PROPERTY_UNDEAD, "Scrolls"),
    ];

    public static List<BuffDefinition> GetETCBuffs() =>
    [
        new("THURISAZ Rune", (uint)EffectStatusIDs.THURISAZ, "ETC"),
        new("OTHILA Rune", (uint)EffectStatusIDs.OTHILA, "ETC"),
        new("HAGALAZ Rune", (uint)EffectStatusIDs.HAGALAZ, "ETC"),
        new("LUX AMINA Rune", (uint)EffectStatusIDs.LUX_AMINA, "ETC"),
        new("Cat Can", (uint)EffectStatusIDs.OVERLAPEXPUP, "ETC"),
        new("HE Bubble Gum", (uint)EffectStatusIDs.CASH_RECEIVEITEM, "ETC"),
        new("Frost Giant Blood", (uint)EffectStatusIDs.EFST_GVG_GIANT, "ETC"),
        new("Battle Manual", (uint)EffectStatusIDs.EFST_GVG_GOLEM, "ETC"),
        new("Magic Candy", (uint)EffectStatusIDs.EFST_MAGIC_CANDY, "ETC"),
        new("Ghostring", (uint)EffectStatusIDs.EFST_ARMOR_PROPERTY, "ETC"),
        new("Angeling", (uint)EffectStatusIDs.EFST_ARMOR_PROPERTY, "ETC"),
        new("Tao Gunka", (uint)EffectStatusIDs.EFST_TAO_GUNKA, "ETC"),
        new("Orc Lord", (uint)EffectStatusIDs.EFST_ORC_LORD, "ETC"),
        new("Orc Hero", (uint)EffectStatusIDs.EFST_ORC_HERO, "ETC"),
        new("Mistress", (uint)EffectStatusIDs.EFST_MISTRESS, "ETC"),
    ];

    public static List<BuffDefinition> GetCandyBuffs() =>
    [
        new("Sweets Macaron Cake", (uint)EffectStatusIDs.EFST_SWEETSFAIR_ATK, "Candies"),
        new("Sweets Strawberry Parfait", (uint)EffectStatusIDs.EFST_SWEETSFAIR_MATK, "Candies"),
        new("Popcorn Festival Buff", (uint)EffectStatusIDs.EFST_FLOWER_LEAF2, "Candies"),
        new("Hyper Sugar Candy", (uint)EffectStatusIDs.EFST_STEAMPACK, "Candies"),
        new("Ultra Miraculous Elixir", (uint)EffectStatusIDs.EFST_ALMIGHTY, "Candies"),
        new("Cherry Blossom Rice Cake", (uint)EffectStatusIDs.EFST_FLOWER_LEAF4, "Candies"),
    ];

    public static List<BuffDefinition> GetEXPBuffs() =>
    [
        new("Bubble Gum", (uint)EffectStatusIDs.CASH_RECEIVEITEM, "EXP"),
        new("Battle Manual (Base)", (uint)EffectStatusIDs.CASH_PLUSEXP, "EXP"),
        new("Battle Manual (Class)", (uint)EffectStatusIDs.CASH_PLUSECLASSXP, "EXP"),
    ];

    public static List<BuffDefinition> GetAllItemBuffs()
    {
        var all = new List<BuffDefinition>();
        all.AddRange(GetPotionBuffs());
        all.AddRange(GetElementalBuffs());
        all.AddRange(GetBoxBuffs());
        all.AddRange(GetFoodBuffs());
        all.AddRange(GetScrollBuffs());
        all.AddRange(GetETCBuffs());
        all.AddRange(GetCandyBuffs());
        all.AddRange(GetEXPBuffs());
        return all;
    }

    // ─── DEBUFFS ──────────────────────────────────────────────

    public static List<BuffDefinition> GetDebuffs() =>
    [
        new("Critical Wounds", (uint)EffectStatusIDs.CRITICALWOUND, "Debuff"),
        new("Freezing", (uint)EffectStatusIDs.EFST_FREEZING, "Debuff"),
        new("Curse", (uint)EffectStatusIDs.CURSE, "Debuff"),
        new("Bleeding", (uint)EffectStatusIDs.EFST_BLEEDING, "Debuff"),
        new("Silence", (uint)EffectStatusIDs.SILENCE, "Debuff"),
        new("Decrease Agi", (uint)EffectStatusIDs.EFST_DECREASE_AGI, "Debuff"),
        new("Confusion / Chaos", (uint)EffectStatusIDs.CONFUSION, "Debuff"),
        new("Stun", (uint)EffectStatusIDs.EFST_STUN, "Debuff"),
        new("Deep Sleep", (uint)EffectStatusIDs.EFST_DEEP_SLEEP, "Debuff"),
        new("Poison", (uint)EffectStatusIDs.POISON, "Debuff"),
        new("Lucky Water", (uint)EffectStatusIDs.EFST_HANDICAPSTATE_MISFORTUNE, "Debuff"),
    ];
}
