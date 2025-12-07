using NUnit.Framework;
using TelegramPlugin.Models;
using TelegramPlugin.Services;

namespace TelegramPlugin.Tests.Services
{
    [TestFixture]
    public class PluginEntryTests
    {
        private MockLogger _logger;
        private MockOrchestrator _orchestrator;
        private InputParser _parser;
        private PluginEntry _plugin;
        private string _tempFile;

        [SetUp]
        public void Setup()
        {
            _logger = new MockLogger();
            _orchestrator = new MockOrchestrator();
            _parser = new InputParser();

            _plugin = new PluginEntry(_orchestrator, _parser, _logger);

            var tmpPath = Path.GetTempFileName();
            var pngPath = Path.ChangeExtension(tmpPath, "png");
            File.Move(tmpPath, pngPath);
            _tempFile = pngPath;
        }

        [TearDown]
        public void TearDown()
        {
            if (File.Exists(_tempFile)) File.Delete(_tempFile);
        }


        [Test]
        public async Task Execute_ConfigError_LogsError_OrchestratorNotCalled()
        {
            var args = new Dictionary<string, object>();

            await _plugin.ExecuteSendAsync(args);

            Assert.That(_logger.Errors.Count, Is.GreaterThan(0));
            Assert.That(_logger.Notifications.Count, Is.GreaterThan(0));
            Assert.That(_logger.Errors[0], Contains.Substring("Config Error"));

            Assert.That(_orchestrator.CallCount, Is.EqualTo(0));
        }


        [Test]
        public async Task Execute_Success_CallsOrchestrator_AndNotifies()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_text", "Hello" },
                { "tg_notification", true }
            };

            _orchestrator.ResultToReturn = OperationResult<Response>.Success(new Response { MessageId = 42 });

            await _plugin.ExecuteSendAsync(args);

            Assert.That(_orchestrator.CallCount, Is.EqualTo(1));
            Assert.That(_logger.Notifications.Count, Is.EqualTo(1));
            Assert.That(_logger.Notifications[0], Contains.Substring("successfully"));
            Assert.That(_logger.Errors, Is.Empty);
        }

        [Test]
        public async Task Execute_Success_NoNotification_IfDisabled()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_text", "Hello" },
                { "tg_notification", false } // Выключено
            };

            _orchestrator.ResultToReturn = OperationResult<Response>.Success(new Response { MessageId = 42 });

            await _plugin.ExecuteSendAsync(args);

            Assert.That(_logger.Notifications, Is.Empty);
        }


        [Test]
        public async Task Execute_DeleteFile_DeletesRealFile()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_media_path", _tempFile },
                { "tg_delete_file", true }
            };

            _orchestrator.ResultToReturn = OperationResult<Response>.Success(new Response { MessageId = 42 });

            await _plugin.ExecuteSendAsync(args);

            foreach (var err in _logger.Errors) Console.WriteLine($"ERR: {err}");
            foreach (var n in _logger.Notifications) Console.WriteLine($"NOT: {n}");
            Assert.That(File.Exists(_tempFile), Is.False, "Файл должен удалиться после успеха");
        }


        [Test]
        public async Task Execute_DeleteFile_DoesNotDelete_IfOrchestratorFails()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_media_path", _tempFile },
                { "tg_delete_file", true }
            };

            _orchestrator.ResultToReturn = OperationResult<Response>.Failure("Something went wrong");

            await _plugin.ExecuteSendAsync(args);

            Assert.That(File.Exists(_tempFile), Is.True, "Файл НЕ должен удаляться при ошибке");
        }


        [Test]
        public async Task Execute_OrchestratorFailure_LogsError()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_text", "Fail" }
            };

            _orchestrator.ResultToReturn = OperationResult<Response>.Failure("Telegram API Error");

            await _plugin.ExecuteSendAsync(args);

            Assert.That(_logger.Errors.Count, Is.EqualTo(1));
            Assert.That(_logger.Notifications.Count, Is.EqualTo(1));
            Assert.That(_logger.Errors[0], Is.EqualTo("Telegram API Error"));
        }

        [Test]
        public async Task Execute_CriticalException_LogsCritical()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_text", "Crash" }
            };

            // Мок выбрасывает исключение (эмуляция бага в коде оркестратора)
            _orchestrator.ExceptionToThrow = new Exception("NRE inside");

            await _plugin.ExecuteSendAsync(args);

            Assert.That(_logger.Errors.Count, Is.EqualTo(1));
            Assert.That(_logger.Errors[0], Contains.Substring("Critical Error"));
            Assert.That(_logger.Errors[0], Contains.Substring("NRE inside"));
        }
        // === НОВЫЕ ТЕСТЫ ДЛЯ DELETE ===

        [Test]
        public async Task ExecuteDelete_Success_CallsOrchestrator()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_state_key", "menu_key" },
                { "tg_delete_all", false }
            };

            // Настраиваем мок (хотя для Delete метод void/Task, но проверим CallCount)
            // В MockOrchestrator надо реализовать ProcessDeleteRequestAsync
            
            await _plugin.ExecuteDeleteAsync(args);

            Assert.That(_orchestrator.DeleteCallCount, Is.EqualTo(1));
            Assert.That(_orchestrator.LastDeleteRequest, Is.Not.Null);
            Assert.That(_orchestrator.LastDeleteRequest.StateKey, Is.EqualTo("menu_key"));
            Assert.That(_logger.Errors, Is.Empty);
        }

        [Test]
        public async Task ExecuteDelete_ConfigError_LogsError()
        {
            // Ошибка: нет state_key и нет delete_all
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" }
            };

            await _plugin.ExecuteDeleteAsync(args);

            Assert.That(_orchestrator.DeleteCallCount, Is.EqualTo(0));
            Assert.That(_logger.Errors.Count, Is.GreaterThan(0));
            Assert.That(_logger.Errors[0], Contains.Substring("Config Error"));
        }

        [Test]
        public async Task ExecuteDelete_OrchestratorThrows_LogsCritical()
        {
            var args = new Dictionary<string, object>
            {
                { "tg_chat_id", "123" },
                { "tg_delete_all", true }
            };

            _orchestrator.ExceptionToThrow = new Exception("Delete failed hard");

            await _plugin.ExecuteDeleteAsync(args);

            Assert.That(_logger.Errors.Count, Is.EqualTo(1));
            Assert.That(_logger.Errors[0], Contains.Substring("Critical Error"));
        }

    }

    // === MOCKS ===

    public class MockOrchestrator : IOrchestrator
    {
        public int CallCount;
        public int DeleteCallCount;
        public BaseRequest LastDeleteRequest;
        public OperationResult<Response> ResultToReturn = OperationResult<Response>.Success(new Response());
        public Exception ExceptionToThrow;

        public Task<OperationResult<Response>> ProcessSendRequestAsync(SendRequest req)
        {
            CallCount++;
            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.FromResult(ResultToReturn);
        }

        public Task ProcessDeleteRequestAsync(BaseRequest req, bool deletePrevious = true)
        {
            DeleteCallCount++;
            LastDeleteRequest = req;

            if (ExceptionToThrow != null) throw ExceptionToThrow;
            return Task.CompletedTask;
        }
    }

    public class MockLogger : IPluginLogger
    {
        public List<string> Errors = new();
        public List<string> Notifications = new();

        public void Error(string message) => Errors.Add(message);

        public void Info(string message)
        {
        }

        public void Notify(string message) => Notifications.Add(message);

        public void Warn(string message)
        {
        }
    }
}