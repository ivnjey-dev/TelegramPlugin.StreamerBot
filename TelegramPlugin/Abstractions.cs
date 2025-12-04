using System.Collections.Concurrent;
using System.Threading.Tasks;
using TelegramPlugin.Models;

namespace TelegramPlugin;

public interface ITelegramGateway
{
    Task<OperationResult<Response>> SendAsync(SendRequest req);
    Task DeleteAsync(long chatId, int messageId);
}

public interface IOrchestrator
{
    Task<OperationResult<Response>> ProcessSendRequestAsync(SendRequest req);
    Task ProcessDeleteRequestAsync(BaseRequest req, bool deletePrevious = true);
}

public interface IPersistenceLayer
{
    ConcurrentDictionary<string, int>? Get();
    void Set(ConcurrentDictionary<string, int> value);
}

public interface IPluginLogger
{
    void Info(string message);
    void Error(string message);
    void Notify(string message);
}