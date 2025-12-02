using NUnit.Framework;
using TelegramPlugin.Enums;
using TelegramPlugin.Models;

namespace TelegramPlugin.Tests.Services
{
    [TestFixture]
    [Explicit("Requires real Telegram Token & Network")]
    public class GatewaySendTests : BaseGatewayTests
    {
        [Test]
        public async Task SendAsync_FileNotFound_ReturnsFailure()
        {
            var req = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = 4,
                Text = "Test",
                MediaType = MediaType.Photo,
                MediaPath = "/nonexistent/path/file.jpg"
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.ErrorMessage, Contains.Substring("[ERROR] File not found"));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task SendAsync_EmptyPath_ReturnsFailure()
        {
            var req = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = 4,
                Text = "Test",
                MediaType = MediaType.Photo,
                MediaPath = ""
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.ErrorMessage, Contains.Substring("[ERROR] File not found"));
            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task SendAsync_NullPath_TreatsAsTextOnly()
        {
            var req = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = 4,
                Text = "Text only",
                MediaType = MediaType.Auto,
                MediaPath = null
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Data?.MessageId, Is.GreaterThan(0));
        }

        [Test]
        public async Task SendAsync_TypePhoto_NullPath_Failure()
        {
            var req = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = TestTopicId,
                Text = "Text only",
                MediaType = MediaType.Photo,
                MediaPath = ""
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Contains.Substring("[ERROR] File not found"));
        }

        [Test]
        public async Task SendAsync_TypePhoto_WrongPath_Failure()
        {
            var req = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = TestTopicId,
                Text = "Text only",
                MediaType = MediaType.Photo,
                MediaPath = null
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Contains.Substring("[ERROR] File not found"));
        }

        [Test]
        public async Task Send_TextOnly_Success()
        {
            var req = CreateRequest("Pure text message");

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task Send_TextWithButtons_Success()
        {
            var req = CreateRequest("Message with buttons");
            req.Buttons = new List<List<ButtonDto>>
            {
                new List<ButtonDto>
                    { new ButtonDto("Google", "https://google.com"), new ButtonDto("Bing", "https://bing.com") },
                new List<ButtonDto> { new ButtonDto("Streamer.bot", "https://streamer.bot") }
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task Send_Photo_Success()
        {
            var req = CreateRequest("Look at this photo", MediaType.Photo);
            req.MediaPath = TempPhotoPath;

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task Send_Video_Success()
        {
            var req = CreateRequest("Watch this video", MediaType.Video);
            req.MediaPath = TempVideoPath;

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task Send_Photo_NoCaption_Success()
        {
            var req = CreateRequest(null, MediaType.Photo);
            req.MediaPath = TempPhotoPath;

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
        }

        [Test]
        public async Task Send_Photo_WithButtons_Success()
        {
            var req = CreateRequest("Photo + Buttons", MediaType.Photo);
            req.MediaPath = TempPhotoPath;
            req.Buttons = new List<List<ButtonDto>>
            {
                new List<ButtonDto> { new ButtonDto("Click Me", "https://t.me") }
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.True);
        }
        [Test]
        public async Task Send_WrongButton_Failure()
        {
            var req = CreateRequest("Wrong Buttons");
            req.Buttons = new List<List<ButtonDto>>
            {
                new List<ButtonDto> { new ButtonDto("Click Me", "google.com ") }
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.False);
        }

        [Test]
        public async Task Send_EmptyText_ShouldFailOrThrow()
        {
            var req = CreateRequest("");

            var result = await Gateway.SendAsync(req);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Contains.Substring("Telegram API error"));
        }

        [Test]
        public async Task Send_EmptyText_WithButtons_Success()
        {
            var req = CreateRequest("   ");
            req.Buttons = new List<List<ButtonDto>> { new List<ButtonDto> { new ButtonDto("B", "https://t.me") } };
            
            var result = await Gateway.SendAsync(req);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Contains.Substring("Telegram API error"));
        }

        [Test]
        public async Task SendAsync_InvalidChatId_ReturnsFailure()
        {
            var req = new SendRequest
            {
                ChatId = -9999999999,
                Text = "This will fail"
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Contains.Substring("Telegram API error"));
        }

        [Test]
        public async Task SendAsync_ApiError_CaughtAndWrapped()
        {
            var req = new SendRequest
            {
                ChatId = TestChatId,
                Text = new string('a', 5000)
            };

            var result = await Gateway.SendAsync(req);

            Assert.That(result, Is.Not.Null);
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.ErrorMessage, Is.Not.Null);
        }
    }
}