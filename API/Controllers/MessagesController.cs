using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class MessagesController : BaseApiController
    {
        private readonly IMessageRepository _messageRepository;
        private readonly IUserRepository _userRepository;
        private readonly IMapper _mapper;
        public MessagesController(IUserRepository userRepository, IMessageRepository messageRepository, IMapper mapper)
        {
            _mapper = mapper;
            _userRepository = userRepository;
            _messageRepository = messageRepository;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessagesForUser(
            [FromQuery] MessageParams messageParams
        )
        {
            messageParams.Username = User.GetUsername();

            var message = await _messageRepository.GetMessagesForUserAsync(messageParams);

            Response.AddPaginationHeader(message.CurrentPage, message.PageSize,
                message.TotalPages, message.TotalPages);

            return message;
        }

        [HttpGet("thread/{username}")]
        public async Task<ActionResult<IEnumerable<MessageDto>>> GetMessageThread(string username)
        {
            var currentUsername = User.GetUsername();

            return Ok(await _messageRepository.GetMessageThreadAsync(currentUsername, username));
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> DeleteMessage(int id)
        {
            var username = User.GetUsername();

            var message = await _messageRepository.GetMessageAsync(id);

            if (message.Sender.UserName != username && message.Recipient.UserName != username)
            {
                return Unauthorized();
            }

            if (message.Sender.UserName == username)
            {
                message.SenderDeleted = true;
            }

            if (message.Recipient.UserName == username)
            {
                message.RecipientDeleted = true;
            }

            if (message.SenderDeleted && message.RecipientDeleted)
            {
                _messageRepository.DeleteMessage(message);
            }

            if (!await _messageRepository.SaveAllAsync())
            {
                return BadRequest("Problem deleting the message");
            }

            return Ok();
        }

    }
}
