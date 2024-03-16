using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.IO;
using MySql.Data.MySqlClient;
using SixLabors.ImageSharp.Formats.Jpeg;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

[Route("api/[controller]")]
[ApiController]
public class UserController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public UserController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUserProfile(int id)
    {
        using (MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            await connection.OpenAsync();

            using (MySqlCommand command = new MySqlCommand(
                "SELECT * FROM user " +
                "INNER JOIN profile ON user.user_id = profile.profile_id " +
                "WHERE user.user_id = @id " +
                "LIMIT 1", connection))
            {
                command.Parameters.AddWithValue("@id", id);

                using (MySqlDataReader reader = (MySqlDataReader)await command.ExecuteReaderAsync())
                {
                    if (await reader.ReadAsync())
                    {
                        string profilePicture = null;
                        string coverPicture = null;
                        string bio = null;
                        if(!reader.IsDBNull("profile_picture"))
                            profilePicture = reader["profile_picture"].ToString();
                        if (!reader.IsDBNull("cover_picture"))
                            coverPicture = reader["cover_picture"].ToString();
                        if (!reader.IsDBNull("bio"))
                            bio = reader["bio"].ToString();

                        return Ok(new
                        {
                            username = reader["username"],
                            displayName = reader["display_name"],
                            profilePicture = profilePicture,
                            coverPicture = coverPicture,
                            bio = bio
                        });
                    }
                    else
                    {
                        return NotFound();
                    }
                }
            }
        }
    }

    [HttpPut]
    public async Task<IActionResult> UpdateUserProfile([FromForm] UserProfileRequest userProfileRequest)
    {
        User user = (User)HttpContext.Items["User"];
        int userId = user.id;
        using MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        try
        {
            
            connection.Open();

            if (userProfileRequest.ProfilePicture != null)
            {
                using (var stream = userProfileRequest.ProfilePicture.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);

                    var compressedBuffer = CompressImage(memoryStream.ToArray());

                    string base64Image = Convert.ToBase64String(compressedBuffer);

                    using (MySqlCommand command = new MySqlCommand("UPDATE profile SET display_name = @displayName, bio = @bio, profile_picture = @profilePicture WHERE profile_id = @userId", connection))
                    {
                        command.Parameters.AddWithValue("@displayName", userProfileRequest.DisplayName);
                        command.Parameters.AddWithValue("@bio", userProfileRequest.Bio);
                        command.Parameters.AddWithValue("@profilePicture", base64Image);
                        command.Parameters.AddWithValue("@userId", userId);

                        command.ExecuteNonQuery();
                    }
                }
            }

            if (userProfileRequest.CoverPicture != null)
            {
                using (var stream = userProfileRequest.CoverPicture.OpenReadStream())
                using (var memoryStream = new MemoryStream())
                {
                    await stream.CopyToAsync(memoryStream);

                    var compressedBuffer = CompressImage(memoryStream.ToArray());

                    string base64Image = Convert.ToBase64String(compressedBuffer);

                    using (MySqlCommand command = new MySqlCommand("UPDATE profile SET display_name = @displayName, bio = @bio, cover_picture = @coverPicture WHERE profile_id = @userId", connection))
                    {
                        command.Parameters.AddWithValue("@displayName", userProfileRequest.DisplayName);
                        command.Parameters.AddWithValue("@bio", userProfileRequest.Bio);
                        command.Parameters.AddWithValue("@coverPicture", base64Image);
                        command.Parameters.AddWithValue("@userId", userId);

                        command.ExecuteNonQuery();
                    }
                }
            }

            if (userProfileRequest.ProfilePicture == null && userProfileRequest.CoverPicture == null)
            {
                using (MySqlCommand command = new MySqlCommand("UPDATE profile SET display_name = @displayName, bio = @bio WHERE profile_id = @userId", connection))
                {
                    command.Parameters.AddWithValue("@displayName", userProfileRequest.DisplayName);
                    command.Parameters.AddWithValue("@bio", userProfileRequest.Bio);
                    command.Parameters.AddWithValue("@userId", userId);

                    command.ExecuteNonQuery();
                }
            }

            return Ok(new
            {
                displayName = userProfileRequest.DisplayName,
                bio = userProfileRequest.Bio,
                profilePicture = "base64Image", // Add the actual base64Image value here
                coverPicture = "base64Image"    // Add the actual base64Image value here
            });
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

    public class UserProfileRequest
    {
        public string DisplayName { get; set; }
        public string Bio { get; set; }
        public IFormFile? ProfilePicture { get; set; }
        public IFormFile? CoverPicture { get; set; }
    }
}