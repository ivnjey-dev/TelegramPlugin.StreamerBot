using System.Collections.Concurrent;
using System.Collections.Generic;

namespace TelegramPlugin.Services;

internal class StateManager(IPersistenceLayer store)
{
    private readonly ConcurrentDictionary<string, int> _cache = store.Get() ?? new ConcurrentDictionary<string, int>();
    private readonly object _cacheLock = new();

    private void SaveState()
    {
        lock (_cacheLock)
        {
            store.Set(_cache);
        }
    }

    private string BuildCompositeKey(long chatId, int? topicId, string userKey) =>
        $"{chatId}:{topicId ?? 0}:{userKey}";

    public int? GetMessageId(long chatId, int? topicId, string userKey) =>
        _cache.TryGetValue(BuildCompositeKey(chatId, topicId, userKey), out int id) ? id : null;

    public void SetMessageId(long chatId, int? topicId, string userKey, int messageId)
    {
        var compositeKey = BuildCompositeKey(chatId, topicId, userKey);
        _cache[compositeKey] = messageId;
        SaveState();
    }

    public void RemoveMessageId(long chatId, int? topicId, string userKey)
    {
        var compositeKey = BuildCompositeKey(chatId, topicId, userKey);

        if (_cache.TryRemove(compositeKey, out _))
        {
            SaveState();
        }
    }

    // todo по идее это куда то надо присобачить
    // public void ClearAll()
    // {
    //     store.Set(new ConcurrentDictionary<string, int>());
    // }

    public Dictionary<string, int> GetAllForChat(long chatId)
    {
        var result = new Dictionary<string, int>();
        var prefix = $"{chatId}:";

        foreach (var kvp in _cache)
        {
            if (kvp.Key.StartsWith(prefix))
            {
                result[kvp.Key] = kvp.Value;
            }
        }

        return result;
    }

    public void ClearForChat(long chatId)
    {
        var prefix = $"{chatId}:";

        var keysToRemove = new List<string>();
        foreach (var key in _cache.Keys)
        {
            if (key.StartsWith(prefix)) keysToRemove.Add(key);
        }

        if (keysToRemove.Count > 0)
        {
            foreach (var key in keysToRemove) _cache.TryRemove(key, out _);
            SaveState();
        }
    }
}