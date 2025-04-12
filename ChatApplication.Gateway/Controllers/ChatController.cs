using System.Collections.Concurrent;
using ChatApplication.Common.ChatProto;
using ChatApplication.Gateway.Dtos;
using Grpc.Core;
using Microsoft.AspNetCore.Mvc;

#pragma warning disable CS0436 // Type conflicts with imported type

namespace ChatApplication.Gateway.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Produces("application/json")]
    [Consumes("application/json")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public class ChatController(Chat.ChatClient client) : ControllerBase
    {
        private static readonly ConcurrentBag<string> _responses = new ConcurrentBag<string>();
        [HttpPost("send-message")]
        public async Task<ActionResult<ConcurrentBag<string>>> SendMessage([FromBody] ChatDto dto)
        {
            using var chat = client.ChatStream();

            var responseTask = Task.Run(async () =>
            {
                await foreach (var message in chat.ResponseStream.ReadAllAsync())
                {
                    _responses.Add($"{message.User}: {message.Message}");
                }
            });

            await chat.RequestStream.WriteAsync(new ChatModel
            {
                User = dto.UserId,
                Message = dto.Message
            });

            await chat.RequestStream.CompleteAsync();
            await responseTask;

            return Ok(_responses);
        }
    }
}
