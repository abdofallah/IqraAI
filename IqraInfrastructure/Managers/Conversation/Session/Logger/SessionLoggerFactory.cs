using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;

namespace IqraInfrastructure.Managers.Conversation.Session.Logger
{
    public class SessionLoggerFactory : ILoggerFactory
    {
        private readonly ILoggerFactory _defaultFactory;
        private readonly string _sessionId;
        private readonly ConversationStateLogsRepository _repository;
        private readonly ConcurrentDictionary<string, SessionLogger> _loggers = new();
        private bool _isDatabaseLoggingActive = false;
        private SemaphoreSlim _sessionSemaphore = new SemaphoreSlim(1, 1);

        public SessionLoggerFactory(ILoggerFactory defaultFactory, string sessionId, ConversationStateLogsRepository repository)
        {
            _defaultFactory = defaultFactory;
            _sessionId = sessionId;
            _repository = repository;
        }

        // This method activates all loggers this factory has created
        public void ActivateDatabaseLogging()
        {
            _isDatabaseLoggingActive = true;
            foreach (var logger in _loggers.Values)
            {
                logger.ActivateDatabaseLogging();
            }
        }

        public void AddProvider(ILoggerProvider provider)
        {
            // We delegate this to the default factory to not break any existing setup
            _defaultFactory.AddProvider(provider);
        }

        public ILogger CreateLogger(string categoryName)
        {
            return _loggers.GetOrAdd(categoryName, name =>
            {
                var defaultLogger = _defaultFactory.CreateLogger(name);
                return new SessionLogger(defaultLogger, _sessionId, categoryName, _repository, _sessionSemaphore, _isDatabaseLoggingActive);
            });
        }

        public void Dispose()
        {
            _defaultFactory.Dispose();
            _loggers.Clear();
        }
    }
}
