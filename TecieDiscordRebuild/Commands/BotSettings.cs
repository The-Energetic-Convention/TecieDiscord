using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TecieDiscordRebuild;

namespace TecieDiscordRebuild.Commands
{
    public static class BotSettings
    {
        public class Settings
        {
            // handle bot settings
            public Dictionary<string, int> warnList = new Dictionary<string, int>();
            public float setVolume = 0.5f;
            public List<string> ignoreChannels = new List<string>();
            public List<string> badWords = new List<string>();
            public int warnLimit = 5;
            public ulong timeoutChanIndex = 0;
            public List<string> announceChannels = new List<string>();
            public List<string> clearIgnore = new List<string>();
            public Dictionary<int, string> eventPings = new Dictionary<int, string>();
        }

        public static Settings settings = new Settings();

        public static void Load()
        {
            //load settings from file, if it exists
            try
            {
                string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TecieDiscord\\SETTINGS.json";
                string json = File.ReadAllText(path);
                settings = JsonConvert.DeserializeObject<Settings>(json)!;
            }
            catch (Exception e)
            {
                Console.WriteLine($"No settings set, or {e.Message}");
            }
        }

        public static void Save()
        {
            string json = JsonConvert.SerializeObject(settings, Formatting.Indented);
            string path = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + "\\TecieDiscord\\SETTINGS.json";
            File.WriteAllText(path, json);
        }
    }
}
