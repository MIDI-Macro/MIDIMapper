using System.Diagnostics;
using Newtonsoft.Json;

namespace MIDIMapper
{
    public static class ConfigManager<T>
    {
        private static readonly string ConfigFilePath = "PGC.json";

        public static void GenerateConfig(T config)
        {
            Debug.Assert(!File.Exists(ConfigFilePath));
            string jsonString = JsonConvert.SerializeObject(config, Formatting.Indented);
            File.WriteAllText(ConfigFilePath, jsonString);
        }

        public static bool isConfigExist()
        {
            return File.Exists(ConfigFilePath);
        }

        public static T LoadConfig()
        {
            Debug.Assert(File.Exists(ConfigFilePath));
            string jsonString = File.ReadAllText(ConfigFilePath);
            T config = JsonConvert.DeserializeObject<T>(jsonString);
            return config;
        }
    }
}
