using System.Net.Http;
using Microsoft.Extensions.Configuration;
using NUnit.Framework;
using TelegramPlugin.Enums;
using TelegramPlugin.Models;
using TelegramPlugin.Services;
using TelegramPlugin.Tests.Infrastructure;

namespace TelegramPlugin.Tests.Services
{
    public abstract class BaseGatewayTests
    {
        protected string TestToken;
        protected long TestChatId;
        protected int TestTopicId;
        protected string TestImagePath;
        protected string TestVideoPath;

        protected ITelegramGateway Gateway;
        protected HttpClient HttpClient;
        protected IPluginLogger Logger;
        protected string TempPhotoPath;
        protected string TempVideoPath;
        protected readonly SemaphoreSlim Gate = new(1, 1);

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            var builder = new ConfigurationBuilder().AddUserSecrets<BaseGatewayTests>();
            var configuration = builder.Build();
            HttpClient = new HttpClient();
            Logger = new TestConsoleLogger();
            Gateway = new TelegramGateway(TestToken, HttpClient, Logger);

            TestToken = configuration["ApiToken"];
            TestChatId = Convert.ToInt64(configuration["ChatId"]);
            TestTopicId = Convert.ToInt32(configuration["TopicId"]);
            TestImagePath = configuration["ImagePath"];
            TestVideoPath = configuration["VideoPath"];

            if (string.IsNullOrWhiteSpace(TestToken))
                Assert.Ignore($"Skipping  tests: {TestToken} not found. Please place a real API token.");

            TempPhotoPath = Path.GetFullPath(TestImagePath!);
            if (!File.Exists(TempPhotoPath))
                Assert.Ignore($"Skipping Media tests: {TempPhotoPath} not found. Please place a real PNG file there.");

            TempVideoPath = Path.GetFullPath(TestVideoPath!);
            if (!File.Exists(TempVideoPath))
                Assert.Ignore($"Skipping Media tests: {TempVideoPath} not found. Please place a real MP4 file there.");
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            HttpClient.Dispose();
        }

        protected SendRequest CreateRequest(string text = null, MediaType type = MediaType.Text)
        {
            return new SendRequest
            {
                ChatId = TestChatId,
                TopicId = TestTopicId,
                Text = text,
                MediaType = type
            };
        }
    }
}