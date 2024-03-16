using System;
using System.Collections.Generic;
using System.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using MySql.Data.MySqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Socioloom_Backend_ASP.Net.Controllers
{
    [Route("api/posts")]
    [ApiController]
    public class PostsController : ControllerBase
    {
        private readonly IConfiguration _configuration;

        public PostsController(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        [HttpGet("personal")]
        public async Task<IActionResult> GetPersonalPosts(
            [FromQuery] int? pageNumber = 1,
            [FromQuery] string? search = null,
            [FromQuery] bool? explore = false)
        {
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                // Extract user information from claims
                User user = (User)HttpContext.Items["User"];
                int userId = user.id;

                // Your MySQL connection and command setup
                
                await connection.OpenAsync();

                // Your SQL query
                string searchLike = !string.IsNullOrEmpty(search) ? $" WHERE post.post_text LIKE '%{search}%'" : "";
                string followLike = (bool)!explore ? $" WHERE post.user_id IN (SELECT DISTINCT following_id FROM follow WHERE follower_id = {userId}) OR post.user_id = {userId}" : "";

                int pageSize = 10;
                int offset = (pageNumber.GetValueOrDefault(1) - 1) * pageSize;

                string query = $@"
                    SELECT 
                        post.post_id,
                        post.post_text,
                        post.post_image,
                        post.user_id,
                        post.time_posted,
                        user.username,
                        profile.display_name,
                        profile.profile_picture,
                        COUNT(DISTINCT likes.like_id) AS like_count,
                        COUNT(DISTINCT replies.reply_id) AS reply_count,
                        COUNT(CASE WHEN likes.user_id = @userId THEN likes.like_id END) > 0 AS user_liked
                    FROM post
                    INNER JOIN user ON post.user_id = user.user_id
                    INNER JOIN profile ON post.user_id = profile.profile_id
                    LEFT JOIN likes ON post.post_id = likes.post_id
                    LEFT JOIN replies ON post.post_id = replies.post_id" +
                    searchLike +
                    " " +
                    followLike +
                    @" GROUP BY post.post_id
                    ORDER BY post.post_id DESC
                    LIMIT @pageSize OFFSET @offset";

                using (MySqlCommand cmd = new MySqlCommand(query, connection)) {
                    cmd.Parameters.AddWithValue("@userId", userId);
                    cmd.Parameters.AddWithValue("@pageSize", pageSize);
                    cmd.Parameters.AddWithValue("@offset", offset);
                    MySqlDataReader reader = (MySqlDataReader)await cmd.ExecuteReaderAsync();

                    List<object> result = new List<object>();
                    while (await reader.ReadAsync())
                    {
                        result.Add(new
                        {
                            post_id = reader.GetInt32(0),
                            post_text = reader.IsDBNull(1) ? "" : reader.GetString(1),
                            post_image = reader.IsDBNull(2) ? "" : reader.GetString(2),
                            user_id = reader.GetInt32(3),
                            time_posted = reader.GetDateTime(4),
                            username = reader.GetString(5),
                            display_name = reader.GetString(6),
                            profile_picture = reader.IsDBNull(7) ? "" : reader.GetString(7),
                            like_count = reader.GetInt32(8),
                            reply_count = reader.GetInt32(9),
                            user_liked = reader.GetBoolean(10)
                        });
                    }
                    return Ok(result);
                }
            }
            catch (Exception ex)
            {
                // Handle exceptions appropriately
                return StatusCode(500, $"Internal Server Error: {ex.Message}");
            }
            finally
            {
                connection.Close();
            }
        }

        [HttpGet("personal/user")]
        public async Task<IActionResult> GetPersonalUserPosts([FromQuery] int pageNumber, [FromQuery] int userId)
        {
            User user = (User)HttpContext.Items["User"];
            int currentUserId = user.id;
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                connection.Open();
                string query =
                    $@"SELECT 
                            post.post_id,
                            post.post_text,
                            post.post_image,
                            post.user_id,
                            post.time_posted,
                            user.username,
                            profile.display_name,
                            profile.profile_picture,
                            COUNT(DISTINCT likes.like_id) AS like_count,
                            COUNT(DISTINCT replies.reply_id) AS reply_count,
                            COUNT(CASE WHEN likes.user_id = @currentUserId THEN likes.like_id END) > 0 AS user_liked
                        FROM post
                        INNER JOIN user ON post.user_id = user.user_id
                        INNER JOIN profile ON post.user_id = profile.profile_id
                        LEFT JOIN likes ON post.post_id = likes.post_id
                        LEFT JOIN replies ON post.post_id = replies.post_id
                        WHERE post.user_id = @userId
                        GROUP BY post.post_id
                        ORDER BY post.post_id DESC
                        LIMIT 10 OFFSET @offset";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@currentUserId", currentUserId);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@offset", (pageNumber - 1) * 10);

                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        post_id = reader.GetInt32(0),
                        post_text = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        post_image = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        user_id = reader.GetInt32(3),
                        time_posted = reader.GetDateTime(4),
                        username = reader.GetString(5),
                        display_name = reader.GetString(6),
                        profile_picture = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        like_count = reader.GetInt32(8),
                        reply_count = reader.GetInt32(9),
                        user_liked = reader.GetBoolean(10)
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

        [HttpPost("personal")]
        public async Task<IActionResult> CreatePersonalPost([FromForm] PostRequest postRequest)
        {
            Console.WriteLine("Creating personal post");
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            if (string.IsNullOrWhiteSpace(postRequest.PostText) && postRequest.PostPicture == null)
            {
                return BadRequest(new { message = "No post text or files" });
            }
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                
                connection.Open();

                if (!string.IsNullOrWhiteSpace(postRequest.PostText) && postRequest.PostPicture != null)
                {
                    foreach (var formFile in postRequest.PostPicture)
                    {
                        using (var stream = formFile.OpenReadStream())
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);

                            var compressedBuffer = CompressImage(memoryStream.ToArray());

                            string base64Image = Convert.ToBase64String(compressedBuffer);

                            using (MySqlCommand command = new MySqlCommand("INSERT INTO post (user_id, post_text, post_image) VALUES (@userId, @postText, @postImage)", connection))
                            {
                                command.Parameters.AddWithValue("@userId", userId);
                                command.Parameters.AddWithValue("@postText", postRequest.PostText);
                                command.Parameters.AddWithValue("@postImage", base64Image);

                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new { postText = postRequest.PostText, postImage = "base64Image" });
                }
                else if (!string.IsNullOrWhiteSpace(postRequest.PostText))
                {
                    using (MySqlCommand command = new MySqlCommand("INSERT INTO post (user_id, post_text) VALUES (@userId, @postText)", connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@postText", postRequest.PostText);

                        command.ExecuteNonQuery();
                    }

                    return Ok(new { postText = postRequest.PostText });
                }
                else if (postRequest.PostPicture != null)
                {
                    foreach (var formFile in postRequest.PostPicture)
                    {
                        using (var stream = formFile.OpenReadStream())
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);

                            var compressedBuffer = CompressImage(memoryStream.ToArray());

                            string base64Image = Convert.ToBase64String(compressedBuffer);

                            using (MySqlCommand command = new MySqlCommand("INSERT INTO post (user_id, post_image) VALUES (@userId, @postImage)", connection))
                            {
                                command.Parameters.AddWithValue("@userId", userId);
                                command.Parameters.AddWithValue("@postImage", base64Image);

                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new { postImage = "base64Image" });
                }
                else
                {
                    return BadRequest(new { message = "Something went wrong" });
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



        [HttpGet("interests")]
        public async Task<IActionResult> GetInterestPosts(
            [FromQuery] int? pageNumber = 1,
            [FromQuery] string? search = null,
            [FromQuery] bool? explore = false)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            string searchLike = !string.IsNullOrEmpty(search) ? $" WHERE interest_post.post_text LIKE '%{search}%'" : "";
            string interestLike = (bool)!explore ? $"WHERE interest_post.interest_id IN (SELECT DISTINCT interest_id FROM subscribed_interest WHERE user_id = {userId})" : "";
            int pageSize = 10;
            int offset = (pageNumber.GetValueOrDefault(1) - 1) * pageSize;
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                
                connection.Open();

                string query =
                    $@"SELECT 
                            interest_post.post_id,
                            interest_post.post_text,
                            interest_post.post_image,
                            interest_post.user_id,
                            interest_post.time_posted,
                            interest.title,
                            user.username,
                            profile.display_name,
                            profile.profile_picture,
                            COUNT(DISTINCT interest_likes.like_id) AS like_count,
                            COUNT(DISTINCT interest_replies.reply_id) AS reply_count,
                            COUNT(CASE WHEN interest_likes.user_id = @userId THEN interest_likes.like_id END) > 0 AS user_liked
                        FROM interest_post
                        INNER JOIN user ON interest_post.user_id = user.user_id
                        INNER JOIN profile ON interest_post.user_id = profile.profile_id
                        INNER JOIN interest ON interest.interest_id = interest_post.interest_id
                        LEFT JOIN interest_likes ON interest_post.post_id = interest_likes.post_id
                        LEFT JOIN interest_replies ON interest_post.post_id = interest_replies.post_id
                        {searchLike} {interestLike}
                        GROUP BY interest_post.post_id
                        ORDER BY interest_post.post_id DESC
                        LIMIT @pageSize OFFSET @offset";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@pageSize", pageSize);
                command.Parameters.AddWithValue("@offset", offset);

                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        post_id = reader.GetInt32(0),
                        post_text = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        post_image = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        user_id = reader.GetInt32(3),
                        time_posted = reader.GetDateTime(4),
                        title = reader.GetString(5),
                        username = reader.GetString(6),
                        display_name = reader.GetString(7),
                        profile_picture = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        like_count = reader.GetInt32(9),
                        reply_count = reader.GetInt32(10),
                        user_liked = reader.GetBoolean(11)
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


        [HttpPost("interests")]
        public async Task<IActionResult> CreateInterestPost([FromForm] InterestPostRequest postRequest)
        {
            Console.WriteLine("Creating interest post");
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;

            if (string.IsNullOrWhiteSpace(postRequest.PostText) && postRequest.PostPicture == null)
            {
                return BadRequest(new { message = "No post text or files" });
            }
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                
                connection.Open();

                if (!string.IsNullOrWhiteSpace(postRequest.PostText) && postRequest.PostPicture != null)
                {
                    foreach (var formFile in postRequest.PostPicture)
                    {
                        using (var stream = formFile.OpenReadStream())
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);

                            var compressedBuffer = CompressImage(memoryStream.ToArray());

                            string base64Image = Convert.ToBase64String(compressedBuffer);

                            using (MySqlCommand command = new MySqlCommand("INSERT INTO interest_post (user_id, post_text, post_image, interest_id) VALUES (@userId, @postText, @postImage, @interestId)", connection))
                            {
                                command.Parameters.AddWithValue("@userId", userId);
                                command.Parameters.AddWithValue("@postText", postRequest.PostText);
                                command.Parameters.AddWithValue("@postImage", base64Image);
                                command.Parameters.AddWithValue("@interestId", postRequest.PostInterest);

                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new { postText = postRequest.PostText, postImage = "base64Image" });
                }
                else if (!string.IsNullOrWhiteSpace(postRequest.PostText))
                {
                    using (MySqlCommand command = new MySqlCommand("INSERT INTO interest_post (user_id, post_text, interest_id) VALUES (@userId, @postText, @interestId)", connection))
                    {
                        command.Parameters.AddWithValue("@userId", userId);
                        command.Parameters.AddWithValue("@postText", postRequest.PostText);
                        command.Parameters.AddWithValue("@interestId", postRequest.PostInterest);

                        command.ExecuteNonQuery();
                    }

                    return Ok(new { postText = postRequest.PostText });
                }
                else if (postRequest.PostPicture != null)
                {
                    foreach (var formFile in postRequest.PostPicture)
                    {
                        using (var stream = formFile.OpenReadStream())
                        using (var memoryStream = new MemoryStream())
                        {
                            await stream.CopyToAsync(memoryStream);

                            var compressedBuffer = CompressImage(memoryStream.ToArray());

                            string base64Image = Convert.ToBase64String(compressedBuffer);

                            using (MySqlCommand command = new MySqlCommand("INSERT INTO interest_post (user_id, post_image, interest_id) VALUES (@userId, @postImage, @interestId)", connection))
                            {
                                command.Parameters.AddWithValue("@userId", userId);
                                command.Parameters.AddWithValue("@postImage", base64Image);
                                command.Parameters.AddWithValue("@interestId", postRequest.PostInterest);
                                Console.WriteLine("Executing command and interest id = "+ postRequest.PostInterest);

                                command.ExecuteNonQuery();
                            }
                        }
                    }

                    return Ok(new { postImage = "base64Image" });
                }
                else
                {
                    return BadRequest(new { message = "Something went wrong" });
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

        [HttpGet("personal/{id}")]
        public async Task<IActionResult> GetPersonalPost(int id)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                
                connection.Open();

                string query =
                    $@"SELECT 
                            post.post_id,
                            post.post_text,
                            post.post_image,
                            post.user_id,
                            post.time_posted,
                            user.username,
                            profile.display_name,
                            profile.profile_picture,
                            COUNT(DISTINCT likes.like_id) AS like_count,
                            COUNT(DISTINCT replies.reply_id) AS reply_count,
                            COUNT(CASE WHEN likes.user_id = @userId THEN likes.like_id END) > 0 AS user_liked
                        FROM post
                        INNER JOIN user ON post.user_id = user.user_id
                        INNER JOIN profile ON post.user_id = profile.profile_id
                        LEFT JOIN likes ON post.post_id = likes.post_id
                        LEFT JOIN replies ON post.post_id = replies.post_id
                        WHERE post.post_id = @postId
                        GROUP BY post.post_id
                        LIMIT 1";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@postId", id);

                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        post_id = reader.GetInt32(0),
                        post_text = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        post_image = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        user_id = reader.GetInt32(3),
                        time_posted = reader.GetDateTime(4),
                        username = reader.GetString(5),
                        display_name = reader.GetString(6),
                        profile_picture = reader.IsDBNull(7) ? "" : reader.GetString(7),
                        like_count = reader.GetInt32(8),
                        reply_count = reader.GetInt32(9),
                        user_liked = reader.GetBoolean(10)
                    });
                }

                if (result.Count == 0)
                    return NotFound(new { message = "Post not found" });
                else
                    return Ok(result[0]);
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

        [HttpGet("interests/{id}")]
        public async Task<IActionResult> GetInterestPost(int id)
        {
            User user = (User)HttpContext.Items["User"];
            int userId = user.id;
            MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            try
            {
                
                connection.Open();

                string query =
                    $@"SELECT 
                            interest_post.post_id,
                            interest_post.post_text,
                            interest_post.post_image,
                            interest_post.user_id,
                            interest_post.time_posted,
                            interest.title,
                            user.username,
                            profile.display_name,
                            profile.profile_picture,
                            COUNT(DISTINCT interest_likes.like_id) AS like_count,
                            COUNT(DISTINCT interest_replies.reply_id) AS reply_count,
                            COUNT(CASE WHEN interest_likes.user_id = @userId THEN interest_likes.like_id END) > 0 AS user_liked
                        FROM interest_post
                        INNER JOIN user ON interest_post.user_id = user.user_id
                        INNER JOIN profile ON interest_post.user_id = profile.profile_id
                        INNER JOIN interest ON interest.interest_id = interest_post.interest_id
                        LEFT JOIN interest_likes ON interest_post.post_id = interest_likes.post_id
                        LEFT JOIN interest_replies ON interest_post.post_id = interest_replies.post_id
                        WHERE interest_post.post_id = @postId
                        GROUP BY interest_post.post_id
                        LIMIT 1";

                MySqlCommand command = new MySqlCommand(query, connection);
                command.Parameters.AddWithValue("@userId", userId);
                command.Parameters.AddWithValue("@postId", id);

                using MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync();
                List<object> result = new List<object>();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        post_id = reader.GetInt32(0),
                        post_text = reader.IsDBNull(1) ? "" : reader.GetString(1),
                        post_image = reader.IsDBNull(2) ? "" : reader.GetString(2),
                        user_id = reader.GetInt32(3),
                        time_posted = reader.GetDateTime(4),
                        title = reader.GetString(5),
                        username = reader.GetString(6),
                        display_name = reader.GetString(7),
                        profile_picture = reader.IsDBNull(8) ? "" : reader.GetString(8),
                        like_count = reader.GetInt32(9),
                        reply_count = reader.GetInt32(10),
                        user_liked = reader.GetBoolean(11)
                    });
                }

                if (result.Count == 0)
                    return NotFound(new { message = "Post not found" });
                else
                    return Ok(result[0]);
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

        private byte[] CompressImage(byte[] inputImage)
        {
            using (var image = Image.Load(inputImage))
            using (var outputStream = new MemoryStream())
            {
                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(800, 600),
                    Mode = ResizeMode.Max
                }));

                image.Save(outputStream, new JpegEncoder());

                return outputStream.ToArray();
            }
        }
    }

    public class PostRequest
    {
        public string? PostText { get; set; }
        public IFormFileCollection? PostPicture { get; set; }
    }

    public class InterestPostRequest
    {
        public string? PostText { get; set; }
        public IFormFileCollection? PostPicture { get; set; }
        public int PostInterest { get; set; }
    }
}
