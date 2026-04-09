using System;
using System.Collections.Generic;
using System.IO;

namespace _4RTools.Utils
{
    internal class AppConfig
    {
        public static string Name = "4RTools";
        public static string ProfileFolder = "Profile\\";
        public static string Website = "https://www.4rtools.com.br";
        public static string GithubLink = "https://github.com/4RTools/4Rtools";
        public static string DiscordLink = "https://discord.gg/AtZ2fJVtBz";
        public static string _4RClientsURL = "https://storage.googleapis.com/4rtools/supported_servers.json";
        public static string _4RAdvertiserUrl = "https://storage.googleapis.com/4rtools/advertisers.json";
        public static string _4RLatestVersionURL = "https://api.github.com/repos/4RTools/4RTools/releases/latest";
        public static string _4RApiHost = "https://api.4rtools.com.br/api";
        public static string Version = "v2.10.0";

        public static string GetProfileDirectory()
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Profile");
        }

        public static string GetProfilePath(string profileName)
        {
            return Path.Combine(GetProfileDirectory(), profileName + ".json");
        }

        public static string GetLastProfilePath()
        {
            return Path.Combine(GetProfileDirectory(), "_last_profile.txt");
        }

        public static void EnsureProfileDirectory()
        {
            string directory = GetProfileDirectory();
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
        }
    }
}
