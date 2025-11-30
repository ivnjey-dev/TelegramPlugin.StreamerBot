using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Streamer.bot.Plugin.Interface;

namespace TelegramPlugin.StreamerBot;

public class PluginMain
{
    private readonly HttpClient _sharedClient;
    private readonly IPluginLogger _logger;
    private readonly IPersistenceLayer _persistence;
    private PluginEntry? _innerEntry;
    private string? _currentToken;
    private readonly object _initLock = new object();

    public PluginMain(IInlineInvokeProxy cph)
    {
        _sharedClient = new HttpClient(); //todo добавить 2.0
        _logger = new Logger(cph, "Telegram Plugin Sender");
        _persistence = new ProxyPersistence(cph, "TelegramPlugin.StateManager", _logger);
    }

    public bool Send(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("tg_bot_token", out var tokenObj) ||
            string.IsNullOrWhiteSpace(tokenObj?.ToString()))
        {
            _logger.Error("Bot token missing. Please set 'tg_bot_token' argument.");
            _logger.Notify("Нет tg_bot_token в аргументах");
            return false;
        }

        var newToken = tokenObj!.ToString();


        if (_innerEntry == null || _currentToken != newToken)
        {
            lock (_initLock)
            {
                if (_innerEntry == null || _currentToken != newToken)
                {
                    _logger.Info("Initializing Telegram Core...");
                    _currentToken = newToken;
                    _innerEntry = new PluginEntry(_currentToken, _sharedClient, _logger, _persistence);
                }
            }
        }

        _ = ExecuteAsync(args);
        return true;
    }

    private async Task<bool> ExecuteAsync(Dictionary<string, object> args)
    {
        if (_innerEntry != null) // немного оверхед ну ладно.
        {
            await _innerEntry.ExecuteAsync(args);
        }

        return true;
    }
}