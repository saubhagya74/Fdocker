using app.Data;
using app.DTO;
using app.Migrations;
using app.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace app.Controllers
{

    [Route("[controller]")]
    [ApiController]
    [Authorize]
    public class ChatController:ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly SnowFlakeGen _idgen;
        public ChatController(ApplicationDbContext context, SnowFlakeGen idgen)
        {
            _context = context;
            _idgen = idgen;
        }
        [HttpGet("loadusers")]
        public async Task<ActionResult<object>> LoadUser()
        {
            string claimuserid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long.TryParse(claimuserid, out long currentUserId);
            var users = await _context.Messages
                .Where(m => m.SenderId == currentUserId || m.ReceiverId == currentUserId) // messages involving current user
                .OrderByDescending(m => m.TimeStamp) // sort latest message first
                .Select(m => m.SenderId == currentUserId ? m.ReceiverId : m.SenderId) // pick the other user
                .Distinct() // only unique users
                .Take(20) // optional: limit results
                .ToListAsync();
            var chatUsers = await _context.Users
                .Where(u => users.Contains(u.UserId))
                .Select(u => new
                {
                    UserId =u.UserId.ToString(),
                    UserName = u.UserName
                })
                .ToListAsync();

            return Ok(chatUsers);
        }
        //[HttpGet("loadmessage/{receiverId}")]
        //public async Task<ActionResult<List<MessageDto>>> GetMessages(string receiverId, [FromQuery] string? date)
        //{
        //    // 1. Parse current user ID from claims
        //    var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        //    if (claimId == null || !long.TryParse(claimId, out long currentUserId))
        //        return Unauthorized();

        //    // 2. Parse receiver ID from route
        //    if (!long.TryParse(receiverId, out long receiverIdLong))
        //        return BadRequest("Invalid receiver ID");

        //    // 3. Parse and normalize the 'date' parameter as UTC
        //    if (!DateTime.TryParse(date, null,
        //        System.Globalization.DateTimeStyles.AssumeUniversal |
        //        System.Globalization.DateTimeStyles.AdjustToUniversal,
        //        out DateTime reqDate))
        //    {
        //        return BadRequest("Invalid time");
        //    }

        //    // 4. Query messages older than reqDate (UTC)
        //    var messages = await _context.Messages
        //        .Where(m =>
        //            ((m.SenderId == currentUserId && m.ReceiverId == receiverIdLong) ||
        //             (m.SenderId == receiverIdLong && m.ReceiverId == currentUserId)) &&
        //             m.TimeStamp < reqDate)
        //        .OrderByDescending(m => m.TimeStamp)
        //        .Take(15)
        //        .Select(m => new MessageDto
        //        {
        //            SenderId = m.SenderId.ToString(),
        //            ReceiverId = m.ReceiverId.ToString(),
        //            Content = m.Content,
        //            TimeStamp = m.TimeStamp // keep as UTC, no conversion here
        //        })
        //        .ToListAsync();

        //    // 5. Reverse so oldest first
        //    messages.Reverse();

        //    return Ok(messages);
        //}
        [HttpGet("loadmessage/{receiverId}")]
        public async Task<ActionResult<List<MessageDto>>> GetAllMessages(string receiverId)
        {
            // 1) Parse the JWT user
            var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (claimId == null)
                return Unauthorized();

            // 2) Parse the incoming receiverId
            if (!long.TryParse(receiverId, out var rid))
                return BadRequest("Invalid receiverId");

            if (!long.TryParse(claimId, out var currentUserId))
                return BadRequest("Invalid user identifier");
            // 3) Project directly to anonymous objects with string IDs
            var messagedto = await _context.Messages
                .Where(m =>
                    (m.SenderId == currentUserId && m.ReceiverId == rid) ||
                    (m.SenderId == rid && m.ReceiverId == currentUserId))
                .OrderBy(m => m.TimeStamp)
                .Select(m => new MessageDto
                {
                    SenderId = m.SenderId.ToString(),
                    ReceiverId = m.ReceiverId.ToString(),
                    Content = m.Content,
                    TimeStamp = m.TimeStamp
                })
                .ToListAsync();

            return Ok(messagedto);
        }

        [HttpGet("searchUser/{searchedName}")]
        [Authorize]
        public async Task<ActionResult<object>> SearchUser(string searchedName)
        {
            var currentUserName = User.FindFirst(ClaimTypes.Name)?.Value;
            if (searchedName == currentUserName)
                return BadRequest(new { message = "It's you" });

            var searcheduser = await _context.Users.FirstOrDefaultAsync(u => u.UserName == searchedName);
            if (searcheduser == null)
                return BadRequest(new { message = "User Not Found" });

            var suser = new
            {
                searchedUserName = searcheduser.UserName,
                searchedUserId = searcheduser.UserId.ToString(),
                searchedNoOfFriends = searcheduser.NumOfFriends
            };

            return Ok(suser);
        }
        [HttpGet("seeprofile")]
        [Authorize]
        public async Task<ActionResult<object>> SeeProfile()
        {
            string claimuserid = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            long.TryParse(claimuserid, out long userid);
            var user = await _context.Users.FirstOrDefaultAsync(u => u.UserId == userid);
            if (user == null) return BadRequest(new {message ="unable to see profile"});
            var xuser = new
            {
                userName = user.UserName,
                userId = user.UserId.ToString(),
                numOfFriends = user.NumOfFriends
            };
            return Ok(xuser);
        }

        [HttpGet("sendrequest/{receiverid}")]
        [Authorize]
        public async Task<ActionResult<object>> SendRequest(string receiverId)
        {
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out long currentuserid);
            var currentuser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentuserid);
            if (currentuser == null) return BadRequest(new { message = "Invalid Request" });

            long.TryParse(receiverId, out long receiverid);
            var receiver = await _context.Users.FirstOrDefaultAsync(u => u.UserId == receiverid);
            if (receiver == null) return BadRequest(new {message= "Receiver Doesnot Exist" });

            var checkreqexist = await _context.FriendRequests.FirstOrDefaultAsync(u =>
              (u.RequesterId == currentuser.UserId && u.RequestToId == receiver.UserId) ||
              (u.RequesterId == receiver.UserId && u.RequestToId == currentuser.UserId)
              );
            if (checkreqexist != null && checkreqexist.RequestStatus == "pending")
                return Ok("Already sent");

            var xreq = new FriendRequestEntity
            {
                RequestId = _idgen.GenerateId(),
                RequesterId = currentuser.UserId,
                RequestToId = receiver.UserId,
                RequestStatus = "pending",
                RequestTime = DateTime.UtcNow
            };
            _context.FriendRequests.Add(xreq);
            await _context.SaveChangesAsync();

            return Ok(new { xreq.RequesterId, xreq.RequestToId, xreq.RequestStatus, xreq.RequestTime });
        }

        [HttpGet("acceptordeclinerequest/{friendid}/{statuschange}")]
        [Authorize]
        public async Task<ActionResult<bool>> AcceptOrDeclineRequest(string friendId, bool statuschange)
        {
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out long currentUserId);
            if (!long.TryParse(friendId, out long friendUserId))
                return BadRequest(new { message="Invalid friend ID" });

            var currentUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentUserId);
            var friendUser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == friendUserId);

            if (currentUser == null || friendUser == null)
                return BadRequest(new { message="User(s) not found" });

            var friendRequest = await _context.FriendRequests.FirstOrDefaultAsync(u =>
                (u.RequesterId == currentUserId && u.RequestToId == friendUserId) ||
                (u.RequesterId == friendUserId && u.RequestToId == currentUserId));

            if (friendRequest == null)
                return BadRequest(new { message= "Friend request not found" });

            if (!statuschange)
            {
                friendRequest.RequestStatus = "declined";
                await _context.SaveChangesAsync();
                return Ok(true);
            }

            // Accept logic
            friendRequest.RequestStatus = "accepted";

            currentUser.NumOfFriends++;
            friendUser.NumOfFriends++;

            _context.Friends.Add(new FriendEntity
            {
                FriendId = _idgen.GenerateId(),
                FUserId1 = friendRequest.RequesterId,
                FUserId2 = friendRequest.RequestToId,
                Status = "friend",
                FriendSince = DateTime.UtcNow
            });

            await _context.SaveChangesAsync();
            return Ok(true);
        }



        //[HttpGet("seenotification")]
        //[Authorize]
        //public async Task<ActionResult<List<NotificationDto>>> SeeNotification()
        //{
        //    long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out long currentuserid);
        //    var currentuser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentuserid);
        //    if (currentuser == null) return BadRequest("invalud userid:unauthorized");
        //    var notification = await _context.FriendRequests.Where(u => u.RequestToId == currentuser.UserId || u.RequesterId == currentuser.UserId).ToListAsync();
        //    if (notification == null) return BadRequest(new { message="no notification" });

        //    var notificationdto = notification.Select(n => new NotificationDto//while working with loop
        //    {

        //        RequesterId = n.RequesterId.ToString(),
        //        RequestToId = n.RequestToId.ToString(),
        //        RequestTime = n.RequestTime,
        //        RequestStatus = n.RequestStatus
        //    }).ToList();
        //    return Ok(notificationdto);
        //}
        [HttpGet("seenotification")]
        [Authorize]
        public async Task<ActionResult<List<NotificationDto>>> SeeNotification()
        {
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out long currentuserid);

            var currentuser = await _context.Users.FirstOrDefaultAsync(u => u.UserId == currentuserid);
            if (currentuser == null)
                return BadRequest("Invalid userid: Unauthorized");

            // Get friend requests where user is either requester or receiver
            var notifications = await _context.FriendRequests
                .Where(u => u.RequestToId == currentuserid || u.RequesterId == currentuserid)
                .ToListAsync();

            if (notifications == null || !notifications.Any())
                return BadRequest(new { message = "No notifications" });

            // Get all unique user IDs involved in notifications
            var userIds = notifications
                .SelectMany(n => new[] { n.RequesterId, n.RequestToId })//both are selcted and put in to one list
                .Distinct()// removes duplicates
                .ToList();
           //after putting in one list
            // Query user names for those IDs
            var users = await _context.Users
                .Where(u => userIds.Contains(u.UserId))
                .ToDictionaryAsync(u => u.UserId, u => u.UserName);

            // Map to DTO
            var notificationDtos = notifications.Select(n => new NotificationDto
            {
                RequesterId = n.RequesterId.ToString(),
                RequesterName = users.ContainsKey(n.RequesterId) ? users[n.RequesterId] : "Unknown",
                //ContainsKey(n.RequesterId) ? checks if exist or not , if not , then unknown,
                //users[n.RequesterId] this means like users[0] and name is extracted
                RequestToId = n.RequestToId.ToString(),
                RequestToName = users.ContainsKey(n.RequestToId) ? users[n.RequestToId] : "Unknown",
                RequestTime = n.RequestTime,
                RequestStatus = n.RequestStatus
            }).ToList();

            return Ok(notificationDtos);
        }

    }

}
