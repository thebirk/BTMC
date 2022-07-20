using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GbxRemoteNet;

namespace BTMC.Core
{
    public enum EventType
    {
        Join,
        Disconnect,
        Chat,
        Finish,
        Load,
        Unload,
        Custom,
    }
    
    [JetBrains.Annotations.MeansImplicitUse]
    [AttributeUsage(AttributeTargets.Method)]
    public class EventHandlerAttribute : Attribute
    {
        public EventType Type { get; set; }

        public EventHandlerAttribute(EventType type)
        {
            Type = type;
        }
    }
    
    public class Event
    {
        public EventType Type { get; set; }
        /// <summary>
        /// Set to true whenever an event handler returns true.
        /// Used as a hint for event handlers to determine what kind of action they should they  take.
        /// </summary>
        public bool Handled { get; set; }
        public GbxRemoteClient Client { get; set; }

        protected Event(EventType type, GbxRemoteClient client)
        {
            Type = type;
            Client = client;
        }
    }

    public class PlayerJoinEvent : Event
    {
        public string Login { get; set; }
        public bool IsSpectator { get; set; }
        
        public PlayerJoinEvent(GbxRemoteClient client, string login, bool isSpectator) : base(EventType.Join, client)
        {
            Login = login;
            IsSpectator = isSpectator;
        }
    }

    public class PlayerChatEvent : Event
    {
        public string Login { get; set; }
        public int PlayerUid { get; set; }
        public string Message { get; set; }

        public PlayerChatEvent(GbxRemoteClient client, string login, int playerUid, string message) : base(EventType.Chat, client)
        {
            Login = login;
            PlayerUid = playerUid;
            Message = message;
        }
    }

    public class PlayerFinishArgs
    {
        public string PlayerUid { get; set; }
        public string TimeOrScore { get; set; }
    }

    public class CustomEvent : Event
    {
        public Guid Id { get; set; }

        public CustomEvent(GbxRemoteClient client, Guid id) : base(EventType.Custom, client)
        {
            Id = id;
        }
    }

    public delegate Task<bool> EventHandler<in TEvent>(TEvent e) where TEvent : Event;

    public class EventSystem
    {
        // How this ended up like this I'm not quite sure of. It came about after a lot experimentation
        // about how to keep as much type information as possible.
        private readonly EventDispatcher<PlayerJoinEvent> _playerJoinDispatcher = new();
        private readonly EventDispatcher<PlayerChatEvent> _playerChatDispatcher = new();
        private readonly EventDispatcher<CustomEvent> _customDispatcher = new();

        public Task DispatchAsync(Event e)
        {
            switch (e.Type)
            {
                case EventType.Join:
                    return _playerJoinDispatcher.DispatchAsync((PlayerJoinEvent)e);
                case EventType.Chat:
                    return _playerChatDispatcher.DispatchAsync((PlayerChatEvent)e);
                case EventType.Custom:
                    return _customDispatcher.DispatchAsync((CustomEvent)e);
            }

            return Task.CompletedTask;
        }

        public void RegisterEventHandler(EventType type, Delegate handler)
        {
            switch (type)
            {
                case EventType.Join:
                    _playerJoinDispatcher.RegisterEventHandler((EventHandler<PlayerJoinEvent>) handler);
                    break;
                case EventType.Custom:
                    _customDispatcher.RegisterEventHandler((EventHandler<CustomEvent>) handler);
                    break;
                case EventType.Chat:
                    _playerChatDispatcher.RegisterEventHandler((EventHandler<PlayerChatEvent>) handler);
                    break;
            }
        }
    }
    
    public class EventDispatcher<TEvent> where TEvent : Event
    {
        private readonly List<EventHandler<TEvent>> _handlers = new List<EventHandler<TEvent>>();

        public void RegisterEventHandler(EventHandler<TEvent> handler)
        {
            _handlers.Add(handler);
        }

        public async Task DispatchAsync(TEvent e)
        {
            foreach (var handler in _handlers)
            {
                if (await handler.Invoke(e))
                {
                    e.Handled = true;
                }
            }
        }
    }
}