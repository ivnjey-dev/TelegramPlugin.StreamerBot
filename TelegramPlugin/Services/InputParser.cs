using System;
using System.Collections.Generic;
using System.IO;
using TelegramPlugin.Enums;
using TelegramPlugin.Models;

namespace TelegramPlugin.Services;

internal class InputParser
{
    private readonly string _argBtnPrefix = "tg_btn_";
    private readonly string _argLayout = "tg_layout";
    private readonly string _tgChatId = "tg_chat_id";
    private readonly string _tgTopicId = "tg_topic_id";
    private readonly string _tgText = "tg_text";
    private readonly string _tgStateKey = "tg_state_key";
    private readonly string _tgDeletePrevious = "tg_delete_prev";
    private readonly string _tgDeleteAllKeys = "tg_delete_all";
    private readonly string _tgDeleteFile = "tg_delete_file";
    private readonly string _tgMediaPath = "tg_media_path";
    private readonly string _tgMediaType = "tg_media_type";
    private readonly string _tgNotification = "tg_notification";

    public OperationResult<SendRequest> ParseSend(IDictionary<string, object> rawArgs)
    {
        var args = new Dictionary<string, object>(rawArgs, StringComparer.OrdinalIgnoreCase);
        if (!TryGetLong(args, _tgChatId, out long chatId))
        {
            return OperationResult<SendRequest>.Failure($"{_tgChatId} is missing or invalid.");
        }

        var req = new SendRequest
        {
            ChatId = chatId,
            TopicId = TryGetInt(args, _tgTopicId, out var tid) ? tid : null,
            Text = GetString(args, _tgText) ?? "",
            StateKey = GetString(args, _tgStateKey),
            DeletePrevious = GetBool(args, _tgDeletePrevious),
            DeleteAllKeys = GetBool(args, _tgDeleteAllKeys),
            DeleteFile = GetBool(args, _tgDeleteFile),
        };
        if (args.TryGetValue(_tgNotification, out var valNotify) &&
            bool.TryParse(valNotify.ToString(), out var isNotification))
            req.Notification = isNotification;

        var mediaResult = ResolveMedia(args);
        if (!mediaResult.IsSuccess)
        {
            return OperationResult<SendRequest>.Failure(mediaResult.ErrorMessage!);
        }

        req.MediaType = mediaResult.Data;
        req.MediaPath = GetString(args, _tgMediaPath);
        var buttonsResult = CollectButtons(args);
        if (!buttonsResult.IsSuccess)
        {
            return OperationResult<SendRequest>.Failure(buttonsResult.ErrorMessage!);
        }

        var flatButtons = buttonsResult.Data;

        if (flatButtons.Count > 0)
        {
            req.Buttons = ApplyLayout(flatButtons, GetString(args, _argLayout));
        }

        return OperationResult<SendRequest>.Success(req);
    }

    public OperationResult<DeleteRequest> ParseDelete(IDictionary<string, object> rawArgs)
    {
        var args = new Dictionary<string, object>(rawArgs, StringComparer.OrdinalIgnoreCase);
        if (!TryGetLong(args, _tgChatId, out long chatId))
        {
            return OperationResult<DeleteRequest>.Failure("tg_chat_id is missing or invalid.");
        }

        var state = GetString(args, _tgStateKey);
        var deleteAllKeys = GetBool(args, _tgDeleteAllKeys);
        if (string.IsNullOrWhiteSpace(state) && !deleteAllKeys)
            return OperationResult<DeleteRequest>.Failure("tg_state_key or tg_delete_all is missing or invalid.");
        return OperationResult<DeleteRequest>.Success(new DeleteRequest
        {
            ChatId = chatId,
            StateKey = state,
            DeleteAllKeys = deleteAllKeys,
            TopicId = TryGetInt(args, _tgTopicId, out var tid) ? tid : null,
        });
    }

    // Определяет тип сообщения на основе запроса и наличия файла.
    private OperationResult<MediaType> ResolveMedia(IDictionary<string, object> args)
    {
        var path = GetString(args, _tgMediaPath);

        var typeStr = GetString(args, _tgMediaType)?.ToLower();

        switch (typeStr)
        {
            case "text":
                return OperationResult<MediaType>.Success(MediaType.Text);
            case "photo":
            case "video":
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    return OperationResult<MediaType>.Failure(
                        $"Explicit media type '{typeStr}' requested, but file is missing: {path}");
                }

                return typeStr switch
                {
                    "photo" when !path!.IsPhoto() => OperationResult<MediaType>.Failure(
                        $"Requested 'photo', but file extension is not an image: {path}"),
                    "video" when !path!.IsVideo() => OperationResult<MediaType>.Failure(
                        $"Requested 'video', but file extension is not a video: {path}"),
                    _ => OperationResult<MediaType>.Success(typeStr == "photo" ? MediaType.Photo : MediaType.Video)
                };
            }
        }

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
            return OperationResult<MediaType>.Success(MediaType.Text);

        return path!.IsPhoto()
            ? OperationResult<MediaType>.Success(MediaType.Photo)
            : OperationResult<MediaType>.Success(path!.IsVideo() ? MediaType.Video : MediaType.Text);
    }


    // Сбор списка кнопок
    private OperationResult<List<ButtonDto>> CollectButtons(IDictionary<string, object> args)
    {
        var list = new List<ButtonDto>();

        for (var i = 0; args.TryGetValue($"{_argBtnPrefix}text{i}", out var tObj) && tObj != null; i++)
        {
            var text = tObj.ToString();
            if (string.IsNullOrWhiteSpace(text))
                return OperationResult<List<ButtonDto>>.Failure($"Button text at index {i} is empty.");

            if (!args.TryGetValue($"{_argBtnPrefix}url{i}", out var uObj) || uObj == null)
                return OperationResult<List<ButtonDto>>.Failure($"Missing URL for button '{text}' (index {i}).");

            var url = uObj.ToString().Trim();
            if (string.IsNullOrWhiteSpace(url))
                return OperationResult<List<ButtonDto>>.Failure($"URL is empty for button '{text}' (index {i}).");

            list.Add(new ButtonDto(text, url));
        }

        return OperationResult<List<ButtonDto>>.Success(list);
    }


    // Сбор макета... todo душно конечно, перепишу на типы телеги позже
    private List<List<ButtonDto>> ApplyLayout(List<ButtonDto> flatList, string? layoutStr)
    {
        var result = new List<List<ButtonDto>>();

        // Парсим конфиг "2, 1, 3"
        var rowConfig = new Queue<int>();
        if (!string.IsNullOrEmpty(layoutStr))
        {
            var parts = layoutStr!.Split([',', ' ', ';'], StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
            {
                if (int.TryParse(p, out int n) && n > 0) rowConfig.Enqueue(n);
            }
        }

        int processed = 0;

        while (processed < flatList.Count)
        {
            // Берем число из очереди или остаток
            int take = rowConfig.Count > 0 ? rowConfig.Dequeue() : flatList.Count - processed;

            // Защита от выхода за границы (если конфиг 100, а осталось 2)
            if (processed + take > flatList.Count)
                take = flatList.Count - processed;

            var row = flatList.GetRange(processed, take);
            result.Add(row);

            processed += take;
        }

        return result;
    }

    private string? GetString(IDictionary<string, object> args, string key)
    {
        return args.TryGetValue(key, out var v) ? v?.ToString() : null;
    }

    private bool TryGetLong(IDictionary<string, object> args, string key, out long val)
    {
        val = 0;
        return args.TryGetValue(key, out var v) && long.TryParse(v?.ToString(), out val);
    }

    private bool TryGetInt(IDictionary<string, object> args, string key, out int val)
    {
        val = 0;
        return args.TryGetValue(key, out var v) && int.TryParse(v?.ToString(), out val);
    }

    private bool GetBool(IDictionary<string, object> args, string key)
    {
        return args.TryGetValue(key, out var v) && bool.TryParse(v?.ToString(), out bool b) && b;
    }
}