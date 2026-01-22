namespace AuthQueryService.Infrastructure.Messaging.EventHandlers
{
    /// <summary>
    /// Factory for resolving event handlers by event type
    /// </summary>
    public interface IEventHandlerFactory
    {
        IUserEventHandler? GetHandler(string eventType);
        IEnumerable<string> GetSupportedEventTypes();
    }

    public sealed class EventHandlerFactory : IEventHandlerFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<string, Type> _handlerTypes;

        public EventHandlerFactory(IServiceProvider serviceProvider, IEnumerable<IUserEventHandler> handlers)
        {
            _serviceProvider = serviceProvider;
            _handlerTypes = handlers.ToDictionary(h => h.EventType, h => h.GetType());
        }

        public IUserEventHandler? GetHandler(string eventType)
        {
            if (_handlerTypes.TryGetValue(eventType, out var handlerType))
            {
                return (IUserEventHandler?)_serviceProvider.GetService(handlerType);
            }
            return null;
        }

        public IEnumerable<string> GetSupportedEventTypes() => _handlerTypes.Keys;
    }
}
