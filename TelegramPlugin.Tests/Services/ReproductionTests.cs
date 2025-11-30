using System.Net;
using NUnit.Framework;
using TelegramPlugin.Enums;
using TelegramPlugin.Models;

namespace TelegramPlugin.Tests.Services
{
    [TestFixture]
    public class ReproductionTests : BaseGatewayTests
    {
        [OneTimeSetUp]
        public void Setup()
        {
            var uri = new Uri("https://api.telegram.org");
            var sp = ServicePointManager.FindServicePoint(uri);
            sp.ConnectionLeaseTimeout = 0;
            sp.MaxIdleTime = 0;

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            ServicePointManager.DefaultConnectionLimit = 50;

            HttpClient.DefaultRequestHeaders.ConnectionClose = true;
            HttpClient.DefaultRequestHeaders.Add("Connection", "close");
            HttpClient.Timeout = TimeSpan.FromSeconds(100);

            // Logger = new TestConsoleLogger();
            // Gateway = new TelegramGateway(TEST_TOKEN, HttpClient, Logger);

            // if (!File.Exists(TEST_IMAGE_PATH))
            //     File.WriteAllBytes(TEST_IMAGE_PATH, new byte[1024 * 500]);
        }

        [Test]
        public async Task T1_Text_And_Text_Should_Not_Fail()
        {
            var task1 = Task.Run(() => ExecuteAction("Text_1", usePhoto: false));
            var task2 = Task.Run(() => ExecuteAction("Text_2", usePhoto: false));

            await Task.WhenAll(task1, task2);
        }

        [Test]
        public async Task T2_Text_And_Photo_FileStream()
        {
            var task1 = Task.Run(() => ExecuteAction("Text_Req", usePhoto: false));
            var task2 = Task.Run(() => ExecuteAction("Photo_Req", usePhoto: true));

            await Task.WhenAll(task1, task2);
        }

        [Test]
        public async Task T3_Photo_And_Photo_FileStream()
        {
            var task1 = Task.Run(() => ExecuteAction("Photo_1", usePhoto: true));
            var task2 = Task.Run(() => ExecuteAction("Photo_2", usePhoto: true));

            await Task.WhenAll(task1, task2);
        }


        [Test]
        public async Task T4_Photo_And_Photo_And_Text_And_Photo_FileStream()
        {
            var task1 = Task.Run(() => ExecuteAction("Фото", usePhoto: true));
            var task2 = Task.Run(() => ExecuteAction("Photo_2", usePhoto: true));
            var task3 = Task.Run(() => ExecuteAction("просто текст", usePhoto: false));
            var task4 = Task.Run(() => ExecuteAction("Photo_3", usePhoto: false));

            await Task.WhenAll(task1, task2, task3, task4);
        }

        [Test]
        public async Task T4_Photo_And_Video_FileStream()
        {
            var task1 = Task.Run(() => ExecuteAction("Фото", usePhoto: true));
            var task2 = Task.Run(() => ExecuteActionVideo("Video", useVideo: true));

            await Task.WhenAll(task1, task2);
        }


        private async Task ExecuteAction(string name, bool usePhoto)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Waiting for Gate...");
            await Gate.WaitAsync();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Entered Gate.");

            try
            {
                var req = new SendRequest
                {
                    ChatId = TestChatId,
                    TopicId = 4,
                    Text = $"Test {name}",
                    MediaType = usePhoto ? MediaType.Photo : MediaType.Text,
                    MediaPath = usePhoto ? TestImagePath : null
                };

                await Gateway.SendAsync(req);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Success!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: FAILED: {ex.Message}");

                if (ex.ToString().Contains("ObjectDisposedException"))
                    Assert.Fail($"BUG CAUGHT in {name}: ObjectDisposedException");
                else
                    throw;
            }
            finally
            {
                Gate.Release();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Released Gate.");
            }
        }

        private async Task ExecuteActionVideo(string name, bool useVideo)
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Waiting for Gate...");
            await Gate.WaitAsync();
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Entered Gate.");

            try
            {
                var req = new SendRequest
                {
                    ChatId = TestChatId,
                    TopicId = 4,
                    Text = $"Test {name}",
                    MediaType = useVideo ? MediaType.Video : MediaType.Text,
                    MediaPath = useVideo ? TestVideoPath : null
                };

                await Gateway.SendAsync(req);

                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Success!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: FAILED: {ex.Message}");

                if (ex.ToString().Contains("ObjectDisposedException"))
                    Assert.Fail($"BUG CAUGHT in {name}: ObjectDisposedException");
                else
                    throw;
            }
            finally
            {
                Gate.Release();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {name}: Released Gate.");
            }
        }
    }
}