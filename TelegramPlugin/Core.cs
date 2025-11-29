using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TelegramPlugin.Services;

namespace TelegramPlugin;

internal class PluginEntry
{
    private readonly SemaphoreSlim Gate = new(1, 1);
    private readonly InputParser _parser = new();
    private readonly Orchestrator _orchestrator;
    private readonly IPluginLogger _logger;
    // private HttpClient sharedClient;

    public PluginEntry(string token, HttpClient sharedClient, IPluginLogger logger, IPersistenceLayer persistence)
    {
        _logger = logger;
        var stateManager = new StateManager(persistence);
        var gateway = new TelegramGateway(token, sharedClient, logger);
        _orchestrator = new Orchestrator(gateway, stateManager, logger);
    }

    public async Task ExecuteAsync(Dictionary<string, object> args)
    {
        var parseResult = _parser.Parse(args);
        if (!parseResult.IsSuccess)
        {
            _logger.Error($"Config Error: {parseResult.ErrorMessage}");
            return;
        }

        // _logger.Info("ожидаем вход в поток...");
        await Gate.WaitAsync();
        // _logger.Info("вошли в поток...");

        try
        {
            await _orchestrator.ProcessRequestAsync(parseResult.Data); // check it
        }
        catch (System.Exception ex)
        {
            _logger.Error($"Execution Error: {ex.Message}");
        }
        finally
        {
            // _logger.Info("вышли из потока...");
            Gate.Release();
        }
    }
}