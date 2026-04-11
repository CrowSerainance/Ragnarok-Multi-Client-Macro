using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using _4RTools.Forms;
using _4RTools.Utils;

namespace _4RTools.Model
{
    public class ProfileSingleton
    {
        public static Profile profile = new Profile("Default");

        public static void Load(string profileName)
        {
            string filePath = AppConfig.GetProfilePath(profileName);
            try
            {
                Console.WriteLine($"[Profile.Load] Loading \"{profileName}\" from \"{filePath}\"");
                JObject root = Profile.ReadProfileRoot(filePath, profileName, createIfMissing: true);
                profile = Profile.DeserializeRoot(root, profileName);
                Console.WriteLine($"[Profile.Load] Loaded OK. Schema={Profile.DetectSchema(root)}, path=\"{filePath}\"");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"[Profile.Load] Error loading \"{profileName}\" from \"{filePath}\": {ex}");
                throw new Exception("Houve um problema ao carregar o perfil. Delete a pasta Profiles e tente novamente.");
            }
        }

        public static void Create(string profileName)
        {
            string filePath = AppConfig.GetProfilePath(profileName);
            AppConfig.EnsureProfileDirectory();

            if (!File.Exists(filePath))
            {
                Console.WriteLine($"[Profile.Create] Creating normalized profile \"{profileName}\" at \"{filePath}\"");
                Profile newProfile = new Profile(profileName);
                JObject root = Profile.BuildCanonicalRoot(newProfile);
                Profile.WriteProfileRoot(filePath, root, profileName);
            }

            Load(profileName);
        }

        public static void Delete(string profileName)
        {
            try
            {
                if (profileName != "Default")
                {
                    string filePath = AppConfig.GetProfilePath(profileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }
            catch { }
        }

        public static void Rename(string oldProfileName, string newProfileName)
        {
            string oldPath = AppConfig.GetProfilePath(oldProfileName);
            string newPath = AppConfig.GetProfilePath(newProfileName);
            AppConfig.EnsureProfileDirectory();

            if (oldProfileName != "Default" && File.Exists(oldPath) && !File.Exists(newPath))
            {
                File.Move(oldPath, newPath);
                Profile.NormalizeProfileFileName(newPath, newProfileName);
            }
        }

        public static void Copy(string profileName)
        {
            try
            {
                AppConfig.EnsureProfileDirectory();
                string copyName = profileName + " Copy";
                string sourcePath = AppConfig.GetProfilePath(profileName);
                string destinationPath = AppConfig.GetProfilePath(copyName);
                if (File.Exists(sourcePath) && !File.Exists(destinationPath))
                {
                    File.Copy(sourcePath, destinationPath);
                    Profile.NormalizeProfileFileName(destinationPath, copyName);
                }
            }
            catch { }
        }

        public static void SetConfiguration(Action action)
        {
            if (profile == null || action == null)
            {
                Console.Error.WriteLine("[SetConfiguration] SKIPPED - profile or action is null.");
                return;
            }

            string filePath = AppConfig.GetProfilePath(profile.Name);
            Console.WriteLine($"[SetConfiguration] Saving {action.GetActionName()} to \"{filePath}\"");

            JObject root = Profile.ReadProfileRoot(filePath, profile.Name, createIfMissing: true);
            root["Name"] = profile.Name;
            root[action.GetActionName()] = Profile.ToConfigurationToken(action);

            Profile.WriteProfileRoot(filePath, root, profile.Name);
            Console.WriteLine($"[SetConfiguration] Saved OK ({action.GetActionName()})");
        }

        public static void PersistAllConfiguration()
        {
            if (profile == null)
            {
                return;
            }

            string path = AppConfig.GetProfilePath(profile.Name);
            Console.WriteLine($"[PersistAllConfiguration] Writing normalized profile to \"{path}\"");
            JObject root = Profile.BuildCanonicalRoot(profile);
            Profile.WriteProfileRoot(path, root, profile.Name);
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

        public static JObject BuildCanonicalRoot(Profile source)
        {
            JObject root = new JObject();
            root["Name"] = source.Name;

            foreach (Action action in GetAllActions(source))
            {
                root[action.GetActionName()] = ToConfigurationToken(action);
            }

            return root;
        }

        public static Profile DeserializeRoot(JObject root, string profileName)
        {
            Profile loaded = new Profile(profileName);
            loaded.Name = profileName;

            loaded.UserPreferences = DeserializeAction<UserPreferences>(root, loaded.UserPreferences);
            loaded.AHK = DeserializeAction<AHK>(root, loaded.AHK);
            loaded.Autopot = DeserializeAction<Autopot>(root, loaded.Autopot);
            loaded.AutopotYgg = DeserializeAction<Autopot>(root, loaded.AutopotYgg);
            loaded.StatusRecovery = DeserializeAction<StatusRecovery>(root, loaded.StatusRecovery);
            loaded.AutoRefreshSpammer1 = DeserializeAction<AutoRefreshSpammer>(root, loaded.AutoRefreshSpammer1);
            loaded.AutoRefreshSpammer2 = DeserializeAction<AutoRefreshSpammer>(root, loaded.AutoRefreshSpammer2);
            loaded.AutoRefreshSpammer3 = DeserializeAction<AutoRefreshSpammer>(root, loaded.AutoRefreshSpammer3);
            loaded.Autobuff = DeserializeAction<AutoBuff>(root, loaded.Autobuff);
            loaded.SongMacro = DeserializeAction<Macro>(root, loaded.SongMacro);
            loaded.AtkDefMode = DeserializeAction<ATKDEFMode>(root, loaded.AtkDefMode);
            loaded.MacroSwitch = DeserializeAction<Macro>(root, loaded.MacroSwitch);
            loaded.DebuffsRecovery = DeserializeAction<DebuffsRecovery>(root, loaded.DebuffsRecovery);

            if (loaded.AHK != null)
            {
                loaded.AHK.EnsureSlotsConfigured();
            }

            return loaded;
        }

        public static JObject ReadProfileRoot(string filePath, string profileName, bool createIfMissing)
        {
            AppConfig.EnsureProfileDirectory();

            if (!File.Exists(filePath))
            {
                if (!createIfMissing)
                {
                    throw new FileNotFoundException("Profile file not found.", filePath);
                }

                Console.WriteLine($"[Profile.ReadProfileRoot] Missing file. Creating default profile at \"{filePath}\"");
                JObject newRoot = BuildCanonicalRoot(new Profile(profileName));
                WriteProfileRoot(filePath, newRoot, profileName);
                return newRoot;
            }

            string json = File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(json))
            {
                Console.WriteLine($"[Profile.ReadProfileRoot] Empty file at \"{filePath}\". Rebuilding default root.");
                JObject emptyRoot = BuildCanonicalRoot(new Profile(profileName));
                WriteProfileRoot(filePath, emptyRoot, profileName);
                return emptyRoot;
            }

            try
            {
                JToken token = JToken.Parse(json);
                JObject root = token as JObject;
                if (root == null)
                {
                    throw new JsonException("Profile root is not a JSON object.");
                }

                Console.WriteLine($"[Profile.ReadProfileRoot] Parsed schema={DetectSchema(root)} from \"{filePath}\"");
                return root;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Profile.ReadProfileRoot] Malformed profile at \"{filePath}\": {ex.Message}. Rebuilding default root.");
                JObject fallbackRoot = BuildCanonicalRoot(new Profile(profileName));
                WriteProfileRoot(filePath, fallbackRoot, profileName);
                return fallbackRoot;
            }
        }

        public static void WriteProfileRoot(string filePath, JObject root, string profileName)
        {
            AppConfig.EnsureProfileDirectory();
            root["Name"] = profileName;

            string output = JsonConvert.SerializeObject(root, Formatting.Indented);
            File.WriteAllText(filePath, output);
            VerifySavedProfile(filePath, profileName);
        }

        public static string DetectSchema(JObject root)
        {
            if (root == null)
            {
                return "missing";
            }

            foreach (JProperty property in root.Properties())
            {
                if (string.Equals(property.Name, "Name", StringComparison.Ordinal))
                {
                    continue;
                }

                if (property.Value != null && property.Value.Type == JTokenType.String)
                {
                    return "legacy-string";
                }
            }

            return "normalized-object";
        }

        public static JToken ToConfigurationToken(Action action)
        {
            if (action == null)
            {
                return new JObject();
            }

            string configuration = action.GetConfiguration();
            if (string.IsNullOrWhiteSpace(configuration))
            {
                return new JObject();
            }

            try
            {
                return JToken.Parse(configuration);
            }
            catch
            {
                return JValue.CreateString(configuration);
            }
        }

        public static object GetByAction(dynamic obj, Action action)
        {
            if (obj == null || action == null)
            {
                return null;
            }

            JToken token = obj[action.GetActionName()];
            if (token == null)
            {
                return action.GetConfiguration();
            }

            return token.Type == JTokenType.String ? token.ToString() : token.ToString(Formatting.None);
        }

        public static List<string> ListAll()
        {
            List<string> profiles = new List<string>();
            try
            {
                AppConfig.EnsureProfileDirectory();
                string[] files = Directory.GetFiles(AppConfig.GetProfileDirectory(), "*.json");

                foreach (string fileName in files)
                {
                    profiles.Add(Path.GetFileNameWithoutExtension(fileName));
                }
            }
            catch { }

            return profiles;
        }

        private static Action[] GetAllActions(Profile source)
        {
            return new Action[]
            {
                source.AHK,
                source.Autopot,
                source.AutopotYgg,
                source.Autobuff,
                source.StatusRecovery,
                source.SongMacro,
                source.MacroSwitch,
                source.AtkDefMode,
                source.DebuffsRecovery,
                source.AutoRefreshSpammer1,
                source.AutoRefreshSpammer2,
                source.AutoRefreshSpammer3,
                source.UserPreferences
            };
        }

        private static T DeserializeAction<T>(JObject root, Action fallbackAction) where T : class
        {
            string actionName = fallbackAction.GetActionName();
            JToken token = root[actionName];
            string payload = fallbackAction.GetConfiguration();
            string schema = "default";

            if (token != null && token.Type != JTokenType.Null && token.Type != JTokenType.Undefined)
            {
                if (token.Type == JTokenType.String)
                {
                    payload = token.ToString();
                    schema = "legacy-string";
                }
                else
                {
                    payload = token.ToString(Formatting.None);
                    schema = "normalized-object";
                }
            }

            Console.WriteLine($"[Profile.DeserializeAction] {actionName}: {schema}");

            try
            {
                T action = JsonConvert.DeserializeObject<T>(payload);
                if (action != null)
                {
                    return action;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Profile.DeserializeAction] {actionName} fallback due to parse error: {ex.Message}");
            }

            return JsonConvert.DeserializeObject<T>(fallbackAction.GetConfiguration());
        }

        private static void VerifySavedProfile(string filePath, string profileName)
        {
            string json = File.ReadAllText(filePath);
            JObject root = JObject.Parse(json);

            if (root["Name"] == null || root["AHK20"] == null || root["UserPreferences"] == null)
            {
                throw new InvalidDataException($"Profile verification failed for \"{profileName}\" at \"{filePath}\".");
            }

            Console.WriteLine($"[Profile.Verify] Verified \"{profileName}\" at \"{filePath}\"");
        }

        internal static void NormalizeProfileFileName(string filePath, string profileName)
        {
            try
            {
                JObject root = ReadProfileRoot(filePath, profileName, createIfMissing: true);
                root["Name"] = profileName;
                string output = JsonConvert.SerializeObject(root, Formatting.Indented);
                File.WriteAllText(filePath, output);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Profile.NormalizeProfileFileName] Skipped for \"{filePath}\": {ex.Message}");
            }
        }
    }
}
