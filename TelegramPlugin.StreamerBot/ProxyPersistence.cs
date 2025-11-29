using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Streamer.bot.Plugin.Interface;

namespace TelegramPlugin.StreamerBot;

public class ProxyPersistence(IInlineInvokeProxy cph, string key, IPluginLogger logger) : IPersistenceLayer
{
    public Dictionary<string, int>? Get()
    {
        var dict = cph.GetGlobalVar<Dictionary<string, int>>(key);
        try
        {
            return JsonConvert.DeserializeObject<Dictionary<string, int>?>(JsonConvert.SerializeObject(dict));
        }
        catch (Exception e)
        {
            logger.Error(e.Message);
            return new Dictionary<string, int>();
        }
    }

    public void Set(Dictionary<string, int> value)
    {
        cph.SetGlobalVar(key, JsonConvert.SerializeObject(value));
    }
}