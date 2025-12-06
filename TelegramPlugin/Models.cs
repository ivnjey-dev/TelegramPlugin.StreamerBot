using System.Collections.Generic;

namespace TelegramPlugin.Enums
{
    public enum MediaType
    {
        Unknown,
        Auto,
        Text,
        Photo,
        Video,
        PhotoUrl,
        VideoUrl
    }
}

namespace TelegramPlugin.Models
{
    public class OperationResult<T>
    {
        public bool IsSuccess { get; }
        public T Data { get; }
        public string? ErrorMessage { get; }
        public string? WarningMessage { get; }

        public bool HasWarning => !string.IsNullOrEmpty(WarningMessage);
        private OperationResult(bool success, T data, string? error, string? warning = null)
        {
            IsSuccess = success;
            Data = data;
            ErrorMessage = error;
            WarningMessage = warning;
        }

        public static OperationResult<T> Success(T data, string? warning = null) 
            => new(true, data, null, warning);
        
        public static OperationResult<T> Failure(string error) 
            => new(false, default(T), error);
    }

    public class BaseRequest
    {
        public long ChatId { get; set; }
        public int? TopicId { get; set; }
        public string? StateKey { get; set; }
        public bool DeleteAllKeys { get; set; }
    }

    public class SendRequest : BaseRequest
    {
        public string Text { get; set; }
        public string? MediaPath { get; set; }
        public Enums.MediaType MediaType { get; set; }

        public List<List<ButtonDto>> Buttons { get; set; } = [];

        public bool DeletePrevious { get; set; }
        public bool DeleteFile { get; set; }
        public bool Notification { get; set; } = true;
    }

    public class DeleteRequest : BaseRequest
    {
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