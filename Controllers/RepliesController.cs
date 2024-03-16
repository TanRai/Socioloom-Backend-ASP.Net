using System;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/replies")]
    [ApiController]
    public class RepliesController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public RepliesController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("personal/{postId}")]
        public async Task<IActionResult> GetPersonalReplies(int postId)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            string query = @"
                SELECT 
                    replies.reply_id,
                    replies.post_id,
                    replies.user_id,
                    replies.reply_text,
                    replies.time_replied,
                    user.username,
                    profile.display_name,
                    profile.profile_picture,
                    COUNT(DISTINCT replies_likes.like_id) AS like_count,
                    COUNT(CASE WHEN replies_likes.user_id = @userId THEN replies_likes.like_id END) > 0 AS user_liked
                FROM replies
                INNER JOIN user ON replies.user_id = user.user_id
                INNER JOIN profile ON replies.user_id = profile.profile_id
                LEFT JOIN replies_likes ON replies.reply_id = replies_likes.reply_id
                WHERE replies.post_id = @postId
                GROUP BY replies.post_id, replies.reply_id
                ORDER BY replies.reply_id DESC
                LIMIT 10 OFFSET 0";

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@postId", postId);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        reply_id = reader.GetInt32("reply_id"),
                        post_id = reader.GetInt32("post_id"),
                        user_id = reader.GetInt32("user_id"),
                        reply_text = reader.GetString("reply_text"),
                        time_replied = reader.GetDateTime("time_replied"),
                        username = reader.GetString("username"),
                        display_name = reader.GetString("display_name"),
                        profile_picture = reader.IsDBNull("profile_picture") ? "" : reader.GetString("profile_picture"),
                        like_count = reader.GetInt32("like_count"),
                        user_liked = reader.GetBoolean("user_liked")
                    });
                }
                return Ok(result);
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

        [HttpPost("personal/{postId}")]
        public IActionResult PostPersonalReply(int postId, [FromBody] ReplyRequest request)
        {
            Console.WriteLine("REEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEEE");
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            string query = @"INSERT INTO replies(post_id, user_id, reply_text) VALUES(@postId, @userId, @replyText)";

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@postId", postId);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@replyText", request.ReplyText);

            try
            {
                command.ExecuteNonQuery();
                return Ok(new { message = "Reply posted successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                return StatusCode(500, new { message = "Internal Server Error" });
            }
        }

        [HttpGet("interests/{postId}")]
        public async Task<IActionResult> GetInterestReplies(int postId)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            string query = @"
                SELECT 
                    interest_replies.reply_id,
                    interest_replies.post_id,
                    interest_replies.user_id,
                    interest_replies.reply_text,
                    interest_replies.time_replied,
                    user.username,
                    profile.display_name,
                    profile.profile_picture,
                    COUNT(DISTINCT interest_replies_likes.like_id) AS like_count,
                    COUNT(CASE WHEN interest_replies_likes.user_id = @userId THEN interest_replies_likes.like_id END) > 0 AS user_liked
                FROM interest_replies
                INNER JOIN user ON interest_replies.user_id = user.user_id
                INNER JOIN profile ON interest_replies.user_id = profile.profile_id
                LEFT JOIN interest_replies_likes ON interest_replies.reply_id = interest_replies_likes.reply_id
                WHERE interest_replies.post_id = @postId
                GROUP BY interest_replies.post_id, interest_replies.reply_id
                ORDER BY interest_replies.reply_id DESC
                LIMIT 10 OFFSET 0";

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@postId", postId);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        reply_id = reader.GetInt32("reply_id"),
                        post_id = reader.GetInt32("post_id"),
                        user_id = reader.GetInt32("user_id"),
                        reply_text = reader.GetString("reply_text"),
                        time_replied = reader.GetDateTime("time_replied"),
                        username = reader.GetString("username"),
                        display_name = reader.GetString("display_name"),
                        profile_picture = reader.IsDBNull("profile_picture") ? "" : reader.GetString("profile_picture"),
                        like_count = reader.GetInt32("like_count"),
                        user_liked = reader.GetBoolean("user_liked")
                    });
                }

                return Ok(result);
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

        [HttpPost("interests/{postId}")]
        public IActionResult PostInterestReply(int postId, [FromBody] ReplyRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            string query = @"INSERT INTO interest_replies(post_id, user_id, reply_text) VALUES(@postId, @userId, @replyText)";

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@postId", postId);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@replyText", request.ReplyText);

            try
            {
                command.ExecuteNonQuery();
                return Ok(new { message = "Reply posted successfully" });
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

    public class ReplyRequest
    {
        public string ReplyText { get; set; }
    }
}