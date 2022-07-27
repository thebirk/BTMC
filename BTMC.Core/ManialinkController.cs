using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GbxRemoteNet;

namespace BTMC.Core
{
    [Plugin("ManialinkController", "0.0.1", Author = "thebirk")]
    public class ManialinkController
    {
        private class Listener
        {
            public TaskCompletionSource<int> CompletionSource { get; init; }
            public int[] Actions { get; init; }
            public DateTime Timeout { get; init; }
        }

        private readonly GbxRemoteClient _client;
        private readonly List<Listener> _listeners = new();
        private readonly object _listenersLock = new();
        private int _actionCounter = 0;
        private readonly CancellationTokenSource _cleanupCancellationTokenSource = new();

        public ManialinkController(GbxRemoteService gbxRemoteService)
        {
            _client = gbxRemoteService.Client;

            // Cleanup out of date listeners
            Task.Run(() =>
            {
                while (true)
                {
                    if (_cleanupCancellationTokenSource.IsCancellationRequested)
                    {
                        return;
                    }

                    lock (_listenersLock)
                    {
                        for (var i = 0; i < _listeners.Count; i++)
                        {
                            var listener = _listeners[i];
                            
                            if (DateTime.Now >= listener.Timeout)
                            {
                                _listeners.RemoveAt(i);
                                listener.CompletionSource.SetCanceled();
                            }
                        }
                    }

                    Thread.Sleep(1000);
                }
            }, _cleanupCancellationTokenSource.Token);
        }

        public async Task<int> SendManialinkAsync(string login, string manialink, int[] actions, int timeout = 10000)
        {
            var completion = new TaskCompletionSource<int>();

            lock (_listenersLock)
            {
                _listeners.Add(new Listener
                {
                    CompletionSource = completion,
                    Actions = actions,
                    // 1s buffer for sending the manialink 
                    Timeout = DateTime.Now + TimeSpan.FromMilliseconds(timeout + 1000),
                });
            }

            await _client.SendDisplayManialinkPageToLoginAsync(login, manialink, timeout, true);
            return await completion.Task;
        }

        public int GenUniqueAction()
        {
            return _actionCounter++;
        }
        
        [EventHandler(EventType.ManialinkAnswer)]
        public Task<bool> OnManialinkAnswer(ManialinkAnswerEvent e)
        {
            if (!int.TryParse(e.Answer, out var answer))
            {
                return Task.FromResult(false);
            }

            lock (_listenersLock)
            {
                for (var i = 0; i < _listeners.Count; i++)
                {
                    var listener = _listeners[i];
                    if (DateTime.Now >= listener.Timeout)
                    {
                        _listeners.RemoveAt(i);
                        listener.CompletionSource.SetCanceled();
                        return Task.FromResult(false);
                    }

                    if (!listener.Actions.Contains(answer)) continue;

                    listener.CompletionSource.SetResult(answer);
                    _listeners.RemoveAt(i);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }

        [EventHandler(EventType.Unload)]
        public Task<bool> OnUnload(Event e)
        {
            _cleanupCancellationTokenSource.Cancel();
            
            return Task.FromResult(false);
        }
    }
}