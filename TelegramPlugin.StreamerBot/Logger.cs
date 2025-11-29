using Streamer.bot.Plugin.Interface;

namespace TelegramPlugin.StreamerBot;

public class Logger(IInlineInvokeProxy cph, string prefix) : IPluginLogger
{
    public void Info(string message)
    {
        cph.LogInfo($"{prefix}: {message}");
    }

    public void Error(string message)
    {
        cph.LogError($"{prefix}: {message}");
    }

    public void Norify(string message)
    {
        cph.ShowToastNotification(prefix, message);
    }
}