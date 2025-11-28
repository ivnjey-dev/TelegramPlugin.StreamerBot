using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using TelegramPlugin.Services;

namespace TelegramPlugin;

public class PluginEntry
{
    private readonly TelegramGateway _gateway;
    private readonly InputParser _parser;

    public PluginEntry(string token, HttpClient sharedClient)
    {   
        _gateway = new TelegramGateway(token, sharedClient);
        _parser = new InputParser();
    }

    public async Task ExecuteAsync(
        Dictionary<string, object> args,
        IPersistenceLayer persistence,
        IPluginLogger logger)
    {
        var parseResult = _parser.Parse(args);
        if (!parseResult.IsSuccess)
        {
            logger.Error($"Config Error: {parseResult.ErrorMessage}");
            return;
        }

        var stateManager = new StateManager(persistence);
        var orchestrator = new Orchestrator(_gateway, stateManager, logger);

        await orchestrator.ProcessRequestAsync(parseResult.Data);
    }
}