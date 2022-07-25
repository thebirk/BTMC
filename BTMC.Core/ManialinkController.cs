using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GbxRemoteNet;

namespace BTMC.Core
{
    [Plugin("ManialinkController", "0.0.1")]
    public class ManialinkController
    {
        private class Listener
        {
            public TaskCompletionSource<int> ComppleCompletionSource { get; init; }
            public int[] Actions { get; init; }
        }
        
        private readonly List<Listener> _listeners = new();
        private int _actionCounter = 0;
        
        //TODO: how do we remove listeners that are timed out, or the player disconnect before answering.
        //      Which is likely for actions part of the main server ui
        public Task<int> SendManialinkAsync(GbxRemoteClient client, string login, string manialink, int[] actions, int timeout = 10000, bool autohide = true)
        {
            client.SendDisplayManialinkPageToLoginAsync(login, manialink, timeout, autohide).GetAwaiter().GetResult();
            
            var completion = new TaskCompletionSource<int>();
            _listeners.Add(new Listener
            {
                ComppleCompletionSource = completion,
                Actions = actions,
            });
            return completion.Task;
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

            for (int i = 0; i < _listeners.Count; i++)
            {
                var listener = _listeners[i];
                if (listener.Actions.Contains(answer))
                {
                    listener.ComppleCompletionSource.SetResult(answer);
                    _listeners.RemoveAt(i);
                    return Task.FromResult(true);
                }
            }

            return Task.FromResult(false);
        }
    }
}