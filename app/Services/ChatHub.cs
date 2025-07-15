using app.Data;
using app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Connections.Features;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace app.Services
{
        [Authorize]
        public class ChatHub : Hub
        {
        private readonly ApplicationDbContext _context;
        private readonly SnowFlakeGen _idgen;

        public ChatHub(ApplicationDbContext context,SnowFlakeGen idgen)
        {
            _idgen = idgen;
            _context = context;
        }
        public override async Task OnConnectedAsync()
        {
            var userIdentifier = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long.TryParse(userIdentifier, out long UserIdentifier);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == UserIdentifier); // await added
            if (user != null)
            { 
                user.ConnectionId = Context.ConnectionId;//yo onconnectasync lea connectionid matra dine ho , yo whole function ok kam nai yei ho
                await _context.SaveChangesAsync();
            }
            await base.OnConnectedAsync();//yo default connecter ho
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out long userIdentifier);//converting the strig guid id in jwt to guid type and then compare
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdentifier); // await added
            if (user != null)
            {
                user.ConnectionId = string.Empty;
                await _context.SaveChangesAsync();
            }
            await base.OnDisconnectedAsync(exception);//yesma pani same chizz gareko ho connection id empty
        }//The OnDisconnectedAsync(Exception? exception) method is triggered automatically by SignalR when a client disconnects (either normally or unexpectedly)

        public async Task SendMessage(string receivername, string messageContent)//,string receivername="")
        {
            
            long.TryParse(Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value, out long userIdentifier);

            var sender = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userIdentifier); //  await added

            if (sender == null) return; //  fixed null check
            //long.TryParse(receiverid, out long ReceiverId);
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserName == receivername);

            if (receiver == null) return;
            var message = new MessageEntity
            {
                //You ignore Context.ConnectionId use idgen
                MessageId = _idgen.GenerateId(),
                SenderId = sender.UserId,
                ReceiverId = receiver.UserId,
                Content = messageContent,
                TimeStamp = DateTime.UtcNow
            };

            _context.Messages.Add(message);
            await _context.SaveChangesAsync();

            if (!string.IsNullOrEmpty(receiver.ConnectionId))
            {
                await Clients.Client(receiver.ConnectionId)///yo sendmesage function ma kei xaina just message database ma store gareko ho 
                    //tara dubai side ma update garauna client send gareko
                    .SendAsync("ReceiveMessage", sender.UserName, messageContent);//calling receivemessage function in the angular
            }

            if (!string.IsNullOrEmpty(sender.ConnectionId)) //  added this null check
            {
                await Clients.Client(sender.ConnectionId)
                    .SendAsync("ReceiveMessage", sender.UserName, messageContent);
            }//Clients.Client(connectionId).SendAsync(...) is a SignalR server-to-client method used to send a real-time message to one specific connected client.
        }
           
       
    }
}