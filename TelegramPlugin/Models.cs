using System.Collections.Generic;
using Telegram.Bot.Types;

namespace TelegramPlugin.Enums
{
    public enum MediaType
    {
        Auto,
        Text,
        Photo,
        Video
    }
}

namespace TelegramPlugin.Models
{
    public class OperationResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string? ErrorMessage { get; }

        private OperationResult(bool success, T data, string? error)
        {
            IsSuccess = success;
            Data = data;
            ErrorMessage = error;
        }

        public static OperationResult<T> Success(T data) => new(true, data, null);
        public static OperationResult<T> Failure(string error) => new(false, default(T), error);
    }

    public class SendRequest
    {
        public long ChatId { get; set; }
        public int? TopicId { get; set; }
        public string? Text { get; set; }
        public string? MediaPath { get; set; }
        public Enums.MediaType MediaType { get; set; }

        public List<List<ButtonDto>> Buttons { get; set; } = [];

        // Управление состоянием
        public string? StateKey { get; set; }
        public bool DeletePrevious { get; set; }
        public bool DeleteAllKeys { get; set; }
        public bool DeleteFile { get; set; }
        public bool Notification { get; set; } = true;
    }

    public class Response
    {
        // public Message Message { get; set; }// добавлю если понадобится...
        public int MessageId { get; set; }
    }

    public class ButtonDto(string text, string url)
    {
        public string Text { get; set; } = text;
        public string Url { get; set; } = url;
    }
}