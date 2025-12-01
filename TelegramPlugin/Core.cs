using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using TelegramPlugin.Services;

namespace TelegramPlugin;

internal class PluginEntry
{
    private readonly SemaphoreSlim _gate = new(1, 1);
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
            _logger.Notify($"Config Error: {parseResult.ErrorMessage}");
            return;
        }

        await _gate.WaitAsync();

        try
        {
            var result = await _orchestrator.ProcessRequestAsync(parseResult.Data);

            if (!result.IsSuccess)
            {
                _logger.Error($"Execution Error: {result.ErrorMessage}");
                if (!parseResult.Data.Notification) return;
                _logger.Notify($"Execution Error: {result.ErrorMessage}");
                return;
            }

            if (parseResult.Data.Notification)
                _logger.Notify($"Сообщение успешно отправлено!");

            if (parseResult.Data.DeleteFile &&
                !string.IsNullOrWhiteSpace(parseResult.Data.MediaPath) &&
                File.Exists(parseResult.Data.MediaPath))
            {
                File.Delete(parseResult.Data.MediaPath!);
            }
        }
        catch (System.Exception ex)
        {
            _logger.Error($"Execution Error: {ex.Message}");
            _logger.Notify($"Execution Error: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }
}