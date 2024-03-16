using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/follow")]
    [ApiController]

    public class FollowController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public FollowController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("{id}")]
        public IActionResult FollowUnfollowUser(int id, [FromBody] FollowRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            bool follow = request.Follow;

            using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand();
            command.Connection = connection;

            if (follow)
            {
                command.CommandText = "INSERT IGNORE INTO follow (follower_id, following_id) VALUES (@followerId, @followingId)";
            }
            else
            {
                command.CommandText = "DELETE FROM follow WHERE follower_id = @followerId AND following_id = @followingId";
            }

            command.Parameters.AddWithValue("@followerId", userId);
            command.Parameters.AddWithValue("@followingId", id);

            try
            {
                command.ExecuteNonQuery();
                string message = follow ? "Followed User" : "Unfollowed User";
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

        [HttpGet("{id}")]
        public IActionResult CheckIfFollowingUser(int id)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand(
                "SELECT * FROM follow WHERE follower_id = @followerId AND following_id = @followingId",
                connection);

            command.Parameters.AddWithValue("@followerId", userId);
            command.Parameters.AddWithValue("@followingId", id);

            try
            {
                using MySqlDataReader reader = command.ExecuteReader();
                if (reader.HasRows)
                {
                    return Ok(new { following = true });
                }
                else
                {
                    return Ok(new { following = false });
                }
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

    public class FollowRequest
    {
        public bool Follow { get; set; }
    }
}