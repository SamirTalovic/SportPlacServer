using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SportPlac.Data;
using SportPlac.Models;
using SportPlac.Services;

namespace SportPlac.Controllers
{
    [ApiController]
    [Route("api/conversations")]
    [Authorize]
    public class ConversationsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly IHubContext<ChatHub> _hub;
        private readonly CloudinaryService _cloudinary;

        public ConversationsController(
            AppDbContext context,
            IHubContext<ChatHub> hub,
            CloudinaryService cloudinary)
        {
            _context = context;
            _hub = hub;
            _cloudinary = cloudinary;
        }

        [HttpGet("count/unread")]
        public async Task<IActionResult> GetUnreadCount()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var unreadCount = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId)
                .SumAsync(cp => cp.UnreadCount);

            return Ok(unreadCount);
        }

        [HttpGet]
        public async Task<IActionResult> GetConversations()
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();

            var userId = Guid.Parse(userIdStr);

            var data = await _context.ConversationParticipants
                .Where(cp => cp.UserId == userId)
                .Select(cp => new
                {
                    cp.Conversation.Id,
                    cp.UnreadCount,
                    cp.Conversation.LastMessageAt,

                    LastMessage = cp.Conversation.Messages
                        .OrderByDescending(m => m.CreatedAt)
                        .Select(m => m.Text)
                        .FirstOrDefault(),

                    Listing = cp.Conversation.Listing != null ? new
                    {
                        cp.Conversation.Listing.Id,
                        cp.Conversation.Listing.Title
                    } : null
                })
                .OrderByDescending(x => x.LastMessageAt)
                .ToListAsync();

            return Ok(data);
        }
        [HttpGet("{id}/messages")]
        public async Task<IActionResult> GetMessages(Guid id, int page = 1, int pageSize = 20)
        {
            var messages = await _context.Messages
                .Where(m => m.ConversationId == id)
                .OrderByDescending(m => m.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(m => new
                {
                    m.Id,
                    m.Text,
                    m.AttachmentUrl,
                    m.IsRead,
                    m.CreatedAt,
                    m.SenderId
                })
                .ToListAsync();

            return Ok(messages);
        }
        [HttpPost]
        public async Task<IActionResult> CreateConversation(Guid listingId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var listing = await _context.Listings
                .Select(l => new { l.Id, l.SellerId })
                .FirstOrDefaultAsync(l => l.Id == listingId);

            if (listing == null) return NotFound();

            if (listing.SellerId == userId)
                return BadRequest("Cannot message yourself");

            var existing = await _context.Conversations
                .Where(c => c.ListingId == listingId)
                .Where(c => c.Participants.Any(p => p.UserId == userId))
                .FirstOrDefaultAsync();

            if (existing != null)
                return Ok(existing.Id);

            var convo = new Conversation
            {
                Id = Guid.NewGuid(),
                ListingId = listingId,
                Participants = new List<ConversationParticipant>
        {
            new ConversationParticipant
            {
                Id = Guid.NewGuid(),
                UserId = userId
            },
            new ConversationParticipant
            {
                Id = Guid.NewGuid(),
                UserId = listing.SellerId
            }
        }
            };

            _context.Conversations.Add(convo);
            await _context.SaveChangesAsync();

            return Ok(convo.Id);
        }
        [HttpPost("{id}/messages")]
        public async Task<IActionResult> SendMessage(Guid id, [FromForm] string text, IFormFile? file)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var isParticipant = await _context.ConversationParticipants
                .AnyAsync(p => p.ConversationId == id && p.UserId == userId);

            if (!isParticipant)
                return Forbid();

            string? fileUrl = null;

            if (file != null)
            {
                fileUrl = await _cloudinary.UploadImageAsync(file);
            }

            var message = new Message
            {
                Id = Guid.NewGuid(),
                ConversationId = id,
                SenderId = userId,
                Text = text,
                AttachmentUrl = fileUrl,
                IsRead = false
            };

            _context.Messages.Add(message);

            // unread count + mark others unread
            var others = await _context.ConversationParticipants
                .Where(p => p.ConversationId == id && p.UserId != userId)
                .ToListAsync();

            foreach (var p in others)
                p.UnreadCount++;

            var convo = await _context.Conversations.FindAsync(id);
            convo.LastMessageAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // 🔥 REALTIME
            await _hub.Clients.Group(id.ToString())
                .SendAsync("ReceiveMessage", new
                {
                    message.Id,
                    message.Text,
                    message.AttachmentUrl,
                    message.SenderId,
                    message.CreatedAt
                });

            return Ok();
        }
        [HttpPatch("{id}/read")]
        public async Task<IActionResult> MarkAsRead(Guid id)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value 
                          ?? User.FindFirst("sub")?.Value;
            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var participant = await _context.ConversationParticipants
                .FirstOrDefaultAsync(p => p.ConversationId == id && p.UserId == userId);

            if (participant == null) return NotFound();

            participant.UnreadCount = 0;

            var messages = await _context.Messages
                .Where(m => m.ConversationId == id && m.SenderId != userId)
                .ToListAsync();

            foreach (var msg in messages)
                msg.IsRead = true;

            await _context.SaveChangesAsync();

            return Ok();
        }
        [HttpDelete("messages/{messageId}")]
        public async Task<IActionResult> DeleteMessage(Guid messageId)
        {
            var userIdStr = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value
                          ?? User.FindFirst("sub")?.Value;

            if (string.IsNullOrEmpty(userIdStr)) return Unauthorized();
            var userId = Guid.Parse(userIdStr);

            var message = await _context.Messages
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null)
                return NotFound();

            // 🔒 SAMO SENDER SME DA BRIŠE
            if (message.SenderId != userId)
                return Forbid();

            var conversationId = message.ConversationId;

            _context.Messages.Remove(message);
            await _context.SaveChangesAsync();

            // 🔥 update last message
            var lastMessage = await _context.Messages
                .Where(m => m.ConversationId == conversationId)
                .OrderByDescending(m => m.CreatedAt)
                .FirstOrDefaultAsync();

            var convo = await _context.Conversations.FindAsync(conversationId);

            convo.LastMessageAt = lastMessage?.CreatedAt;

            await _context.SaveChangesAsync();

            await _hub.Clients.Group(conversationId.ToString())
                .SendAsync("MessageDeleted", new
                {
                    MessageId = messageId
                });

            return Ok();
        }


    }
}
