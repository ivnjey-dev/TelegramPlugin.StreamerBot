using NUnit.Framework;
using TelegramPlugin.Models;
using TelegramPlugin.Services;
using TelegramPlugin.Tests.Infrastructure;

namespace TelegramPlugin.Tests.Services;

[TestFixture]
public class OrchestratorRealTests : BaseGatewayTests
{
    private TestFilePersistence _persistence;
    private TestConsoleLogger _logger;
    private Orchestrator _orchestrator;
    private StateManager _stateManager;

    [SetUp]
    public void Setup()
    {
        _persistence = new TestFilePersistence();
        _persistence.Clear();

        _logger = new TestConsoleLogger();
        _stateManager = new StateManager(_persistence);

        _orchestrator = new Orchestrator(Gateway, _stateManager, _logger);
    }

    [Test]
    public async Task Scenario_SelectiveDelete_And_MarkdownStability()
    {
        string textA = "Message A: _italic_ *bold* `code` [link](google.com)";
        string keyA = "key_A";

        TestContext.WriteLine("1. Sending Msg A (Rich Text)...");
        await _orchestrator.ProcessRequestAsync(new SendRequest
        {
            ChatId = TestChatId,
            Text = textA,
            StateKey = keyA
        });

        int? idA = _stateManager.GetMessageId(TestChatId, null, keyA);
        Assert.That(idA, Is.Not.Null, "Msg A ID not saved");
        TestContext.WriteLine($"   Msg A saved as ID: {idA}");


        string textB = "Message B: Just simple text to stay alive.";
        string keyB = "key_B";

        TestContext.WriteLine("2. Sending Msg B...");
        await _orchestrator.ProcessRequestAsync(new SendRequest
        {
            ChatId = TestChatId,
            Text = textB,
            StateKey = keyB
        });

        int? idB = _stateManager.GetMessageId(TestChatId, null, keyB);
        Assert.That(idB, Is.Not.Null, "Msg B ID not saved");
        Assert.That(idB, Is.Not.EqualTo(idA), "IDs must differ");
        TestContext.WriteLine($"   Msg B saved as ID: {idB}");


        TestContext.WriteLine("3. Replacing Msg A (Should delete old A, keep B)...");
        await _orchestrator.ProcessRequestAsync(new SendRequest
        {
            ChatId = TestChatId,
            Text = "Message A v2 (Updated)",
            StateKey = keyA,
            DeletePrevious = true
        });

        // В этот момент в чате должно произойти:
        // 1. Сообщение A (верхнее) исчезло.
        // 2. Сообщение B (нижнее) ОСТАЛОСЬ.
        // 3. Появилось сообщение A v2 (самое нижнее).

        int? idA_new = _stateManager.GetMessageId(TestChatId, null, keyA);
        Assert.That(idA_new, Is.Not.EqualTo(idA), "Msg A ID should update");

        // Проверяем, что ID для B не изменился в стейте (мы его не трогали)
        int? idB_check = _stateManager.GetMessageId(TestChatId, null, keyB);
        Assert.That(idB_check, Is.EqualTo(idB), "Msg B ID should remain unchanged in state");


        // Удаление несуществующего (Bad ID) ---
        // Вручную подсунем в стейт фейковый ID
        string fakeKey = "fake_key";
        _stateManager.SetMessageId(TestChatId, null, fakeKey, 9999999); // ID которого нет

        TestContext.WriteLine("4. Trying to delete fake ID...");
        // Пытаемся удалить
        try
        {
            await _orchestrator.ProcessRequestAsync(new SendRequest
            {
                ChatId = TestChatId,
                Text = "Ignored",
                StateKey = fakeKey,
                DeletePrevious = true
            });
            // Если дошли сюда - значит исключение не вылетело (Gateway проглотил ошибку удаления)
            Assert.Pass("Gateway correctly handled deletion of non-existent message");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Should not throw on delete error: {ex.Message}");
        }
    }

    [Test]
    public async Task Markdown_StressTest()
    {
        string brokenMd = "This is *bold without close tag";

        try
        {
            await Gateway.SendAsync(new SendRequest
            {
                ChatId = TestChatId,
                Text = brokenMd,
                MediaType = Enums.MediaType.Text
            });
        }
        catch (Telegram.Bot.Exceptions.ApiRequestException)
        {
            TestContext.WriteLine("Caught expected Telegram API error for broken MD");
            Assert.Pass();
        }
    }

    [Test]
    public async Task Markdown_Chaos_Text_ShouldTriggerApiError()
    {
        // Набор строк, ломающих Markdown V1
        var brokenInputs = new[]
        {
            "1 Unclosed bold: *bold text",
            "2 Unclosed italic: _italic text",
            "3 Unclosed code: `code block",
            "4 Unclosed link bracket: [Google",
            "5 Unclosed link paren: (google.com)",
            // Ссылочный ад
            "6 Broken link syntax: [Link] (google.com)",
            "7 Nested brackets: [[Link]]",
            "8 Empty link: []()",
            "9 Link inside link: [Outer [Inner](url)](url)",
            "10 Link with newlines: [Link\nText](url)",
            // Хаос символов 
            "11 Math chaos: 2 * 2 *",
            "12 Snake_case_madness: var_name_final_v2",
            "13 Multiline code broken: `````, но если не закрыть?",
            "14 Mixed tags intersection: *bold _italic*",
            // Псевдо-графика и ASCII
            "15 Table-like: | Col1 | Col2 |",
            "16 Arrow: --> <--",
            "17 Quote: > Quote text",
            // Редкие
            "18 Underscore at start: _text",
            "19 Underscore at end: text_",
            "20 Backslash hell: \\ \\* \\_ \\[",
            "21 Emoji mix: 🤖 *bold* 💀 _italic_ 💩",

            // --- Инъекции ---
            "22 HTML injection: <b>Bold</b>",
            """
            23 Hardcore: [  (  {  # + - . ! | > = ] ) ~ @ % ^ ** ))) ((( -= == +++ =- $ ##`~ '' ~ \\\\\  /// / //  \\\ 'ff' 
            '  
            """,
            "24 Script injection: <script>alert(1)</script>"
        };


        foreach (var text in brokenInputs)
        {
            TestContext.WriteLine($"Testing chaos input: {text}");

            try
            {
                await _orchestrator.ProcessRequestAsync(new SendRequest
                {
                    ChatId = TestChatId,
                    Text = text,
                    MediaType = Enums.MediaType.Text
                });

                TestContext.WriteLine($"[WARNING] Telegram ACCEPTED: {text}");
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                TestContext.WriteLine($"[SUCCESS] Telegram rejected as expected: {ex.Message}");
            }
            catch (Exception ex)
            {
                Assert.Fail($"Unexpected exception type: {ex.GetType().Name}: {ex.Message}");
            }
        }
    }

    [Test]
    public async Task Markdown_Chaos_Caption_ShouldTriggerApiError()
    {
        // Caption (подпись к фото) имеет те же ограничения разметки, но иногда ведет себя иначе
        var brokenCaptions = new[]
        {
            "Broken caption *bold",
            "Math in caption: 10 * 20",
            "File_name_style_caption"
        };

        if (!File.Exists(TempPhotoPath))
            Assert.Ignore("Skipping Caption test: test_valid.jpg not found");

        foreach (var caption in brokenCaptions)
        {
            TestContext.WriteLine($"Testing chaos caption: {caption}");

            try
            {
                await _orchestrator.ProcessRequestAsync(new SendRequest
                {
                    ChatId = TestChatId,
                    Text = caption,
                    MediaType = Enums.MediaType.Photo,
                    MediaPath = TempPhotoPath
                });

                TestContext.WriteLine($"[WARNING] Telegram ACCEPTED caption: {caption}");
            }
            catch (Telegram.Bot.Exceptions.ApiRequestException ex)
            {
                TestContext.WriteLine($"[SUCCESS] Telegram rejected caption: {ex.Message}");
            }
        }
    }
}