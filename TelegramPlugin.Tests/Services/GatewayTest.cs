using NUnit.Framework;
using TelegramPlugin.Models;
using TelegramPlugin.Enums;
using TelegramPlugin.Tests.Services;

namespace TelegramPlugin.Tests
{
    [TestFixture]
    [Explicit("Requires real Telegram Token & Network")]
    public class GatewaySendTests : BaseGatewayTests
    {
        [Test]
        public async Task Send_TextOnly_Success()
        {
            var req = CreateRequest("Pure text message");
            int id = await Gateway.SendAsync(req);

            Assert.That(id, Is.GreaterThan(0));
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

            int id = await Gateway.SendAsync(req);
            Assert.That(id, Is.GreaterThan(0));
        }

        [Test]
        public async Task Send_Photo_Success()
        {
            var req = CreateRequest("Look at this photo", MediaType.Photo);
            req.MediaPath = TempPhotoPath;

            int id = await Gateway.SendAsync(req);
            Assert.That(id, Is.GreaterThan(0));
        }

        [Test]
        public async Task Send_Video_Success()
        {
            var req = CreateRequest("Watch this video", MediaType.Video);
            req.MediaPath = TempVideoPath;

            int id = await Gateway.SendAsync(req);
            Assert.That(id, Is.GreaterThan(0));
        }

        [Test]
        public async Task Send_Photo_NoCaption_Success()
        {
            var req = CreateRequest(null, MediaType.Photo); // Пустой текст
            req.MediaPath = TempPhotoPath;

            int id = await Gateway.SendAsync(req);
            Assert.That(id, Is.GreaterThan(0));
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

            int id = await Gateway.SendAsync(req);
            Assert.That(id, Is.GreaterThan(0));
        }

        [Test]
        public async Task Send_EmptyText_ShouldFailOrThrow()
        {
            var req = CreateRequest("");

            Assert.ThrowsAsync<Telegram.Bot.Exceptions.ApiRequestException>(async () =>
            {
                await Gateway.SendAsync(req);
            });
        }

        [Test]
        public async Task Send_EmptyText_WithButtons_Success()
        {
            var req = CreateRequest("   ");
            req.Buttons = new List<List<ButtonDto>> { new List<ButtonDto> { new ButtonDto("B", "https://t.me") } };

            Assert.ThrowsAsync<Telegram.Bot.Exceptions.ApiRequestException>(async () =>
            {
                await Gateway.SendAsync(req);
            });
        }
    }
}