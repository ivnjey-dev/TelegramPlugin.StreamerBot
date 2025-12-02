using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.Http;
using Streamer.bot.Plugin.Interface;
using TelegramPlugin.Services;

namespace TelegramPlugin.StreamerBot;

public class PluginMain
{
    private readonly HttpClient _sharedClient;
    private readonly ConcurrentDictionary<string, Lazy<PluginEntry>> _bots = new();
    private readonly IPluginLogger _logger;
    private readonly IPersistenceLayer _persistence;
    private readonly InputParser _parser;

    public PluginMain(IInlineInvokeProxy cph)
    {
        _sharedClient = new HttpClient();
        _logger = new Logger(cph, "Telegram Plugin Sender");
        _persistence = new ProxyPersistence(cph, "TelegramPlugin.StateManager", _logger);
        _parser = new InputParser();
    }

    public bool Send(Dictionary<string, object> args)
    {
        if (!args.TryGetValue("tg_bot_token", out var tokenObj) ||
            string.IsNullOrWhiteSpace(tokenObj?.ToString()))
        {
            _logger.Error("Bot token missing. Please set 'tg_bot_token'.");
            _logger.Notify("tg_bot_token is missing");
            return false;
        }

        var token = tokenObj!.ToString();

        var entry = _bots.GetOrAdd(token, t => new Lazy<PluginEntry>(() =>
        {
            _logger.Info($"Initializing Core for bot token ending in ...{t[Math.Max(0, t.Length - 4)..]}");
            var stateManager = new StateManager(_persistence);
            var gateway = new TelegramGateway(t, _sharedClient, _logger);
            var orchestrator = new Orchestrator(gateway, stateManager, _logger);
            return new PluginEntry(orchestrator, _parser, _logger);
        })).Value;

        _ = entry.ExecuteAsync(args);

        return true;
    }
}