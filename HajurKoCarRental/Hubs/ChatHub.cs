using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Data;
using HajurKoCarRental.Models;

namespace HajurKoCarRental.Hubs
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly HajurKoCarRentalContext _context;

        public ChatHub(HajurKoCarRentalContext context)
        {
            _context = context;
        }

        public async Task SendMessage(string receiverId, string message)
        {
            var senderId = Context.UserIdentifier;

            var chatMessage = new ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = senderId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessage!.Add(chatMessage);
            await _context.SaveChangesAsync();

            await Clients.User(receiverId)!.SendAsync("ReceiveMessage", senderId, message, chatMessage.SentAt.ToString("o"));
            await Clients.Caller.SendAsync("MessageSent", chatMessage.Id);
        }

        public async Task MarkAsRead(string senderId)
        {
            var receiverId = Context.UserIdentifier;

            var unread = await _context.ChatMessage!
                .Where(m => m.SenderId == senderId && m.ReceiverId == receiverId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread)
                msg.IsRead = true;

            await _context.SaveChangesAsync();

            await Clients.User(senderId).SendAsync("MessagesRead", receiverId);
        }

        public async Task JoinGroup(string groupName)
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
        }

        public async Task LeaveGroup(string groupName)
        {
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
        }

        public override async Task OnConnectedAsync()
        {
            var userId = Context.UserIdentifier;
            await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
            await base.OnConnectedAsync();
        }
    }
}
