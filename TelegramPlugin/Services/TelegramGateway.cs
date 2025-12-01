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
    public async Task<OperationResult<Response>> SendAsync(SendRequest req)
    {
        try
        {
            var bot = new TelegramBotClient(token, httpClient);
            var safeText = req.Text?.SmartEscape() ?? "";
            var markup = BuildMarkup(req.Buttons);

            var isTextOnly = req.MediaType == MediaType.Text ||
                             (req.MediaType == MediaType.Auto &&
                              (string.IsNullOrEmpty(req.MediaPath) || !File.Exists(req.MediaPath)));
            if (!isTextOnly && !File.Exists(req.MediaPath))
                return OperationResult<Response>.Failure("[ERROR] File not found");
            var msgId = isTextOnly
                ? (await bot.SendMessage(req.ChatId, safeText, ParseMode.Markdown, replyMarkup: markup,
                    messageThreadId: req.TopicId)).MessageId
                : await SendMediaAsync(bot, req, safeText, markup);
            //todo определить неудачная отправка всегда вызовет исключения?
            //или же мы получим просто какой то ответ

            return OperationResult<Response>.Success(new Response { MessageId = msgId });
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            logger.Error($"Telegram API Error {ex.ErrorCode}: {ex.Message}");
            return OperationResult<Response>.Failure($"Telegram API error: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.Error($"Gateway error: {ex.Message}");
            return OperationResult<Response>.Failure($"Error: {ex.Message}");
        }
    }

    private async Task<int> SendMediaAsync(ITelegramBotClient bot, SendRequest req, string caption,
        InlineKeyboardMarkup? markup)
    {
        using var stream = File.OpenRead(req.MediaPath!);
        var file = new Telegram.Bot.Types.InputFileStream(stream, Path.GetFileName(req.MediaPath));

        var msg = req.MediaType == MediaType.Photo
            ? await bot.SendPhoto(req.ChatId, file, caption: caption, parseMode: ParseMode.Markdown,
                replyMarkup: markup, messageThreadId: req.TopicId)
            : await bot.SendVideo(req.ChatId, file, caption: caption, parseMode: ParseMode.Markdown,
                replyMarkup: markup, messageThreadId: req.TopicId, supportsStreaming: true);

        return msg.MessageId;
    }

    public async Task DeleteAsync(long chatId, int messageId)
    {
        try
        {
            var bot = new TelegramBotClient(token, httpClient);
            await bot.DeleteMessage(chatId, messageId);
        }
        catch (Exception ex)
        {
            logger.Error($"Delete error: {ex.Message}");
        }
    }

    private InlineKeyboardMarkup? BuildMarkup(List<List<ButtonDto>>? buttons)
    {
        if (buttons == null || !buttons.Any()) return null;
        var rows = buttons.Select(row => row.Select(b => InlineKeyboardButton.WithUrl(b.Text, b.Url)).ToList())
            .ToList();
        return new InlineKeyboardMarkup(rows);
    }
}