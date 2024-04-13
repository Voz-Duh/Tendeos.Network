using Microsoft.Extensions.Logging;

namespace Tendeos.Network
{
    internal class ConsoleLogger<T> : ILogger<T>
    {
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            switch (logLevel)
            {
                case LogLevel.Trace:
                    Network.Log.War(formatter(state, exception));
                    break;
                case LogLevel.Debug:
                    Network.Log.Mes(formatter(state, exception));
                    break;
                case LogLevel.Information:
                    Network.Log.Mes(formatter(state, exception));
                    break;
                case LogLevel.Warning:
                    Network.Log.War(formatter(state, exception));
                    break;
                case LogLevel.Error:
                    Network.Log.Err(formatter(state, exception));
                    break;
                case LogLevel.Critical:
                    Network.Log.Err(formatter(state, exception));
                    break;
                case LogLevel.None:
                    Network.Log.Mes(formatter(state, exception));
                    break;
            }
        }

        public bool IsEnabled(LogLevel logLevel) => false;

        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}