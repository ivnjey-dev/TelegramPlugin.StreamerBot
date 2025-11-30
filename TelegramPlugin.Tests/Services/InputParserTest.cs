using NUnit.Framework;
using TelegramPlugin.Services;
using TelegramPlugin.Enums;

namespace TelegramPlugin.Tests.Services
{
    [TestFixture]
    public class InputParserTests
    {
        private InputParser _parser;

        [SetUp]
        public void Setup()
        {
            _parser = new InputParser();
        }

        [Test]
        public void Parse_MissingChatId_ReturnsFailure()
        {
            var args = new Dictionary<string, object>();
            var result = _parser.Parse(args);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("tg_chatId"));
        }

        [Test]
        public void Parse_ValidChatId_ReturnsSuccess()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chatId", "-100123" },
                { "tg_text", "Test" }
            };
            var result = _parser.Parse(args);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data?.ChatId, Is.EqualTo(-100123));
            Assert.That(result.Data.Text, Is.EqualTo("Test"));
        }

        [Test]
        public void Parse_Buttons_CollectsSequentialOnly()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chatId", 123 },
                { "tg_btn_text0", "Zero" }, { "tg_btn_url0", "u0" },
                { "tg_btn_text1", "One" }, { "tg_btn_url1", "u1" },
                // 3 - игнорируется? так как 2 пропущен
                { "tg_btn_text3", "Three" }, { "tg_btn_url3", "u3" }
            };

            var result = _parser.Parse(args);
            var buttons = result.Data.Buttons; // List<List<ButtonDto>>

            Assert.That(buttons.Count, Is.EqualTo(1));
            Assert.That(buttons[0].Count, Is.EqualTo(2)); 
            Assert.That(buttons[0][0].Text, Is.EqualTo("Zero"));
            Assert.That(buttons[0][1].Text, Is.EqualTo("One"));
        }

        [Test]
        public void Parse_ButtonMissingUrl_ReturnsFailure()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chatId", 123 },
                { "tg_btn_text0", "BtnWithoutUrl" }
            };

            var result = _parser.Parse(args);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("Missing URL"));
        }

        [Test]
        public void Parse_Layout_SplitsCorrectly()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chatId", 123 },
                { "tg_btn_text0", "A" }, { "tg_btn_url0", "u" },
                { "tg_btn_text1", "B" }, { "tg_btn_url1", "u" },
                { "tg_btn_text2", "C" }, { "tg_btn_url2", "u" },
                { "tg_layout", "2, 1" } // 2 в первой, 1 во второй
            };

            var result = _parser.Parse(args);
            var rows = result.Data.Buttons;

            Assert.That(rows.Count, Is.EqualTo(2), "Should have 2 rows");
            Assert.That(rows[0].Count, Is.EqualTo(2), "Row 1 should have 2 btns");
            Assert.That(rows[1].Count, Is.EqualTo(1), "Row 2 should have 1 btn");
            Assert.That(rows[0][0].Text, Is.EqualTo("A"));
            Assert.That(rows[1][0].Text, Is.EqualTo("C"));
        }

        [Test]
        public void ResolveMedia_ExplicitPhotoMissingFile_ReturnsFailure()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chatId", 123 },
                { "tg_media_type", "photo" },
                { "tg_media_path", "non_existent.jpg" }
            };

            var result = _parser.Parse(args);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Does.Contain("file is missing"));
        }

        [Test]
        public void ResolveMedia_AutoMissingFile_ReturnsTextMode()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chatId", 123 },
                { "tg_media_type", "auto" },
                { "tg_media_path", "non_existent.jpg" }
            };

            var result = _parser.Parse(args);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data.MediaType, Is.EqualTo(MediaType.Text));
        }
    }
}