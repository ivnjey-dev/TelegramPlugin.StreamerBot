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

        var req = parseResult.Data;

        await _gate.WaitAsync();
        try
        {
            var result = await _orchestrator.ProcessRequestAsync(req);

            if (!result.IsSuccess)
            {
                _logger.Error(result.ErrorMessage!);
                _logger.Notify(result.ErrorMessage!);
                return;
            }

            if (req.Notification) _logger.Notify("Сообщение успешно отправлено!");

            if (req.DeleteFile && !string.IsNullOrWhiteSpace(req.MediaPath))
            {
                TryDeleteFile(req.MediaPath!);
            }
        }
        catch (System.Exception ex)
        {
            _logger.Error($"Critical Error: {ex.Message}");
            _logger.Notify($"Critical Error: {ex.Message}");
        }
        finally
        {
            _gate.Release();
        }
    }

    private void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path)) File.Delete(path);
        }

        catch (System.Exception ex)
        {
            _logger.Error($"Warning: Could not delete file '{path}': {ex.Message}");
        }
    }
}