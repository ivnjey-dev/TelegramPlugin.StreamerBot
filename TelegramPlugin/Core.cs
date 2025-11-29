using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TelegramPlugin.Services;

namespace TelegramPlugin;

internal class PluginEntry
{
    private readonly InputParser _parser = new();
    private readonly Orchestrator _orchestrator;
    private readonly IPluginLogger _logger;

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

        await _orchestrator.ProcessRequestAsync(parseResult.Data);
    }
}