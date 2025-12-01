using System.Collections.Generic;
using System.Threading.Tasks;
using TelegramPlugin.Models;

namespace TelegramPlugin;

public interface ITelegramGateway
{
    Task<OperationResult<Response>> SendAsync(SendRequest req);
    Task DeleteAsync(long chatId, int messageId);
}

public interface IPersistenceLayer
{
    Dictionary<string, int>? Get();
    void Set(Dictionary<string, int> value);
}

public interface IPluginLogger
{
    void Info(string message);

    void Error(string message);
    void Notify(string message);
}