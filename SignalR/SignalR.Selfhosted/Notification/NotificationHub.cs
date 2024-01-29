using Microsoft.AspNetCore.SignalR;
using System;
using System.Threading.Tasks;

namespace SignalR.Selfhosted.Notification;
public class NotificationHub : Hub
{
    public Task JoinGroup(string groupName)
    {
        return Groups.AddToGroupAsync(Context.ConnectionId, groupName);
    }

    /// <summary>
    /// Automatically called when connected.
    /// </summary>
    /// <returns>A completed Task.</returns>
    public override Task OnConnectedAsync()
    {
        ConnectionHandler.ConnectedIds.Add(Context.ConnectionId);
        return Task.WhenAll(
            Clients.All.SendAsync("ConnectedClientUpdated", ConnectionHandler.ConnectedIds.Count),
            base.OnConnectedAsync());
    }

    /// <summary>
    /// Automatically called when disconnected.
    /// </summary>
    /// <param name="exception">Exception generated from connection.</param>
    /// <returns>A completed Task.</returns>
    public override Task OnDisconnectedAsync(Exception exception)
    {
        ConnectionHandler.ConnectedIds.Remove(Context.ConnectionId);

        return Task.WhenAll(
            Clients.All.SendAsync("ConnectedClientUpdated", ConnectionHandler.ConnectedIds.Count),
            Clients.All.SendAsync("ExceptionOccured", exception?.Message),
            base.OnDisconnectedAsync(exception));
    }
}
