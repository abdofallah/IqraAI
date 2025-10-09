using IqraCore.Entities.Conversation.Logs;
using IqraCore.Entities.Conversation.Logs.Enums;
using IqraInfrastructure.Repositories.Conversation;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace IqraInfrastructure.Managers.Conversation.Session.Logger
{
    public class SessionLogger : ILogger
    {
        private readonly ILogger _defaultLogger;
        private readonly string _sessionId;
        private readonly string _categoryName;
        private readonly ConversationStateLogsRepository _repository;
        private readonly SemaphoreSlim _sessionSemaphore;

        private bool _isDatabaseLoggingActive = false;

        public SessionLogger(
            ILogger defaultLogger,
            string sessionId,
            string categoryName,
            ConversationStateLogsRepository repository,
            SemaphoreSlim sessionSemaphore,
            bool isDatabaseLoggingActive = false)
        {
            _defaultLogger = defaultLogger;
            _sessionId = sessionId;
            _categoryName = categoryName;
            _repository = repository;
            _sessionSemaphore = sessionSemaphore;
            _isDatabaseLoggingActive = isDatabaseLoggingActive;
        }

        public void ActivateDatabaseLogging()
        {
            _isDatabaseLoggingActive = true;
        }

        public IDisposable BeginScope<TState>(TState state) => _defaultLogger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    await _sessionSemaphore.WaitAsync();

                    if (!_isDatabaseLoggingActive)
                    {
                        return;
                    }

                    var message = formatter(state, exception);
                    var logEntry = new ConversationStateLogEntry
                    {
                        Id = eventId.ToString(),
                        SenderType = ConversationStateLogSenderTypeEnum.System,
                        SystemSenderReference = _categoryName,
                        Timestamp = DateTime.UtcNow,
                        Level = MapLogLevel(logLevel),
                        Message = message,
                        ExceptionDataJson = SerializeException(exception)
                    };

                    await _repository.AddLogEntryAsync(_sessionId, logEntry);
                }
                catch (Exception ex)
                {
                    // Log failure to write log to console to avoid infinite loops
                    _defaultLogger.LogError(ex, "Failed to write session log to database for SessionId: {SessionId}", _sessionId);
                }
                finally
                {
                    _sessionSemaphore.Release();
                }
            });
        }

        private string? SerializeException(Exception? exception)
        {
            if (exception == null)
            {
                return null;
            }

            var exceptionData = new
            {
                Type = exception.GetType().FullName,
                Message = exception.Message,
                StackTrace = exception.StackTrace,
                Source = exception.Source,
                // Optionally serialize inner exception recursively
                InnerException = exception.InnerException != null ? new
                {
                    Type = exception.InnerException.GetType().FullName,
                    Message = exception.InnerException.Message,
                    StackTrace = exception.InnerException.StackTrace
                } : null
            };

            try
            {
                return JsonSerializer.Serialize(exceptionData, new JsonSerializerOptions
                {
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                // Fallback in case serialization fails
                return $"{{ \"ErrorDuringSerialization\": \"{ex.Message}\", \"OriginalException\": \"{exception.Message}\", \"Source\": \"{exception.Source}\", \"StackTrace\": \"{exception.StackTrace}\" }}";
            }
        }

        private static ConversationStateLogLevelEnum MapLogLevel(LogLevel logLevel)
        {
            return logLevel switch
            {
                LogLevel.Trace => ConversationStateLogLevelEnum.Trace,
                LogLevel.Debug => ConversationStateLogLevelEnum.Debug,
                LogLevel.Information => ConversationStateLogLevelEnum.Information,
                LogLevel.Warning => ConversationStateLogLevelEnum.Warning,
                LogLevel.Error => ConversationStateLogLevelEnum.Error,
                LogLevel.Critical => ConversationStateLogLevelEnum.Critical,
                _ => ConversationStateLogLevelEnum.Information,
            };
        }
    }
}
