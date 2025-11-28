using System.Threading.Tasks;
using TelegramPlugin.Models;

namespace TelegramPlugin;

public interface ITelegramGateway
{
    Task<int> SendAsync(SendRequest req);
    Task DeleteAsync(long chatId, int messageId);
}

public interface IPersistenceLayer
{
    T Get<T>(string key);
    void Set<T>(string key, T value);
}

public interface IPluginLogger
{
    void Info(string message);

    void Error(string message);
    // void Norify(string prefix, string message);// todo: add notify log level
}