using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using NUnit.Framework;

namespace TelegramPlugin.Tests.Infrastructure
{
    public class TestFilePersistence : IPersistenceLayer
    {
        private readonly string _key = "TestTelegramPlugin.Key";
        private readonly string _filePath = Path.Combine(TestContext.CurrentContext.TestDirectory, "test_state.json");

        public T Get<T>(string key)
        {
            if (!File.Exists(_filePath)) return default;
            var json = File.ReadAllText(_filePath);
            var dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(json);

            if (dict != null && dict.TryGetValue(key, out var val))
            {
                var innerJson = JsonConvert.SerializeObject(val);
                return JsonConvert.DeserializeObject<T>(innerJson);
            }

            return default;
        }

        public void Set<T>(string key, T value)
        {
            Dictionary<string, object> dict;
            if (File.Exists(_filePath))
            {
                dict = JsonConvert.DeserializeObject<Dictionary<string, object>>(File.ReadAllText(_filePath))
                       ?? new Dictionary<string, object>();
            }
            else
            {
                dict = new Dictionary<string, object>();
            }

            dict[key] = value;
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(dict, Formatting.Indented));
        }

        public void Clear()
        {
            if (File.Exists(_filePath)) File.Delete(_filePath);
        }

        public Dictionary<string, int> Get()
        {
            if (!File.Exists(_filePath)) return new Dictionary<string, int>();
            var json = File.ReadAllText(_filePath);
            return JsonConvert.DeserializeObject<Dictionary<string, int>>(json);
        }

        public void Set(Dictionary<string, int> value)
        {
            File.WriteAllText(_filePath, JsonConvert.SerializeObject(value, Formatting.Indented));
        }
    }

    public class TestConsoleLogger : IPluginLogger
    {
        public void Info(string m) => TestContext.WriteLine($"[INFO] {m}");
        public void Error(string m) => TestContext.WriteLine($"[ERROR] {m}");
        public void Notify(string m) => TestContext.WriteLine($"[NOTIFY] {m}");
    }
}