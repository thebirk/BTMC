using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BTMC.Core.Commands;
using Microsoft.Extensions.Logging;

namespace BTMC.Core
{
    [Command("addadmin")]
    public class AdminCommand : CommandBase
    {
        private readonly AdminController _adminController;
        
        public AdminCommand(AdminController adminController)
        {
            _adminController = adminController;
        }
        
        public override async Task ExecuteAsync()
        {
            if (Args.Length != 1)
            {
                await Client.ChatSendServerMessageToLoginAsync("Usage: /addadmin <login>", PlayerLogin);
                return;
            }
            
            _adminController.AddAdmin(Args[0]);
            await Client.ChatSendServerMessageToLoginAsync($"Added {PlayerLogin} as an admin", PlayerLogin);
        }
    }
    
    public class AdminController
    {
        private readonly ILogger<AdminController> _logger;
        private readonly List<string> _admins = new();

        public AdminController(ILogger<AdminController> logger)
        {
            _logger = logger;
        }

        public bool IsAdmin(string login)
        {
            return _admins.Contains(login);
        }

        public void AddAdmin(string login)
        {
            if (!_admins.Contains(login))
            {
                _admins.Add(login);
                _logger.LogWarning("Added login {} as an admin", login);
            }
        }
    }
}