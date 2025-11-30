using NUnit.Framework;
using TelegramPlugin.Enums;

namespace TelegramPlugin.Tests.Services
{
    [TestFixture]
    public class ConcurrencyGatewayTests : BaseGatewayTests
    {
        [Test]
        [Category("Integration")]
        public async Task Hammering_Gateway_With_Parallel_Photos_Should_Not_Crash()
        {
            int parallelCount = 5;
            var tasks = new List<Task>();

            TestContext.WriteLine($"[START] Launching {parallelCount} parallel photo requests...");

            for (int i = 0; i < parallelCount; i++)
            {
                int id = i;
                tasks.Add(Task.Run(async () =>
                {
                    try
                    {
                        var request = CreateRequest($"Concurrent Img #{id}", MediaType.Photo);
                        request.MediaPath = TempPhotoPath;

                        TestContext.WriteLine($"[{id}] Sending request...");

                        int msgId = await Gateway.SendAsync(request);

                        TestContext.WriteLine($"[{id}] Success! MsgID: {msgId}");
                    }
                    catch (Exception ex)
                    {
                        TestContext.WriteLine($"[{id}] FAILED: {ex.GetType().Name} -> {ex.Message}");

                        if (ex.Message.Contains("ObjectDisposedException") ||
                            ex.Message.Contains("StringContent") ||
                            ex.Message.Contains("Content"))
                        {
                            Assert.Fail($"[BUG REPRODUCED] Request {id} failed with Disposed Content bug!");
                        }

                        if (ex is IOException)
                        {
                            TestContext.WriteLine($"[{id}] File Access Violation (Expected without Semaphore)");
                        }

                        throw;
                    }
                }));
            }

            try
            {
                await Task.WhenAll(tasks);
                TestContext.WriteLine("[DONE] All requests finished without exception.");
            }
            catch (Exception)
            {
                Assert.Fail("Concurrent batch execution failed. Check output for details.");
            }
        }

        [Test]
        [Category("Integration")]
        public async Task Hammering_Gateway_With_Mixed_Text_And_Photo()
        {
            var tasks = new List<Task>();

            tasks.Add(Task.Run(async () => await Gateway.SendAsync(CreateRequest("Text 1"))));
            tasks.Add(Task.Run(async () =>
            {
                var req = CreateRequest("Photo 1", MediaType.Photo);
                req.MediaPath = TempPhotoPath;
                await Gateway.SendAsync(req);
            }));
            tasks.Add(Task.Run(async () => await Gateway.SendAsync(CreateRequest("Text 2"))));
            tasks.Add(Task.Run(async () =>
            {
                var req = CreateRequest("Photo 2", MediaType.Photo);
                req.MediaPath = TempPhotoPath;
                await Gateway.SendAsync(req);
            }));

            try
            {
                await Task.WhenAll(tasks);
                Assert.Pass();
            }
            catch (Exception ex)
            {
                Assert.Fail($"Mixed concurrency failed: {ex.Message}");
            }
        }
    }
}