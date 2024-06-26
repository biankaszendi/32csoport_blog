﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using BlogBeadando.Data;
using BlogBeadando.Models;
using BlogBeadando.Services;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.SignalR;
using static BlogBeadando.Controllers.CommentsController;

namespace Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TopicsController : ControllerBase
    {
        private readonly DataContext _context;
        private readonly TopicService _topicService;
        private readonly ILogger<TopicsController> _logger;
        private readonly IHubContext<CommentHub> _commentHubContext;

        public TopicsController(DataContext context, TopicService topicService, ILogger<TopicsController> logger, IHubContext<CommentHub> commentHubContext)
        {
            _context = context;
            _topicService = topicService;
            _logger = logger;
            _commentHubContext = commentHubContext;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Topic>>> GetTopics()
        {
            return await _context.Topics.ToListAsync();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Topic>> GetTopic(int id)
        {
            var topic = await _context.Topics.FindAsync(id);

            if (topic == null)
            {
                return NotFound();
            }

            return topic;
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> PutTopic(int id, TopicInputModel model)
        {
            if (id != model.TopicId)
            {
                return BadRequest();
            }

            var topicEntity = _topicService.Update(model);

            if (topicEntity == null)
            {
                return NotFound();
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Topic>> PostTopic(Topic topic)
        {
            _context.Topics.Add(topic);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTopic", new { id = topic.TopicId }, topic);
        }

        [HttpDelete("{id}")]
        [Authorize(Roles = "Administrator")]
        public async Task<IActionResult> DeleteTopic(int id)
        {
            var topic = await _context.Topics.FindAsync(id);
            if (topic == null)
            {
                return NotFound();
            }

            _context.Topics.Remove(topic);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        private bool TopicExists(int id)
        {
            return _context.Topics.Any(e => e.TopicId == id);
        }

        [HttpPost("AddComment")]
        public async Task<IActionResult> AddComment(Comment comment)
        {
            try
            {
                var commentModel = new EventBindingModel.CommentModel
                {
                    Id = comment.CommentId,
                    UserId = comment.UserId,
                    TopicId = comment.TopicId,
                    Body = comment.Body,
                    Timestamp = comment.Timestamp
                };

                _topicService.AddComment(commentModel);

                await _commentHubContext.Clients.All
                    .SendAsync("NewCommentNotification", new { CommentId = comment.CommentId });

                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while adding comment");
                return StatusCode(500);
            }
        }
    }
}
