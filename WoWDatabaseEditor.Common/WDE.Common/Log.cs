using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace WDE.Common;

public static class LOG
{
    public static ILogger Logger { get; private set; } = NullLogger.Instance;
    public static ILoggerFactory Factory { get; private set; } = NullLoggerFactory.Instance;

    public static EventId NonCriticalInvalidStateEventId { get; } = new(1, "Non Critical Invalid State");

    public static void Initialize(ILoggerFactory factory)
    {
        Factory = factory;
        Logger = factory.CreateLogger("Global");
    }

    public static void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        Logger.Log(logLevel, eventId, state, exception, formatter);
    }

    public static void LogDebug(EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.LogDebug(eventId, exception, message, args);
    }

    public static void LogDebug(EventId eventId, string? message, params object?[] args)
    {
        Logger.LogDebug(eventId, message, args);
    }

    public static void LogDebug(Exception? exception, string? message, params object?[] args)
    {
        Logger.LogDebug(exception, message, args);
    }

    public static void LogDebug(string? message, params object?[] args)
    {
        Logger.LogDebug(message, args);
    }

    public static void LogTrace(EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.LogTrace(eventId, exception, message, args);
    }

    public static void LogTrace(EventId eventId, string? message, params object?[] args)
    {
        Logger.LogTrace(eventId, message, args);
    }

    public static void LogTrace(Exception? exception, string? message, params object?[] args)
    {
        Logger.LogTrace(exception, message, args);
    }

    public static void LogTrace(string? message, params object?[] args)
    {
        Logger.LogTrace(message, args);
    }

    public static void LogInformation(EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.LogInformation(eventId, exception, message, args);
    }

    public static void LogInformation(EventId eventId, string? message, params object?[] args)
    {
        Logger.LogInformation(eventId, message, args);
    }

    public static void LogInformation(Exception? exception, string? message, params object?[] args)
    {
        Logger.LogInformation(exception, message, args);
    }

    public static void LogInformation(string? message, params object?[] args)
    {
        Logger.LogInformation(message, args);
    }
    
    public static void LogWarning(EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.LogWarning(eventId, exception, message, args);
    }

    public static void LogWarning(EventId eventId, string? message, params object?[] args)
    {
        Logger.LogWarning(eventId, message, args);
    }

    public static void LogWarning(Exception? exception,
        [CallerMemberName] string? caller = null,
        [CallerFilePath] string? callerFile = null,
        [CallerLineNumber] int? callerLineNumber = null)
    {
        Logger.LogWarning(exception, "Warning in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
    }

    public static void LogWarning(Exception? exception, string? message, params object?[] args)
    {
        Logger.LogWarning(exception, message, args);
    }

    public static void LogWarning(string? message, params object?[] args)
    {
        Logger.LogWarning(message, args);
    }

    public static void LogError(EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.LogError(eventId, exception, message, args);
    }

    public static void LogError(EventId eventId, string? message, params object?[] args)
    {
        Logger.LogError(eventId, message, args);
    }

    public static void LogError(Exception? exception,
        [CallerMemberName] string? caller = null,
        [CallerFilePath] string? callerFile = null,
        [CallerLineNumber] int? callerLineNumber = null)
    {
        Logger.LogError(exception, "Error in {0} at {1}:{2}", caller, callerFile, callerLineNumber);
    }

    public static void LogError(Exception? exception, string? message, params object?[] args)
    {
        Logger.LogError(exception, message, args);
    }

    public static void LogError(string? message, params object?[] args)
    {
        Logger.LogError(message, args);
    }

    public static void LogCritical(EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.LogCritical(eventId, exception, message, args);
    }

    public static void LogCritical(EventId eventId, string? message, params object?[] args)
    {
        Logger.LogCritical(eventId, message, args);
    }

    public static void LogCritical(Exception? exception, string? message, params object?[] args)
    {
        Logger.LogCritical(exception, message, args);
    }

    public static void LogCritical(string? message, params object?[] args)
    {
        Logger.LogCritical(message, args);
    }

    public static void Log(LogLevel logLevel, string? message, params object?[] args)
    {
        Logger.Log(logLevel, message, args);
    }

    public static void Log(LogLevel logLevel, EventId eventId, string? message, params object?[] args)
    {
        Logger.Log(logLevel, eventId, message, args);
    }

    public static void Log(LogLevel logLevel, Exception? exception, string? message, params object?[] args)
    {
        Logger.Log(logLevel, exception, message, args);
    }

    public static void Log(LogLevel logLevel, EventId eventId, Exception? exception, string? message, params object?[] args)
    {
        Logger.Log(logLevel, eventId, exception, message, args);
    }
}