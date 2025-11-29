using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlugin.Enums;
using TelegramPlugin.Extensions;
using TelegramPlugin.Models;

namespace TelegramPlugin.Services;

internal class TelegramGateway(string token, HttpClient httpClient, IPluginLogger logger) : ITelegramGateway
{
    private readonly TelegramBotClient _bot = new(token, httpClient);

    public async Task<int> SendAsync(SendRequest req)
    {
        var safeText = req.Text?.SmartEscape() ?? "";
        var markup = BuildMarkup(req.Buttons);

        var isTextOnly = req.MediaType == MediaType.Text ||
                         (req.MediaType == MediaType.Auto &&
                          (string.IsNullOrEmpty(req.MediaPath) || !File.Exists(req.MediaPath)));

        switch (isTextOnly)
        {
            case false when !File.Exists(req.MediaPath):
                return -1;
            case true:
            {
                var msg = await _bot.SendMessage(req.ChatId, safeText, ParseMode.Markdown, replyMarkup: markup,
                    messageThreadId: req.TopicId);
                return msg.MessageId;
            }
        }

        // Медиа
        using var stream = File.OpenRead(req.MediaPath!);
        var file = new Telegram.Bot.Types.InputFileStream(stream, Path.GetFileName(req.MediaPath));

        Telegram.Bot.Types.Message mediaMsg;
        if (req.MediaType == MediaType.Photo)
        {
            mediaMsg = await _bot.SendPhoto(req.ChatId, file, caption: safeText, parseMode: ParseMode.Markdown,
                replyMarkup: markup, messageThreadId: req.TopicId);
        }
        else
        {
            mediaMsg = await _bot.SendVideo(req.ChatId, file, caption: safeText, parseMode: ParseMode.Markdown,
                replyMarkup: markup, messageThreadId: req.TopicId, supportsStreaming: true);
        }

        return mediaMsg.MessageId;
    }


    public async Task DeleteAsync(long chatId, int messageId)
    {
        try
        {
            await _bot.DeleteMessage(chatId, messageId);
        }
        catch (Exception ex)
        {
            logger.Error(ex.Message);
        }
    }


    private InlineKeyboardMarkup? BuildMarkup(
        List<List<ButtonDto>>? buttons)
    {
        if (buttons == null || !buttons.Any()) return null;
        var rows = buttons.Select(row => row.Select(b => InlineKeyboardButton.WithUrl(b.Text, b.Url)).ToList())
            .ToList();
        return new InlineKeyboardMarkup(rows);
    }
}