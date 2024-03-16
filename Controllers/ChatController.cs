using System;
using Microsoft.AspNetCore.Authorization;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/chat")]
    [ApiController]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public ChatController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("getChatId/{otherPersonId}")]
        public async Task<IActionResult> GetChatId(int otherPersonId)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            if (userId == otherPersonId)
            {
                return Ok(new { chatId = -1 });
            }
            else
            {
                Console.WriteLine("Getting chat id");
                string query = @"SELECT * FROM chat WHERE (user_1 = @userId AND user_2 = @otherPersonId) OR (user_1 = @otherPersonId AND user_2 = @userId)";
                using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                connection.Open();

                using MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@otherPersonId", otherPersonId);

                try
                {
                    Console.WriteLine("Getting chat id 2");
                    using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                    Console.WriteLine("Getting chat id 3");
                    if (await reader.ReadAsync())
                    {
                        Console.WriteLine("Getting chat id 4");
                        return Ok(new { chatId = reader.GetInt32("chat_id") });
                    }
                    else
                    {
                        reader.Close();
                        string insertQuery = @"INSERT IGNORE INTO chat (user_1, user_2) VALUES (@userId, @otherPersonId)";
                        using MySqlCommand insertCommand = new MySqlCommand(insertQuery, connection);
                        insertCommand.Parameters.AddWithValue("@userId", userId);
                        insertCommand.Parameters.AddWithValue("@otherPersonId", otherPersonId);

                        insertCommand.ExecuteNonQuery();

                        return Ok(new { chatId = insertCommand.LastInsertedId });
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

        [HttpGet("chatList")]
        public async Task<IActionResult> GetChatList()
        {
            Console.WriteLine("Getting chat list");
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            string query = @"SELECT 
                                chat.chat_id,
                                profile1.profile_id as user_1,
                                profile1.display_name as disp1,
                                profile1.profile_picture as pic1,
                                profile2.profile_id as user_2,
                                profile2.display_name as disp2,
                                profile2.profile_picture as pic2,
                                user1.username as username1,
                                user2.username as username2
                            FROM chat 
                            INNER JOIN profile AS profile1 ON chat.user_1 = profile1.profile_id
                            INNER JOIN profile AS profile2 ON chat.user_2 = profile2.profile_id
                            INNER JOIN user AS user1 ON user1.user_id = chat.user_1
                            INNER JOIN user AS user2 ON user2.user_id = chat.user_2
                            WHERE user_1 = @userId OR user_2 = @userId";

            using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@userId", userId);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        chat_id = reader.GetInt32("chat_id"),
                        user_1 = reader.GetInt32("user_1"),
                        disp1 = reader.GetString("disp1"),
                        pic1 = reader.IsDBNull("pic1") ? "" : reader.GetString("pic1"),
                        user_2 = reader.GetInt32("user_2"),
                        disp2 = reader.GetString("disp2"),
                        pic2 = reader.IsDBNull("pic2") ? "" : reader.GetString("pic2"),
                        username1 = reader.GetString("username1"),
                        username2 = reader.GetString("username2")
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

        [HttpGet("{chatId}")]
        public async Task<IActionResult> GetChatMessages(int chatId)
        {
            string query = @"SELECT * FROM message WHERE chat_id = @chatId";

            using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@chatId", chatId);

            try
            {
                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        message_id = reader.GetInt32("message_id"),
                        chat_id = reader.GetInt32("chat_id"),
                        message_text = reader.GetString("message_text"),
                        sender = reader.GetInt32("sender"),
                        //sent = reader.GetDateTime("sent")
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

        [HttpGet("getChatInfo/{chatId}")]
        public IActionResult GetChatInfo(int chatId)
        {
            string query = @"SELECT 
                                user1.profile_id as user_1,
                                user1.display_name as disp1,
                                user2.profile_id as user_2,
                                user2.display_name as disp2
                            FROM chat 
                            INNER JOIN profile AS user1 ON chat.user_1 = user1.profile_id
                            INNER JOIN profile AS user2 ON chat.user_2 = user2.profile_id
                            WHERE chat_id = @chatId";

            using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            connection.Open();

            using MySqlCommand command = new MySqlCommand(query, connection);
            command.Parameters.AddWithValue("@chatId", chatId);

            try
            {
                using MySqlDataReader reader = command.ExecuteReader();
                if (reader.Read())
                {
                    return Ok(new
                    {
                        user_1 = reader.GetInt32("user_1"),
                        disp1 = reader.GetString("disp1"),
                        user_2 = reader.GetInt32("user_2"),
                        disp2 = reader.GetString("disp2")
                    });
                }
                else
                {
                    return NotFound(new { message = "Chat not found" });
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
}
