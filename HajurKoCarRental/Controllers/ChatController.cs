using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using HajurKoCarRental.Areas.Identity.Data;
using HajurKoCarRental.Data;

namespace HajurKoCarRental.Controllers
{
    public class LastMessageInfo
    {
        public string Message { get; set; }
        public DateTime SentAt { get; set; }
        public bool IsRead { get; set; }
        public bool SentByMe { get; set; }
    }

    [Authorize]
    public class ChatController : Controller
    {
        private readonly HajurKoCarRentalContext _context;
        private readonly UserManager<HajurKoCarRentalUser> _userManager;

        public ChatController(HajurKoCarRentalContext context, UserManager<HajurKoCarRentalUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // Admin: list all conversations
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Index()
        {
            var adminId = _userManager.GetUserId(User);

            var conversationUsers = await _context.ChatMessage!
                .Where(m => m.ReceiverId == adminId || m.SenderId == adminId)
                .Select(m => m.SenderId == adminId ? m.ReceiverId : m.SenderId)
                .Distinct()
                .ToListAsync();

            var users = await _context.Users
                .Where(u => conversationUsers.Contains(u.Id))
                .Select(u => new { u.Id, Name = u.Firstname + " " + u.Lastname, u.ProfilePhotoUrl })
                .ToListAsync();

            var lastMessages = new Dictionary<string, LastMessageInfo>();
            var lastMessageTimes = new Dictionary<string, DateTime>();

            foreach (var userId in conversationUsers)
            {
                var lastMsg = await _context.ChatMessage!
                    .Where(m => (m.SenderId == adminId && m.ReceiverId == userId) ||
                                (m.SenderId == userId && m.ReceiverId == adminId))
                    .OrderByDescending(m => m.SentAt)
                    .FirstOrDefaultAsync();

                if (lastMsg != null)
                {
                    lastMessages[userId] = new LastMessageInfo
                    {
                        Message = lastMsg.Message,
                        SentAt = lastMsg.SentAt,
                        IsRead = lastMsg.IsRead,
                        SentByMe = lastMsg.SenderId == adminId
                    };
                    lastMessageTimes[userId] = lastMsg.SentAt;
                }
                else
                {
                    lastMessageTimes[userId] = DateTime.MinValue;
                }
            }

            var unreadCounts = await _context.ChatMessage!
                .Where(m => m.ReceiverId == adminId && !m.IsRead)
                .GroupBy(m => m.SenderId)
                .Select(g => new { UserId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(g => g.UserId, g => g.Count);

            // Sort users by most recent message first
            users = users.OrderByDescending(u => lastMessageTimes.ContainsKey(u.Id) ? lastMessageTimes[u.Id] : DateTime.MinValue).ToList();

            ViewBag.Users = users;
            ViewBag.LastMessages = lastMessages;
            ViewBag.UnreadCounts = unreadCounts;

            return View();
        }

        // Admin: view conversation with a specific customer
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Conversation(string userId)
        {
            var adminId = _userManager.GetUserId(User);
            var customer = await _userManager.FindByIdAsync(userId);
            if (customer == null) return NotFound();

            var messages = await _context.ChatMessage!
                .Where(m => (m.SenderId == adminId && m.ReceiverId == userId) ||
                            (m.SenderId == userId && m.ReceiverId == adminId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.Message,
                    m.SentAt,
                    IsMine = m.SenderId == adminId,
                    m.IsRead
                })
                .ToListAsync();

            // Mark unread messages as read
            var unread = await _context.ChatMessage!
                .Where(m => m.SenderId == userId && m.ReceiverId == adminId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread) msg.IsRead = true;
            await _context.SaveChangesAsync();

            ViewBag.CustomerId = userId;
            ViewBag.CustomerName = $"{customer.Firstname} {customer.Lastname}".Trim();
            ViewBag.CustomerPhoto = customer.ProfilePhotoUrl;
            ViewBag.Messages = messages;
            ViewBag.CurrentUserId = adminId;

            return View();
        }

        // Customer: get chat history with admin (does NOT mark as read)
        [Authorize(Roles = "Customer")]
        public async Task<IActionResult> GetMessages()
        {
            var userId = _userManager.GetUserId(User);
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminId = admins.FirstOrDefault()?.Id;
            if (adminId == null) return Json(new { error = "No admin available" });

            var rawMessages = await _context.ChatMessage!
                .Where(m => (m.SenderId == userId && m.ReceiverId == adminId) ||
                            (m.SenderId == adminId && m.ReceiverId == userId))
                .OrderBy(m => m.SentAt)
                .Select(m => new
                {
                    m.Id,
                    m.Message,
                    m.SentAt,
                    IsMine = m.SenderId == userId,
                    m.IsRead
                })
                .ToListAsync();

            var messages = rawMessages.Select(m => new
            {
                m.Id,
                m.Message,
                sentAt = DateTime.SpecifyKind(m.SentAt, DateTimeKind.Utc).ToString("o"),
                m.IsMine,
                m.IsRead
            }).ToList();

            var unreadCount = await _context.ChatMessage!
                .Where(m => m.SenderId == adminId && m.ReceiverId == userId && !m.IsRead)
                .CountAsync();

            return Json(new { adminId, messages, unreadCount });
        }

        // Customer: mark conversation as read (called when panel opens)
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> MarkConversationRead()
        {
            var userId = _userManager.GetUserId(User);
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            var adminId = admins.FirstOrDefault()?.Id;
            if (adminId == null) return Json(new { success = false });

            var unread = await _context.ChatMessage!
                .Where(m => m.SenderId == adminId && m.ReceiverId == userId && !m.IsRead)
                .ToListAsync();

            foreach (var msg in unread) msg.IsRead = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Customer: send a message
        [Authorize(Roles = "Customer")]
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> SendMessage(string receiverId, string message)
        {
            var userId = _userManager.GetUserId(User);

            var chatMessage = new Models.ChatMessage
            {
                Id = Guid.NewGuid(),
                SenderId = userId,
                ReceiverId = receiverId,
                Message = message,
                SentAt = DateTime.UtcNow,
                IsRead = false
            };

            _context.ChatMessage!.Add(chatMessage);
            await _context.SaveChangesAsync();

            return Json(new { success = true, id = chatMessage.Id });
        }
    }
}
