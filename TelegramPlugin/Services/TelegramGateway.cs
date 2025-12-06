using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using TelegramPlugin.Enums;
using TelegramPlugin.Models;
using File = System.IO.File;

namespace TelegramPlugin.Services;

internal class TelegramGateway(string token, HttpClient httpClient, IPluginLogger logger) : ITelegramGateway
{
    public async Task<OperationResult<Response>> SendAsync(SendRequest req)
    {
        try
        {
            var bot = new TelegramBotClient(token, httpClient);
            var markup = BuildMarkup(req.Buttons);

            switch (req.MediaType)
            {
                case MediaType.Text:
                {
                    var msg = await bot.SendMessage(req.ChatId, req.Text, ParseMode.Markdown, replyMarkup: markup,
                        messageThreadId: req.TopicId);
                    return OperationResult<Response>.Success(new Response { MessageId = msg.MessageId });
                }
                case MediaType.PhotoUrl or MediaType.VideoUrl:
                    return await SendMediaAsync(bot, req, InputFile.FromUri(req.MediaPath!), markup!);
            }

            using var stream = File.OpenRead(req.MediaPath!);
            var inputFile = InputFile.FromStream(stream, Path.GetFileName(req.MediaPath));
            return await SendMediaAsync(bot, req, inputFile, markup!);
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException ex)
        {
            logger.Error($"Telegram API Error {ex.ErrorCode}: {ex.Message}");
            return OperationResult<Response>.Failure($"Telegram API error: {ex.Message}");
        }

        catch (FileNotFoundException ex)
        {
            logger.Error($"File missing during send: {ex.FileName}");
            return OperationResult<Response>.Failure($"File not found: {ex.FileName}");
        }

        catch (Exception ex)
        {
            logger.Error($"Gateway error: {ex.Message}");
            return OperationResult<Response>.Failure($"Error: {ex.Message}");
        }
    }


    private async Task<OperationResult<Response>> SendMediaAsync(TelegramBotClient bot, SendRequest req,
        InputFile inputFile, InlineKeyboardMarkup markup)
    {
        var msg = req.MediaType is MediaType.Photo or MediaType.PhotoUrl
            ? await bot.SendPhoto(req.ChatId, inputFile, caption: req.Text, parseMode: ParseMode.Markdown,
                replyMarkup: markup, messageThreadId: req.TopicId)
            : await bot.SendVideo(req.ChatId, inputFile, caption: req.Text, parseMode: ParseMode.Markdown,
                replyMarkup: markup, messageThreadId: req.TopicId, supportsStreaming: true);

        return OperationResult<Response>.Success(new Response { MessageId = msg.MessageId });
    }


    private InlineKeyboardMarkup? BuildMarkup(List<List<ButtonDto>>? buttons)
    {
        if (buttons == null || !buttons.Any()) return null;
        var rows = buttons.Select(row => row.Select(b => InlineKeyboardButton.WithUrl(b.Text, b.Url)).ToList())
            .ToList();
        return new InlineKeyboardMarkup(rows);
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
}

// public async Task EditMessageAsync(ITelegramBotClient bot, SendRequest req, string caption,

//     InlineKeyboardMarkup? markup)

// {

// }


// public async Task EditMediaAsync(long chatId, int messageId, string text)

// {

//     try

//     {

//         var bot = new TelegramBotClient(token, httpClient);

//         // await bot.EditMessageMedia(chatId, messageId,);

//     }

//     catch (Exception ex)

//     {

//         logger.Error($"Edit error: {ex.Message}");

//     }

// }

// public async Task<OperationResult<Response>> EditAsync(SendRequest req)

// {

//     try

//     {

//         var bot = new TelegramBotClient(token, httpClient);

//         var safeText = req.Text?.SmartEscape() ?? "";

//         var markup = BuildMarkup(req.Buttons);

//         // await bot.EditMessageText(chatId, messageId, text);

//         var isTextOnly = req.MediaType == MediaType.Text ||

//                          (req.MediaType == MediaType.Auto &&

//                           (string.IsNullOrEmpty(req.MediaPath) || !File.Exists(req.MediaPath)));

//         if (!isTextOnly && !File.Exists(req.MediaPath))

//             return OperationResult<Response>.Failure("[ERROR] File not found");

//         var msgId = isTextOnly

//             ? (await bot.SendMessage(req.ChatId, safeText, ParseMode.Markdown, replyMarkup: markup,

//                 messageThreadId: req.TopicId)).MessageId

//             : await SendMediaAsync(bot, req, safeText, markup);

//

//         return OperationResult<Response>.Success(new Response { MessageId = msgId });

//     }

//     catch (Telegram.Bot.Exceptions.ApiRequestException ex)

//     {

//         logger.Error($"Telegram API Error {ex.ErrorCode}: {ex.Message}");

//         return OperationResult<Response>.Failure($"Telegram API error: {ex.Message}");

//     }

//     catch (Exception ex)

//     {

//         logger.Error($"Gateway error: {ex.Message}");

//         return OperationResult<Response>.Failure($"Error: {ex.Message}");

//     }

// }