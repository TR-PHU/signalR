using ChatService.Models;
using Microsoft.AspNetCore.SignalR;

namespace ChatService.Hubs
{
    public class ChatHub : Hub
    {
        private readonly string _botUser;
        private readonly IDictionary<string, UserConnection> _connections;

        public ChatHub(IDictionary<string, UserConnection> connections)
        {
            _botUser = "MyChat Bot";
            _connections = connections;
        }

        public async Task JoinRoom(UserConnection userConnection) 
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, userConnection.Room);

            _connections[Context.ConnectionId] = userConnection;

            await Clients.Groups(userConnection.Room).SendAsync("ReceiveMessage", _botUser, $"{userConnection.User} has joined {userConnection.Room}");

            SendUserConnected(userConnection.Room);
        }

        public async Task SendMessage(string message)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                await Clients.Groups(userConnection.Room).SendAsync("ReceiveMessage", userConnection.User, message);
            }
        }

        public override  Task OnDisconnectedAsync(Exception? exception)
        {
            if (_connections.TryGetValue(Context.ConnectionId, out UserConnection userConnection))
            {
                _connections.Remove(Context.ConnectionId);
                 Clients.Group(userConnection.Room).SendAsync("ReceiveMessage", _botUser, $"{userConnection.User} has left");
                SendUserConnected(userConnection.Room);
            }
            return base.OnDisconnectedAsync(exception);
        }

        public Task SendUserConnected(string room)
        {
            var users = _connections.Values
                                    .Where(uc => uc.Room == room)
                                    .Select(uc => uc.User);

            return Clients.Group(room).SendAsync("UsersInRoom", users);
        }
    }
}
