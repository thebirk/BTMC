using GbxRemoteNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BTMC.Core.Commands
{
    public abstract class CommandBase
    {
        public GbxRemoteClient Client { get; private set; }
        public int PlayerUid { get; private set; }
        public string PlayerLogin { get; private set; }
        public string[] Args { get; private set; }

        public void Init(GbxRemoteClient client, int playerUid, string login, string[] args)
        {
            Client = client;
            PlayerUid = playerUid;
            PlayerLogin = login;
            Args = args;
        }

        public abstract Task ExecuteAsync();

        public Task SendMessageAsync(string message)
        {
            return Client.ChatSendServerMessageToIdAsync(message, PlayerUid);
        }

        public static string[] ParseArgs(string text)
        {
            text = text.Trim();

            if (!text.StartsWith('/'))
            {
                return null;
            }
            text = text[1..];

            List<string> args = new List<string>();
            StringBuilder sb = new StringBuilder();

            bool inQuotes = false;

            foreach (char ch in text)
            {
                if (ch == '\"')
                {
                    if (inQuotes)
                    {
                        inQuotes = false;
                        args.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        inQuotes = true;
                        if (sb.Length > 0)
                        {
                            args.Add(sb.ToString());
                            sb.Clear();
                        }
                    }

                    continue;
                }

                if (!inQuotes && ch == ' ' && sb.Length > 0)
                {
                    args.Add(sb.ToString());
                    sb.Clear();
                    continue;
                }

                if (!inQuotes && ch == ' ')
                {
                    continue;
                }

                sb.Append(ch);
            }

            if (sb.Length > 0)
            {
                args.Add(sb.ToString());
            }

            return args.ToArray();
        }
    }
}
