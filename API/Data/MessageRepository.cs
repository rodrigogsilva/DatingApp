using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Helpers;
using API.Interfaces;
using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;

namespace API.Data
{
    public class MessageRepository : IMessageRepository
    {
        private readonly DataContext _context;
        private readonly IMapper _mapper;

        public MessageRepository(DataContext context, IMapper mapper)
        {
            _mapper = mapper;
            _context = context;
        }

        public void AddGroup(Group group)
        {
            _context.Groups.Add(group);
        }

        public void AddMessage(Message message)
        {
            _context.Add(message);
        }

        public void DeleteMessage(Message message)
        {
            _context.Messages.Remove(message);
        }

        public async Task<Connection> GetConnection(string connectionId)
        {
            return await _context.Connections.FindAsync(connectionId);
        }

        public async Task<Group> GetGroupForConnection(string connectionId)
        {
            return await _context.Groups
            .Include(c => c.Connections)
            .Where(c => c.Connections.Any(x => x.ConnectionId == connectionId))
            .FirstOrDefaultAsync();
        }

        public async Task<Message> GetMessageAsync(int id)
        {
            return await _context.Messages
                .Include(user => user.Sender)
                .Include(user => user.Recipient)
                .SingleOrDefaultAsync(message => message.Id == id);
        }

        public async Task<Group> GetMessageGroup(string groupName)
        {
            return await _context.Groups
                .Include(x => x.Connections)
                .FirstOrDefaultAsync(x => x.Name == groupName);
        }

        public async Task<PagedList<MessageDto>> GetMessagesForUserAsync(MessageParams messageParams)
        {
            var messages = _context.Messages
                .OrderByDescending(message => message.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .AsQueryable();

            messages = messageParams.Container switch
            {
                "Inbox" => messages.Where(user => user.RecipientUsername == messageParams.Username && user.RecipientDeleted == false),
                "Outbox" => messages.Where(user => user.SenderUsername == messageParams.Username && user.SenderDeleted == false),
                _ => messages.Where(user =>
                    user.RecipientUsername == messageParams.Username && user.RecipientDeleted == false && user.DateRead == null),
            };

            return await PagedList<MessageDto>.CreateAsync(messages, messageParams.PageNumber, messageParams.PageSize);
        }

        public async Task<IEnumerable<MessageDto>> GetMessageThreadAsync(string currentUsername, string recipientUsername)
        {
            var messages = await _context.Messages
                .Include(user => user.Sender).ThenInclude(photo => photo.Photos)
                .Include(user => user.Recipient).ThenInclude(photo => photo.Photos)
                .Where(message => message.Recipient.UserName == currentUsername
                    && message.RecipientDeleted == false
                    && message.Sender.UserName == recipientUsername
                    || message.Recipient.UserName == recipientUsername
                    && message.Sender.UserName == currentUsername
                    && message.SenderDeleted == false
                )
                .OrderBy(message => message.MessageSent)
                .ProjectTo<MessageDto>(_mapper.ConfigurationProvider)
                .ToListAsync();

            var unreadMessages = messages.Where(message => message.DateRead == null
                && message.RecipientPhotoUrl == currentUsername).ToList();

            if (unreadMessages.Any())
            {
                foreach (var message in unreadMessages)
                {
                    message.DateRead = DateTime.UtcNow;
                }
            }

            return messages;
        }

        public void removeConnection(Connection connection)
        {
            _context.Connections.Remove(connection);
        }
    }
}
