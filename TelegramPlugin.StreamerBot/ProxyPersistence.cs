using System;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using Streamer.bot.Plugin.Interface;

namespace TelegramPlugin.StreamerBot;

public class ProxyPersistence(IInlineInvokeProxy cph, string key, IPluginLogger logger) : IPersistenceLayer
{
    public ConcurrentDictionary<string, int>? Get()
    {
        try
        {
            var dict = cph.GetGlobalVar<ConcurrentDictionary<string, int>>(key);
            return JsonConvert.DeserializeObject<ConcurrentDictionary<string, int>>(JsonConvert.SerializeObject(dict));
        }
        catch (Exception e)
        {
            logger.Error(e.Message);
            logger.Notify(e.Message);
            return new ConcurrentDictionary<string, int>();
        }
    }

    public void Set(ConcurrentDictionary<string, int> value)
    {
        cph.SetGlobalVar(key, JsonConvert.SerializeObject(value, Formatting.Indented));
    }
}