using System;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/people")]
    [ApiController]
    public class PeopleController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PeopleController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet]
        public async Task<IActionResult> GetPeople([FromQuery] int pageNumber = 1, [FromQuery] string? search = "")
        {
            Console.WriteLine("Getting people");
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            int pageSize = 10;
            int offset = (pageNumber - 1) * pageSize;
            string searchLike = "%" + search + "%";
            Console.WriteLine("SearchLike = "+searchLike);

            string query = @"SELECT *
                             FROM user
                             INNER JOIN profile ON user.user_id = profile.profile_id
                             WHERE (profile.display_name LIKE @searchLike OR user.username LIKE @searchLike) AND user.user_id != @userId
                             LIMIT @pageSize OFFSET @offset";

            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@searchLike", searchLike);
            command.Parameters.AddWithValue("@userId", userId);
            command.Parameters.AddWithValue("@pageSize", pageSize);
            command.Parameters.AddWithValue("@offset", offset);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                Console.WriteLine("Retrieved people");
                while (await reader.ReadAsync())
                {
                    Console.WriteLine("Reading people");
                    result.Add(new
                    {
                        user_id = reader.GetInt32(0),
                        username = reader.GetString("username"),
                        display_name = reader.GetString("display_name"),
                        profile_picture = reader.IsDBNull("profile_picture") ? "" : reader.GetString("profile_picture"),
                        profile_id = reader.GetInt32("profile_id"),
                        bio = reader.IsDBNull("bio") ? "" : reader.GetString("bio")
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
}