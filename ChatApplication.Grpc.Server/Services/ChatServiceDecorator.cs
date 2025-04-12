using ChatApplication.Common.ChatProto; // Ensure this is the correct namespace
using Grpc.Core;

// Fully qualify the conflicting type to avoid ambiguity
using Chat = ChatApplication.Common.ChatProto.Chat;

#pragma warning disable CS0436 // Type conflicts with imported type

namespace ChatApplication.Grpc.Server.Services
{
    public class ChatServiceDecorator(ChatStateService userState) : Chat.ChatBase
    {
        public override async Task ChatStream(
        IAsyncStreamReader<ChatModel> requestStream,
        IServerStreamWriter<ChatModel> responseStream,
        ServerCallContext context)
        {
            userState.AddClient(responseStream);

            foreach (var msg in userState.GetAllMessages())
            {
                await responseStream.WriteAsync(msg);
            }

            try
            {
                await foreach (var incomingMsg in requestStream.ReadAllAsync())
                {
                    userState.AddMessage(incomingMsg);
                    await userState.BroadcastMessageAsync(incomingMsg);
                }
            }
            catch (RpcException ex) when (ex.StatusCode == StatusCode.Cancelled)
            {
                throw new Exception("Something wrong happened");
            }
        }
    }
}