using System;
using System.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/interests")]
    [ApiController]
    public class InterestController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public InterestController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpPost("{id}")]
        public IActionResult SubscribeUnsubscribe(int id, [FromBody] SubscribeRequest request)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            bool subscribe = request.Subscribe;

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand();
            command.Connection = connection;

            if (subscribe)
            {
                command.CommandText = "INSERT IGNORE INTO subscribed_interest (user_id, interest_id) VALUES (@userId, @interestId)";
            }
            else
            {
                command.CommandText = "DELETE FROM subscribed_interest WHERE user_id = @userId AND interest_id = @interestId";
            }

            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@interestId", id);

            try
            {
                command.ExecuteNonQuery();
                string message = subscribe ? "Subscribed Interest" : "Unsubscribed Interest";
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

        [HttpGet]
        public async Task<IActionResult> GetAllInterests()
        {
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand("SELECT * FROM interest", connection);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        interest_id = reader.GetInt32(0),
                        title = reader.GetString(1)
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

        [HttpGet("user")]
        public async Task<IActionResult> GetUserInterests()
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand(
                @"SELECT i.interest_id, i.title,
                          CASE WHEN si.user_id IS NOT NULL THEN TRUE ELSE FALSE END AS subscribed
                  FROM interest i
                  LEFT JOIN subscribed_interest si ON i.interest_id = si.interest_id AND si.user_id = @userId",
                connection);

            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        interest_id = reader.GetInt32(0),
                        title = reader.GetString(1),
                        subscribed = reader.GetBoolean(2)
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
    }

    public class SubscribeRequest
    {
        public bool Subscribe { get; set; }
    }
}