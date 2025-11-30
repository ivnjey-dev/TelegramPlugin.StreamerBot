using System;
using System.Threading.Tasks;
using TelegramPlugin.Models;

namespace TelegramPlugin.Services;

internal class Orchestrator(ITelegramGateway gateway, StateManager stateManager, IPluginLogger logger)
{
    public async Task ProcessRequestAsync(SendRequest req)
    {
        var chatMessages = stateManager.GetAllForChat(req.ChatId);

        if (req.DeleteAllKeys)
        {
            foreach (var kvp in chatMessages) await gateway.DeleteAsync(req.ChatId, kvp.Value);
            stateManager.ClearForChat(req.ChatId);
            logger.Info("All previous messages deleted.");
        }
        else if (req.DeletePrevious && !string.IsNullOrEmpty(req.StateKey))
        {
            var oldId = stateManager.GetMessageId(req.ChatId, req.TopicId, req.StateKey!);

            if (oldId.HasValue)
            {
                await gateway.DeleteAsync(req.ChatId, oldId.Value);
                stateManager.RemoveMessageId(req.ChatId, req.TopicId, req.StateKey!);
            }
        }

        try
        {
            var newId = await gateway.SendAsync(req);

            if (newId > 0 && !string.IsNullOrEmpty(req.StateKey))
            {
                stateManager.SetMessageId(req.ChatId, req.TopicId, req.StateKey!, newId);
            }
        }
        catch (Exception ex)
        {
            logger.Error($"Telegram API Error: {ex.Message}");
            logger.Notify($"Telegram API Error: {ex.Message}");
        }
    }
}