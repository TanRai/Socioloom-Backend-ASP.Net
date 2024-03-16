using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/likes")]
    [ApiController]
    public class LikesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public LikesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("post/personal/{postId}")]
        public IActionResult LikeUnlikePersonalPost(int postId, [FromBody] LikeRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            bool like = request.Like;

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand();
            command.Connection = connection;

            if (like)
            {
                command.CommandText = "INSERT IGNORE INTO likes (post_id, user_id) VALUES (@postId, @userId)";
            }
            else
            {
                command.CommandText = "DELETE FROM likes WHERE post_id = @postId AND user_id = @userId";
            }

            command.Parameters.AddWithValue("@postId", postId);
            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                command.ExecuteNonQuery();
                string message = like ? "Liked Personal Post" : "Unliked Personal Post";
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost("reply/personal/{replyId}")]
        public IActionResult LikeUnlikePersonalReply(int replyId, [FromBody] LikeRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            bool like = request.Like;

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand();
            command.Connection = connection;

            if (like)
            {
                command.CommandText = "INSERT IGNORE INTO replies_likes (reply_id, user_id) VALUES (@replyId, @userId)";
            }
            else
            {
                command.CommandText = "DELETE FROM replies_likes WHERE reply_id = @replyId AND user_id = @userId";
            }

            command.Parameters.AddWithValue("@replyId", replyId);
            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                command.ExecuteNonQuery();
                string message = like ? "Liked Personal Reply" : "Unliked Personal Reply";
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost("post/interests/{postId}")]
        public IActionResult LikeUnlikeInterestPost(int postId, [FromBody] LikeRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            bool like = request.Like;

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand();
            command.Connection = connection;

            if (like)
            {
                command.CommandText = "INSERT IGNORE INTO interest_likes (post_id, user_id) VALUES (@postId, @userId)";
            }
            else
            {
                command.CommandText = "DELETE FROM interest_likes WHERE post_id = @postId AND user_id = @userId";
            }

            command.Parameters.AddWithValue("@postId", postId);
            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                command.ExecuteNonQuery();
                string message = like ? "Liked Interest Post" : "Unliked Interest Post";
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpPost("reply/interests/{replyId}")]
        public IActionResult LikeUnlikeInterestReply(int replyId, [FromBody] LikeRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            bool like = request.Like;

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand();
            command.Connection = connection;

            if (like)
            {
                command.CommandText = "INSERT IGNORE INTO interest_replies_likes (reply_id, user_id) VALUES (@replyId, @userId)";
            }
            else
            {
                command.CommandText = "DELETE FROM interest_replies_likes WHERE reply_id = @replyId AND user_id = @userId";
            }

            command.Parameters.AddWithValue("@replyId", replyId);
            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                command.ExecuteNonQuery();
                string message = like ? "Liked Interest Reply" : "Unliked Interest Reply";
                return Ok(new { message });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
            finally
            {
                connection.Close();
            }
        }
    }

    public class LikeRequest
    {
        public bool Like { get; set; }
    }
}
