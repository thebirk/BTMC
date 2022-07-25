using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using GbxRemoteNet;
using GbxRemoteNet.Structs;

namespace BTMC.Core
{
    public enum EventType
    {
        /// <summary>
        /// Dispatched when a player joins the server
        /// </summary>
        Join,
        
        /// <summary>
        /// Dispatches when a player disconnects from the server
        /// </summary>
        Disconnect,
        
        /// <summary>
        /// Dispatched when a player send a chat message
        /// </summary>
        Chat,
        
        /// <summary>
        /// Dispatched when a player finished a run
        /// </summary>
        Finish,

        /// <summary>
        /// Dispatched when a player crosses a checkpoint
        /// </summary>
        Checkpoint,
        
        /// <summary>
        /// Dispatched when a player crosses a waypoint (finish, CP, multi-lap)
        /// </summary>
        Waypoint,
        
        /// <summary>
        /// Dispatched when the plugin is loaded
        /// </summary>
        Load,
        
        /// <summary>
        /// Dispatched when the plugin is unloaded
        /// </summary>
        Unload,
        
        /// <summary>
        /// Dispatched when a players info updates
        /// </summary>
        PlayerInfo,
        
        /// <summary>
        /// Dispatched when a manialink page answer is received
        /// </summary>
        ManialinkAnswer,
        
        /// <summary>
        /// Custom event dispatched by plugins themselves. Useful for inter-plugin communication
        /// </summary>
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

    public class PlayerDisconnectEvent : Event
    {
        public string Login { get; set; }
        public string Reason { get; set; }

        public PlayerDisconnectEvent(GbxRemoteClient client, string login, string reason) : base(EventType.Disconnect, client)
        {
            Login = login;
            Reason = reason;
        }
    }

    public class LoadEvent : Event
    {
        public LoadEvent(GbxRemoteClient client) : base(EventType.Load, client)
        {
        }
    }

    public class CheckpointEvent : Event
    {
        public string Login { get; set; }
        public string AccountId { get; set; }
        /// <summary>
        /// Speed in m/s
        /// </summary>
        public float Speed { get; set; }
        public int RaceTime { get; set; }
        public int LapTime { get; set; }
        public int CheckpointInRace { get; set; }
        public int CheckpointInLap { get; set; }
        public string BlockId { get; set; }
        /// <summary>
        /// Server time when the checkpoint was crossed
        /// </summary>
        public int ServerTime { get; set; }
        public bool IsEndLap { get; set; }
        
        
        public CheckpointEvent(GbxRemoteClient client) : base(EventType.Checkpoint, client)
        {
        }
    }

    public class FinishEvent : Event
    {
        public string Login { get; set; }
        public string AccountId { get; set; }
        /// <summary>
        /// Speed in m/s
        /// </summary>
        public float Speed { get; set; }
        public int RaceTime { get; set; }
        public int LapTime { get; set; }
        public int CheckpointInRace { get; set; }
        public int CheckpointInLap { get; set; }
        public string BlockId { get; set; }
        /// <summary>
        /// Server time when the checkpoint was crossed
        /// </summary>
        public int ServerTime { get; set; }
        public bool IsEndLap { get; set; }
        
        public FinishEvent(GbxRemoteClient client) : base(EventType.Finish, client)
        {
        }
    }

    public class WaypointEvent : Event
    {
        public string Login { get; set; }
        public string AccountId { get; set; }
        /// <summary>
        /// Speed in m/s
        /// </summary>
        public float Speed { get; set; }
        public int RaceTime { get; set; }
        public int LapTime { get; set; }
        public int CheckpointInRace { get; set; }
        public int CheckpointInLap { get; set; }
        public string BlockId { get; set; }
        /// <summary>
        /// Server time when the checkpoint was crossed
        /// </summary>
        public int ServerTime { get; set; }
        public bool IsEndLap { get; set; }
        public bool IsEndRace { get; set; }
        
        public WaypointEvent(GbxRemoteClient client) : base(EventType.Waypoint, client)
        {
        }
    }

    public class PlayerInfoEvent : Event
    {
        public string Login { get; set; }
        public string Nickname { get; set; }
        public int PlayerId { get; set; }
        //TODO: parse SpectatorStatus
        public int SpectatorStatus { get; set; }
        //TODO: parse
        public int Flags { get; set; }
        
        public PlayerInfoEvent(GbxRemoteClient client, SPlayerInfo info) : base(EventType.PlayerInfo, client)
        {
            Login = info.Login;
            Nickname = info.NickName;
            PlayerId = info.PlayerId;
            SpectatorStatus = info.SpectatorStatus;
            Flags = info.Flags;
        }
    }

    public class ManialinkAnswerEvent : Event
    {
        public int PlayerUid { get; set; }
        public string Login { get; set; }
        public string Answer { get; set; }
        public SEntryVal[] Entries { get; set; }
        
        public ManialinkAnswerEvent(int playerUid, string login, string answer, SEntryVal[] entries, GbxRemoteClient client) : base(EventType.ManialinkAnswer, client)
        {
            PlayerUid = playerUid;
            Login = login;
            Answer = answer;
            Entries = entries;
        }
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
        private readonly EventDispatcher<PlayerDisconnectEvent> _playerDisconnectDispatcher = new();
        private readonly EventDispatcher<PlayerChatEvent> _playerChatDispatcher = new();
        private readonly EventDispatcher<CustomEvent> _customDispatcher = new();
        private readonly EventDispatcher<LoadEvent> _loadDispatcher = new();
        private readonly EventDispatcher<CheckpointEvent> _checkpointDispatcher = new();
        private readonly EventDispatcher<FinishEvent> _finishDispatcher = new();
        private readonly EventDispatcher<WaypointEvent> _waypointDispatcher = new();
        private readonly EventDispatcher<PlayerInfoEvent> _playerInfoDispatcher = new();
        private readonly EventDispatcher<ManialinkAnswerEvent> _manialinkAnswerDispatcher = new();

        public Task DispatchAsync(Event e)
        {
            switch (e.Type)
            {
                case EventType.Join:
                    return _playerJoinDispatcher.DispatchAsync((PlayerJoinEvent)e);
                case EventType.Disconnect:
                    return _playerDisconnectDispatcher.DispatchAsync((PlayerDisconnectEvent)e);
                case EventType.Chat:
                    return _playerChatDispatcher.DispatchAsync((PlayerChatEvent)e);
                case EventType.Custom:
                    return _customDispatcher.DispatchAsync((CustomEvent)e);
                case EventType.Load:
                    return _loadDispatcher.DispatchAsync((LoadEvent)e);
                case EventType.Checkpoint:
                    return _checkpointDispatcher.DispatchAsync((CheckpointEvent)e);
                case EventType.Finish:
                    return _finishDispatcher.DispatchAsync((FinishEvent)e);
                case EventType.Waypoint:
                    return _waypointDispatcher.DispatchAsync((WaypointEvent) e);
                case EventType.PlayerInfo:
                    return _playerInfoDispatcher.DispatchAsync((PlayerInfoEvent) e);
                case EventType.ManialinkAnswer:
                    return _manialinkAnswerDispatcher.DispatchAsync((ManialinkAnswerEvent) e);
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
                case EventType.Disconnect:
                    _playerDisconnectDispatcher.RegisterEventHandler((EventHandler<PlayerDisconnectEvent>) handler);
                    break;
                case EventType.Custom:
                    _customDispatcher.RegisterEventHandler((EventHandler<CustomEvent>) handler);
                    break;
                case EventType.Chat:
                    _playerChatDispatcher.RegisterEventHandler((EventHandler<PlayerChatEvent>) handler);
                    break;
                case EventType.Load:
                    _loadDispatcher.RegisterEventHandler((EventHandler<LoadEvent>) handler);
                    break;
                case EventType.Checkpoint:
                    _checkpointDispatcher.RegisterEventHandler((EventHandler<CheckpointEvent>) handler);
                    break;
                case EventType.Finish:
                    _finishDispatcher.RegisterEventHandler((EventHandler<FinishEvent>) handler);
                    break;
                case EventType.Waypoint:
                    _waypointDispatcher.RegisterEventHandler((EventHandler<WaypointEvent>) handler);
                    break;
                case EventType.PlayerInfo:
                    _playerInfoDispatcher.RegisterEventHandler((EventHandler<PlayerInfoEvent>) handler);
                    break;
                case EventType.ManialinkAnswer:
                    _manialinkAnswerDispatcher.RegisterEventHandler((EventHandler<ManialinkAnswerEvent>) handler);
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