using System.Net.Http;
using NUnit.Framework;
using TelegramPlugin.Services;
using TelegramPlugin.Tests.Infrastructure;
using TelegramPlugin.Tests.Services;

namespace TelegramPlugin.Tests;

[TestFixture]
[Explicit("Integration Tests: Requires Telegram Token")]
public class PluginEntryRealTests : BaseGatewayTests
{
    private PluginEntry _plugin;
    private MockLogger _logger;
    private TestFilePersistence _persistence;

    [SetUp]
    public void Setup()
    {
        _logger = new MockLogger();
        _persistence = new TestFilePersistence();
        _persistence.Clear();

        var stateManager = new StateManager(_persistence);
        var orchestrator = new Orchestrator(Gateway, stateManager, _logger);
        var parser = new InputParser();

        _plugin = new PluginEntry(orchestrator, parser, _logger);
    }

    [Test]
    public async Task Real_SendPhoto_AndDeleteFile()
    {
        // 1. Создаем реальный файл
        var path = Path.ChangeExtension(Path.GetTempFileName(), ".jpg");
        // Копируем туда реальную картинку (или создаем фиктивную, если телега съест)
        // Лучше скопировать валидный хедер JPEG, иначе телега может отбить "Bad Request"
        // Но для теста удаления файла достаточно, чтобы Gateway попытался отправить.
        // Давай возьмем текстовый файл, переименуем в jpg. Телега может ругнуться, 
        // но Гейтвей вернет ошибку API, а НЕ ошибку файла.
        // А стоп, если Гейтвей вернет ошибку API, то PluginEntry НЕ удалит файл (безопасность).
        // Значит нужен ВАЛИДНЫЙ JPG.

        // Хак: скачаем логотип гугла и сохраним как файл
        using (var client = new HttpClient())
        {
            var bytes = await client.GetByteArrayAsync(
                "https://www.google.com/images/branding/googlelogo/1x/googlelogo_color_272x92dp.png");
            File.WriteAllBytes(path, bytes);
        }

        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", TestChatId },
            { "tg_text", "Integration Test: Photo Delete" },
            { "tg_media_path", path },
            { "tg_media_type", "photo" }, // Явно, т.к. это png, а мы хотим отправить как фото
            { "tg_delete_file", true },
            { "tg_notification", true }
        };

        TestContext.WriteLine($"Sending file: {path}");
        await _plugin.ExecuteSendAsync(args);

        // Проверяем логи на ошибки
        if (_logger.Errors.Count > 0)
        {
            Assert.Fail($"Plugin logged errors: {string.Join(", ", _logger.Errors)}");
        }

        // Проверяем удаление
        Assert.That(File.Exists(path), Is.False, "Real file should be deleted after successful send");
    }

    [Test]
    public async Task Real_SendUrl_NoExtension()
    {
        var args = new Dictionary<string, object>
        {
            { "tg_chat_id", TestChatId },
            { "tg_text", "Integration Test: URL No Ext" },
            { "tg_media_path", "https://avatars.githubusercontent.com/u/10251060" },
            { "tg_media_type", "photo" } // Explicit
        };

        await _plugin.ExecuteSendAsync(args);

        Assert.That(_logger.Errors, Is.Empty);
        Assert.That(_logger.Notifications.Count, Is.GreaterThan(0));
    }

    [Test]
    public async Task Real_DeleteAll()
    {
        // Сначала пошлем что-то, чтобы было что удалять (и сохраним в стейт)
        var argsSend = new Dictionary<string, object>
        {
            { "tg_chat_id", TestChatId },
            { "tg_text", "Message to delete" },
            { "tg_state_key", "temp_key" }
        };
        await _plugin.ExecuteSendAsync(argsSend);

        // Теперь удаляем всё
        var argsDel = new Dictionary<string, object>
        {
            { "tg_chat_id", TestChatId },
            { "tg_delete_all", true }
        };

        await _plugin.ExecuteDeleteAsync(argsDel);

        Assert.That(_logger.Errors, Is.Empty);
        // Проверить, что удалилось, сложно программно, но ошибок быть не должно.
    }
}