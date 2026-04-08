using System.Collections.Generic;
using Newtonsoft.Json;
using _4RTools.Utils;
using _4RTools.Forms;
using System.IO;
using System;

namespace _4RTools.Model
{
    public class ProfileSingleton
    {
        public static Profile profile = new Profile("Default");

        public static void Load(string profileName)
        {
            try
            {
                string filePath = AppConfig.ProfileFolder + profileName + ".json";
                Console.WriteLine($"[Profile.Load] Loading \"{profileName}\" from \"{Path.GetFullPath(filePath)}\"");
                string json = File.ReadAllText(filePath);
                dynamic rawObject = JsonConvert.DeserializeObject(json);

                if ((rawObject != null))
                {
                    profile.Name = profileName;
                    profile.UserPreferences = JsonConvert.DeserializeObject<UserPreferences>(Profile.GetByAction(rawObject, profile.UserPreferences));

                    object ahkRaw = Profile.GetByAction(rawObject, profile.AHK);
                    Console.WriteLine($"[Profile.Load] AHK20 raw type: {ahkRaw?.GetType()?.Name}, length: {ahkRaw?.ToString()?.Length}");
                    profile.AHK = JsonConvert.DeserializeObject<AHK>(ahkRaw?.ToString());
                    int boundSlots = 0;
                    if (profile.AHK?.Slots != null)
                    {
                        foreach (var s in profile.AHK.Slots) { if (s.TriggerKey != "None" && !string.IsNullOrEmpty(s.TriggerKey)) boundSlots++; }
                    }
                    Console.WriteLine($"[Profile.Load] AHK deserialized OK — {boundSlots} bound slot(s), mode={profile.AHK?.ahkMode}");
                    profile.Autopot = JsonConvert.DeserializeObject<Autopot>(Profile.GetByAction(rawObject, profile.Autopot));
                    profile.AutopotYgg = JsonConvert.DeserializeObject<Autopot>(Profile.GetByAction(rawObject, profile.AutopotYgg));
                    profile.StatusRecovery = JsonConvert.DeserializeObject<StatusRecovery>(Profile.GetByAction(rawObject, profile.StatusRecovery));
                    profile.AutoRefreshSpammer1 = JsonConvert.DeserializeObject<AutoRefreshSpammer>(Profile.GetByAction(rawObject, profile.AutoRefreshSpammer1));
                    profile.AutoRefreshSpammer2 = JsonConvert.DeserializeObject<AutoRefreshSpammer>(Profile.GetByAction(rawObject, profile.AutoRefreshSpammer2));
                    profile.AutoRefreshSpammer3 = JsonConvert.DeserializeObject<AutoRefreshSpammer>(Profile.GetByAction(rawObject, profile.AutoRefreshSpammer3));
                    profile.Autobuff = JsonConvert.DeserializeObject<AutoBuff>(Profile.GetByAction(rawObject, profile.Autobuff));
                    profile.SongMacro = JsonConvert.DeserializeObject<Macro>(Profile.GetByAction(rawObject, profile.SongMacro));
                    profile.AtkDefMode = JsonConvert.DeserializeObject<ATKDEFMode>(Profile.GetByAction(rawObject, profile.AtkDefMode));
                    profile.MacroSwitch = JsonConvert.DeserializeObject<Macro>(Profile.GetByAction(rawObject, profile.MacroSwitch));
                    profile.DebuffsRecovery = JsonConvert.DeserializeObject<DebuffsRecovery>(Profile.GetByAction(rawObject, profile.DebuffsRecovery));
                }
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Profile] Error Message: {ex.Message}");
                throw new Exception("Houve um problema ao carregar o perfil. Delete a pasta Profiles e tente novamente.");
            }
        }

        public static void Create(string profileName)
        {
            string jsonFileName = AppConfig.ProfileFolder + profileName + ".json";

            if (!File.Exists(jsonFileName))
            {
                if (!Directory.Exists(AppConfig.ProfileFolder)) { Directory.CreateDirectory(AppConfig.ProfileFolder); }
                FileStream fs = File.Create(jsonFileName);
                fs.Close();

                Profile profile = new Profile(profileName);
                string output = JsonConvert.SerializeObject(profile, Formatting.Indented);
                File.WriteAllText(jsonFileName, output);
            }

            ProfileSingleton.Load(profileName);
        }

        public static void Delete(string profileName)
        {
            try
            {
                if (profileName != "Default") { File.Delete(AppConfig.ProfileFolder + profileName + ".json"); }
            }
            catch { }
        }

        public static void Rename(string oldProfileName, string newProfileName)
        {
            string jsonFileName = AppConfig.ProfileFolder + newProfileName + ".json";
            if (oldProfileName != "Default" && !File.Exists(jsonFileName)) {
                File.Move(AppConfig.ProfileFolder + oldProfileName + ".json", jsonFileName);
            }
        }

        public static void Copy(string profileName)
        {
            try
            {
                string copyName = profileName + " Copy";
                string jsonFileName = AppConfig.ProfileFolder + copyName + ".json";
                if (!File.Exists(jsonFileName))
                {
                    File.Copy(AppConfig.ProfileFolder + profileName + ".json", jsonFileName);
                }
            }
            catch { }
        }

        /// <summary>
        /// Saves a single module to the current profile file.
        /// Uses the same dynamic serialization format as the original 4RTools.
        /// </summary>
        public static void SetConfiguration(Action action)
        {
            if (profile != null)
            {
                string filePath = AppConfig.ProfileFolder + profile.Name + ".json";
                Console.WriteLine($"[SetConfiguration] Saving {action.GetActionName()} to \"{Path.GetFullPath(filePath)}\"");
                string jsonData = File.ReadAllText(filePath);
                dynamic jsonObj = JsonConvert.DeserializeObject(jsonData);
                jsonObj[action.GetActionName()] = action.GetConfiguration();
                string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
                File.WriteAllText(filePath, output);
                Console.WriteLine($"[SetConfiguration] Saved OK ({output.Length} bytes)");
            }
            else
            {
                Console.Error.WriteLine("[SetConfiguration] SKIPPED — profile is null!");
            }
        }

        /// <summary>
        /// Writes every configurable module from the current in-memory profile to disk.
        /// Uses the same format as SetConfiguration (dynamic, string values) for consistency.
        /// </summary>
        public static void PersistAllConfiguration()
        {
            if (profile == null)
            {
                return;
            }

            string path = AppConfig.ProfileFolder + profile.Name + ".json";
            if (!File.Exists(path))
            {
                if (!Directory.Exists(AppConfig.ProfileFolder)) { Directory.CreateDirectory(AppConfig.ProfileFolder); }
                File.WriteAllText(path, "{}");
            }

            string jsonData = File.ReadAllText(path);
            dynamic jsonObj = JsonConvert.DeserializeObject(jsonData);

            // Write all modules using the exact same format as SetConfiguration.
            Action[] allActions = new Action[]
            {
                profile.AHK,
                profile.Autopot,
                profile.AutopotYgg,
                profile.Autobuff,
                profile.StatusRecovery,
                profile.SongMacro,
                profile.MacroSwitch,
                profile.AtkDefMode,
                profile.DebuffsRecovery,
                profile.AutoRefreshSpammer1,
                profile.AutoRefreshSpammer2,
                profile.AutoRefreshSpammer3,
                profile.UserPreferences
            };

            foreach (Action action in allActions)
            {
                jsonObj[action.GetActionName()] = action.GetConfiguration();
            }

            string output = JsonConvert.SerializeObject(jsonObj, Formatting.Indented);
            File.WriteAllText(path, output);
        }

        public static Profile GetCurrent()
        {
            return profile;
        }
    }

    public class Profile
    {
        public string Name { get; set; }
        public UserPreferences UserPreferences { get; set; }

        // JsonProperty aligns Create() serialization key names with GetActionName()
        // so Load()/GetByAction() finds the data under the correct key.
        [JsonProperty("AHK20")]
        public AHK AHK { get; set; }
        public Autopot Autopot { get; set; }
        public Autopot AutopotYgg { get; set; }
        [JsonProperty("AutoRefreshSpammer01")]
        public AutoRefreshSpammer AutoRefreshSpammer1 { get; set; }
        [JsonProperty("AutoRefreshSpammer02")]
        public AutoRefreshSpammer AutoRefreshSpammer2 { get; set; }
        [JsonProperty("AutoRefreshSpammer03")]
        public AutoRefreshSpammer AutoRefreshSpammer3 { get; set; }
        public AutoBuff Autobuff { get; set; }
        public StatusRecovery StatusRecovery { get; set; }
        [JsonProperty("SongMacro2.0")]
        public Macro SongMacro { get; set; }
        [JsonProperty("MacroSwitch2.0")]
        public Macro MacroSwitch { get; set; }

        [JsonProperty("ATKDEFMode")]
        public ATKDEFMode AtkDefMode { get; set; }
        public DebuffsRecovery DebuffsRecovery { get; set; }

        public Profile(string name)
        {
            this.Name = name;

            this.UserPreferences = new UserPreferences();
            this.AHK = new AHK();
            this.Autopot = new Autopot(Autopot.ACTION_NAME_AUTOPOT);
            this.AutopotYgg = new Autopot(Autopot.ACTION_NAME_AUTOPOT_YGG);
            this.AutoRefreshSpammer1 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer01");
            this.AutoRefreshSpammer2 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer02");
            this.AutoRefreshSpammer3 = new AutoRefreshSpammer(actionName: "AutoRefreshSpammer03");
            this.Autobuff = new AutoBuff();
            this.StatusRecovery = new StatusRecovery();
            this.SongMacro = new Macro(Macro.ACTION_NAME_SONG_MACRO, MacroSongForm.TOTAL_MACRO_LANES_FOR_SONGS);
            this.MacroSwitch = new Macro(Macro.ACTION_NAME_MACRO_SWITCH, MacroSwitchForm.TOTAL_MACRO_LANES);
            this.AtkDefMode = new ATKDEFMode();
            this.DebuffsRecovery = new DebuffsRecovery();
        }

        public static object GetByAction(dynamic obj, Action action)
        {
            if (obj != null && obj[action.GetActionName()] != null)
            {
                return obj[action.GetActionName()].ToString();
            }

            return action.GetConfiguration();
        }

        public static List<string> ListAll()
        {
            List<string> profiles = new List<string>();
            try
            {
                string[] files = Directory.GetFiles(AppConfig.ProfileFolder, "*.json");

                foreach (string fileName in files)
                {
                    string profileName = Path.GetFileNameWithoutExtension(fileName);
                    profiles.Add(profileName);
                }
            }
            catch { }
            return profiles;
        }
    }

}
