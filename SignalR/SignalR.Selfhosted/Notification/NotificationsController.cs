using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using SignalR.Common;
using SignalR.Models;
using System.Threading.Tasks;

namespace SignalR.Selfhosted.Notification;
[ApiController]
[Route("[controller]")]
public class NotificationsController : ControllerBase
{
    private readonly IHubContext<NotificationHub> _hubContext;

    public NotificationsController(IHubContext<NotificationHub> hubContext)
    {
        _hubContext = hubContext;
    }

    [HttpPost("groups/{groupName}")]
    public IActionResult CreateGroup([FromRoute] string groupName)
    {
        ConnectionHandler.Groups.Add(groupName);
        return Ok(ConnectionHandler.Groups);
    }

    [HttpGet("groups")]
    public IActionResult GetGroups()
    {
        return Ok(ConnectionHandler.Groups);
    }

    [HttpPost("send/all")]
    public async Task<IActionResult> SendAll()
    {
        await _hubContext.Clients.Groups(ConnectionHandler.Groups)
            .SendCoreAsync(Constants.NotificationCreatedEvent,
                new object[]{
                    new NotificationMessageModel
                    {
                        Id = 1,
                        Content = "Some Content",
                        Title = "Some Title"
                    }
                });

        return Ok("Sent to all connected clients.");
    }

    [HttpPost("send/{groupName}")]
    public async Task<IActionResult> SendToGroup([FromRoute] string groupName)
    {
        await _hubContext.Clients
            .Groups(groupName)
            .SendCoreAsync(Constants.NotificationCreatedEvent,
                new object[]{
                    new NotificationMessageModel
                    {
                        Id = 1,
                        Content = "Some Content",
                        Title = "Some Title"
                    }
                });

        return Ok($"Sent to all connected clients of {groupName} group.");
    }
}