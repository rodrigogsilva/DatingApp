using System.Collections.Generic;
using System.Threading.Tasks;
using API.DTOs;
using API.Entities;
using API.Extensions;
using API.Helpers;
using API.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace API.Controllers
{
    [Authorize]
    public class LikesController : BaseApiController
    {
        private readonly IUnitOfWork _unitOfWork;
        public LikesController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpPost("{username}")]
        public async Task<ActionResult> addLike(string username)
        {
            var sourceUserId = User.GetUserId();
            var likedUser = await _unitOfWork.UserRepository.GetUserByUsernameAsync(username);
            var sourceUser = await _unitOfWork.LikesRepository.GetUserWithLikesAsync(sourceUserId);

            if (likedUser == null)
            {
                return NotFound();
            }

            if (sourceUser.UserName == username)
            {
                return BadRequest("You cannot like yourself");
            }

            var userLike = await _unitOfWork.LikesRepository.GetUserLikeAsync(sourceUserId, likedUser.Id);

            if (userLike != null)
            {
                return BadRequest("You already like this user");
            }

            userLike = new UserLike
            {
                SourceUserId = sourceUserId,
                LikedUserId = likedUser.Id
            };

            sourceUser.LikedUsers.Add(userLike);

            if (!await _unitOfWork.Complete())
            {
                return BadRequest("Failed to like user");
            }

            return Ok();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<LikeDTO>>> GetUserLikes([FromQuery] LikesParams likesParams)
        {
            likesParams.UserId = User.GetUserId();

            var users = await _unitOfWork.LikesRepository.GetUserLikesAsync(likesParams);

            Response.AddPaginationHeader(users.CurrentPage, users.PageSize, users.TotalCount, users.CurrentPage);

            return Ok(users);
        }
    }
}
