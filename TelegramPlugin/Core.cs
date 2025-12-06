using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using TelegramPlugin.Services;

namespace TelegramPlugin;

internal class PluginEntry
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly IOrchestrator _orchestrator;
    private readonly InputParser _parser;
    private readonly IPluginLogger _logger;

    public PluginEntry(IOrchestrator orchestrator, InputParser parser, IPluginLogger logger)
    {
        _orchestrator = orchestrator;
        _parser = parser;
        _logger = logger;
    }

    public async Task ExecuteSendAsync(Dictionary<string, object> args)
    {
        var parseResult = _parser.ParseSend(args);
        if (!parseResult.IsSuccess)
        {
            _logger.Error($"Config Error: {parseResult.ErrorMessage}");
            _logger.Notify($"Config Error: {parseResult.ErrorMessage}");
            return;
        }

        if (parseResult.HasWarning) _logger.Warn(parseResult.WarningMessage!);

        var req = parseResult.Data;
        await ExecuteRequestAsync(async () =>
        {
            var result = await _orchestrator.ProcessSendRequestAsync(req);

            if (!result.IsSuccess)
            {
                _logger.Error(result.ErrorMessage!);
                _logger.Notify(result.ErrorMessage!);
                return;
            }

            if (req.Notification) _logger.Notify("Message sent successfully!");
            if (req.DeleteFile && !string.IsNullOrWhiteSpace(req.MediaPath))
                TryDeleteFile(req.MediaPath!);
        });
    }

    public async Task ExecuteDeleteAsync(Dictionary<string, object> args)
    {
        var delReq = _parser.ParseDelete(args);
        if (!delReq.IsSuccess)
        {
            _logger.Error($"Config Error: {delReq.ErrorMessage}");
            _logger.Notify($"Config Error: {delReq.ErrorMessage}");
            return;
        }

        var req = delReq.Data;
        await ExecuteRequestAsync(async () => await _orchestrator.ProcessDeleteRequestAsync(req));
    }

    private async Task ExecuteRequestAsync(Func<Task> callback)
    {
        await _gate.WaitAsync();
        try
        {
            await callback();
        }
        catch (Exception ex)
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

        catch (Exception ex)
        {
            _logger.Error($"Warning: Could not delete file '{path}': {ex.Message}");
        }
    }
}