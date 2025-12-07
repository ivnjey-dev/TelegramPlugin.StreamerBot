using NUnit.Framework;
using TelegramPlugin.Enums;
using TelegramPlugin.Models;
using TelegramPlugin.Services;
using TelegramPlugin.Tests.Infrastructure;

namespace TelegramPlugin.Tests.Services
{
    [TestFixture]
    public class OrchestratorTests : BaseGatewayTests
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
        public async Task FullLifecycle_SendReplaceDelete()
        {
            string myKey = "lifecycle_key";

            var req1 = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = TestTopicId,
                MediaType = MediaType.Text,
                Text = "Msg 1",
                StateKey = myKey,
                DeletePrevious = true
            };
            await _orchestrator.ProcessSendRequestAsync(req1);

            int? id1 = _stateManager.GetMessageId(TestChatId, 4, myKey);
            Assert.That(id1, Is.Not.Null.And.GreaterThan(0));

            var req2 = new SendRequest
            {
                ChatId = TestChatId,
                TopicId = null,
                MediaType = MediaType.Text,
                Text = "Msg 2 (Replaced)",
                StateKey = myKey,
                DeletePrevious = true
            };
            await _orchestrator.ProcessSendRequestAsync(req2);

            int? id2 = _stateManager.GetMessageId(TestChatId, null, myKey);
            Assert.That(id2, Is.Not.Null.And.Not.EqualTo(id1));

            var req3 = new SendRequest
            {
                ChatId = TestChatId,
                MediaType = MediaType.Text,
                Text = "Done",
                DeleteAllKeys = true
            };
            await _orchestrator.ProcessSendRequestAsync(req3);

            // Проверка: Стейт для этого ключа должен быть пуст
            int? id3 = _stateManager.GetMessageId(TestChatId, null, myKey);
            Assert.That(id3, Is.Null);
        }

        [Test]
        public async Task State_IsIsolated_BetweenChats()
        {
            var mockGateway = new MockGatewayForState();
            var orch = new Orchestrator(mockGateway, _stateManager, _logger);

            long chatA = -1002106947874;
            long chatB = -1002758981100;
            string key = "menu";

            // Чат А: Отправляет меню -> ID 10
            await orch.ProcessSendRequestAsync(new SendRequest { ChatId = chatA, Text = "A", StateKey = key });

            // Чат Б: Отправляет меню -> ID 20
            await orch.ProcessSendRequestAsync(new SendRequest { ChatId = chatB, Text = "B", StateKey = key });

            // Проверка: Ключи не пересеклись
            Assert.That(_stateManager.GetMessageId(chatA, null, key), Is.EqualTo(10));
            Assert.That(_stateManager.GetMessageId(chatB, null, key), Is.EqualTo(20));

            // Чат А: Удаляет своё меню
            await orch.ProcessSendRequestAsync(new SendRequest
                { ChatId = chatA, Text = "Del A", StateKey = key, DeletePrevious = true });

            // Проверка: У Чата А удалилось, у Чата Б осталось
            Assert.That(_stateManager.GetMessageId(chatB, null, key), Is.EqualTo(20));
            Assert.That(_stateManager.GetMessageId(chatA, null, key), Is.EqualTo(30));
        }

        [Test]
        public async Task State_IsIsolated_BetweenTopics()
        {
            var mockGateway = new MockGatewayForState();
            var orch = new Orchestrator(mockGateway, _stateManager, _logger);

            long chat = 100;
            int topic1 = 1;
            int topic2 = 2;
            string key = "alert";

            // Топик 1
            await orch.ProcessSendRequestAsync(new SendRequest
                { ChatId = chat, TopicId = topic1, Text = "T1", StateKey = key });

            // Топик 2
            await orch.ProcessSendRequestAsync(new SendRequest
                { ChatId = chat, TopicId = topic2, Text = "T2", StateKey = key });

            // Проверка
            Assert.That(_stateManager.GetMessageId(chat, topic1, key), Is.EqualTo(10));
            Assert.That(_stateManager.GetMessageId(chat, topic2, key), Is.EqualTo(20));
        }
    }

    public class MockGatewayForState : ITelegramGateway
    {
        private int _counter;

        public Task<OperationResult<Response>> SendAsync(SendRequest req)
        {
            _counter += 10;
            var response = new Response { MessageId = _counter };
            return Task.FromResult(OperationResult<Response>.Success(response));
        }

        public Task DeleteAsync(long chatId, int messageId)
        {
            return Task.CompletedTask;
        }
    }
}