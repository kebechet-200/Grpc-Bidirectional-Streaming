using ChatApplication.Common.ChatProto;
using Grpc.Core;
using System.Collections.Concurrent;

namespace ChatApplication.Grpc.Server.Services
{
    public class ChatStateService
    {
        private readonly ConcurrentBag<IServerStreamWriter<ChatModel>> _clients = new();
        private readonly ConcurrentQueue<ChatModel> _messages = new();

        public IEnumerable<ChatModel> GetAllMessages() => _messages.ToArray();

        public void AddMessage(ChatModel message)
        {
            _messages.Enqueue(message);
        }

        public void AddClient(IServerStreamWriter<ChatModel> client)
        {
            _clients.Add(client);
        }

        public async Task BroadcastMessageAsync(ChatModel message)
        {
            foreach (var client in _clients)
            {
                try
                {
                    await client.WriteAsync(message);
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"Error broadcasting to a client: {ex.Message}");
                }
            }
        }
    }
}
