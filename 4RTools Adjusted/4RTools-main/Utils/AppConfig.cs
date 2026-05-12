using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

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
            string assemblyDirectory = Path.GetDirectoryName(typeof(AppConfig).Assembly.Location);
            if (string.IsNullOrWhiteSpace(assemblyDirectory))
            {
                assemblyDirectory = AppDomain.CurrentDomain.BaseDirectory;
            }

            return Path.Combine(assemblyDirectory, "Profile");
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

        /// <summary>
        /// Safer drop-in for <see cref="File.WriteAllText(string, string)"/> when several 4RTools
        /// instances may write the same path concurrently. Writes to a sibling temp file then
        /// atomically swaps it in via <see cref="File.Replace"/>, which avoids the half-written /
        /// truncated states that a raw WriteAllText leaves if two processes race.
        /// </summary>
        public static void AtomicWriteAllText(string filePath, string contents)
        {
            string dir = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            // PID-suffixed temp keeps two concurrent writers from clobbering each other's staging file.
            string tmp = filePath + "." + System.Diagnostics.Process.GetCurrentProcess().Id + ".tmp";

            try
            {
                File.WriteAllText(tmp, contents);

                if (File.Exists(filePath))
                {
                    // File.Replace is atomic on NTFS and preserves the destination ACLs.
                    File.Replace(tmp, filePath, null);
                }
                else
                {
                    File.Move(tmp, filePath);
                }
            }
            finally
            {
                try { if (File.Exists(tmp)) File.Delete(tmp); } catch { }
            }
        }
    }
}
