using System.Threading.Tasks;
using TelegramPlugin.Models;

namespace TelegramPlugin.Services;

internal class Orchestrator(ITelegramGateway gateway, StateManager stateManager, IPluginLogger logger) : IOrchestrator
{
    public async Task<OperationResult<Response>> ProcessRequestAsync(SendRequest req)
    {
        if (req.DeleteAllKeys)
        {
            foreach (var kvp in stateManager.GetAllForChat(req.ChatId))
                await gateway.DeleteAsync(req.ChatId, kvp.Value);
            stateManager.ClearForChat(req.ChatId);
            logger.Info("All previous messages deleted.");
        }
        else if (req.DeletePrevious && !string.IsNullOrWhiteSpace(req.StateKey))
        {
            var oldId = stateManager.GetMessageId(req.ChatId, req.TopicId, req.StateKey!);
            if (oldId.HasValue)
            {
                await gateway.DeleteAsync(req.ChatId, oldId.Value);
                stateManager.RemoveMessageId(req.ChatId, req.TopicId, req.StateKey!);
            }
        }

        var sendResult = await gateway.SendAsync(req);

        if (!sendResult.IsSuccess)
            return sendResult;

        if (!string.IsNullOrWhiteSpace(req.StateKey))
            stateManager.SetMessageId(req.ChatId, req.TopicId, req.StateKey!, sendResult.Data.MessageId);

        return sendResult;
    }
}