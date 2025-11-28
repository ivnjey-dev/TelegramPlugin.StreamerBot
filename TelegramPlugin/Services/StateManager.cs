using System.Collections.Generic;

namespace TelegramPlugin.Services
{
    public class StateManager(IPersistenceLayer store, string varKey = "TelegramPlugin.StateManager")
    {
        private Dictionary<string, int> LoadState()
        {
            var data = store.Get<Dictionary<string, int>?>(varKey);
            return data ?? new Dictionary<string, int>();
        }

        private void SaveState(Dictionary<string, int> state)
        {
            store.Set(varKey, state);
        }

        private string BuildCompositeKey(long chatId, int? topicId, string userKey)
        {
            return $"{chatId}:{topicId ?? 0}:{userKey}";
        }

        public int? GetMessageId(long chatId, int? topicId, string userKey)
        {
            if (string.IsNullOrEmpty(userKey)) return null;

            var state = LoadState();
            var compositeKey = BuildCompositeKey(chatId, topicId, userKey);

            return state.TryGetValue(compositeKey, out int id) ? id : null;
        }

        public void SetMessageId(long chatId, int? topicId, string userKey, int messageId)
        {
            if (string.IsNullOrEmpty(userKey)) return;

            var state = LoadState();
            var compositeKey = BuildCompositeKey(chatId, topicId, userKey);

            state[compositeKey] = messageId;
            SaveState(state);
        }

        public void RemoveMessageId(long chatId, int? topicId, string userKey)
        {
            if (string.IsNullOrEmpty(userKey)) return;

            var state = LoadState();
            var compositeKey = BuildCompositeKey(chatId, topicId, userKey);

            if (state.Remove(compositeKey))
            {
                SaveState(state);
            }
        }

        public void ClearAll()// по идее это куда то надо присобачить
        {
            store.Set<Dictionary<string, int>>(varKey, null);
        }

        public Dictionary<string, int> GetAllForChat(long chatId)
        {
            var state = LoadState();
            var result = new Dictionary<string, int>();
            var prefix = $"{chatId}:";

            foreach (var kvp in state)
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
            var state = LoadState();
            var prefix = $"{chatId}:";

            var keysToRemove = new List<string>();
            foreach (var key in state.Keys)
            {
                if (key.StartsWith(prefix)) keysToRemove.Add(key);
            }

            if (keysToRemove.Count > 0)
            {
                foreach (var key in keysToRemove) state.Remove(key);
                SaveState(state);
            }
        }
    }
}