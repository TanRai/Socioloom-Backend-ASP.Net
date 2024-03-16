using System;
using System.Data;
using System.Data.SqlClient;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using MySql.Data.MySqlClient;
using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Jose;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;

    public AuthController(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        using (MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            try
            {
                await connection.OpenAsync();
                Console.WriteLine("Connected to database");
                // Check if username exists
                using (MySqlCommand usernameCommand = new MySqlCommand("SELECT * FROM `user` WHERE username = @username", connection))
                {
                    usernameCommand.Parameters.AddWithValue("@username", request.Username);
                    using MySqlDataReader usernameReader = (MySqlDataReader)await usernameCommand.ExecuteReaderAsync();
                    if (usernameReader.HasRows)
                    {
                        return BadRequest(new { message = "Username already exists" });
                    }
                    usernameReader.Close();
                }
                Console.WriteLine("Username does not exist");
                // Check if email exists
                using (MySqlCommand emailCommand = new MySqlCommand("SELECT * FROM `user` WHERE email = @email", connection))
                {
                    emailCommand.Parameters.AddWithValue("@email", request.Email);
                    using MySqlDataReader emailReader = (MySqlDataReader)await emailCommand.ExecuteReaderAsync();
                    if (emailReader.HasRows)
                    {
                        return BadRequest(new { message = "Email already exists" });
                    }
                    emailReader.Close();
                }
                Console.WriteLine("Email does not exist");
                // Hash the password
                string hashedPassword = BCrypt.Net.BCrypt.HashPassword(request.Password, 10);

                // Insert user data
                using (MySqlCommand insertUserCommand = new MySqlCommand("INSERT INTO `user` (username, email, password) VALUES (@username, @email, @password); SELECT LAST_INSERT_ID();", connection))
                {
                    insertUserCommand.Parameters.AddWithValue("@username", request.Username);
                    insertUserCommand.Parameters.AddWithValue("@email", request.Email);
                    insertUserCommand.Parameters.AddWithValue("@password", hashedPassword);
                    int userId = Convert.ToInt32(await insertUserCommand.ExecuteScalarAsync());

                    // Insert profile data
                    using (MySqlCommand insertProfileCommand = new MySqlCommand("INSERT INTO profile (profile_id, display_name) VALUES (@profileId, @displayName)", connection))
                    {
                        insertProfileCommand.Parameters.AddWithValue("@profileId", userId);
                        insertProfileCommand.Parameters.AddWithValue("@displayName", request.DisplayName);
                        int i = await insertProfileCommand.ExecuteNonQueryAsync();
                    }

                    // Generate and return JWT token
                    var payload = new Dictionary<string, object>()
                            {
                                { "username", request.Username },
                                { "email", request.Email },
                                { "id", userId }
                            };

                    var secretKey = Encoding.ASCII.GetBytes("secretkey");

                    string token = Jose.JWT.Encode(payload, secretKey, JwsAlgorithm.HS256);
                    return Ok(new { token = token, user = new { username = request.Username, email = request.Email, id = userId } });
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }


    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        using (MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection")))
        {
            try
            {
                await connection.OpenAsync();

                // Check if email or username exists
                using (MySqlCommand selectUserCommand = new MySqlCommand("SELECT * FROM `user` WHERE email = @email OR username = @username", connection))
                {
                    selectUserCommand.Parameters.AddWithValue("@email", request.email_username);
                    selectUserCommand.Parameters.AddWithValue("@username", request.email_username);
                    using MySqlDataReader userReader = (MySqlDataReader)await selectUserCommand.ExecuteReaderAsync();

                    if (userReader.HasRows)
                    {
                        await userReader.ReadAsync();
                        string storedPasswordHash = userReader["password"].ToString();

                        // Check password
                        if (BCrypt.Net.BCrypt.Verify(request.Password, storedPasswordHash))
                        {
                            int userId = Convert.ToInt32(userReader["user_id"]);

                            var payload = new Dictionary<string, object>()
                            {
                                { "username", userReader["username"] },
                                { "email", userReader["email"] },
                                { "id", userId }
                            };

                            var secretKey = Encoding.ASCII.GetBytes("secretkey");

                            string token = Jose.JWT.Encode(payload, secretKey, JwsAlgorithm.HS256);



                            return Ok(new { token = token, user = new { username = userReader["username"], email = userReader["email"], id = userId } });
                        }
                        else
                        {
                            return BadRequest(new { message = "Wrong password" });
                        }
                    }
                    else
                    {
                        return BadRequest(new { message = "Wrong email/username" });
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = ex.Message });
            }
        }
    }
}


public class RegisterRequest
{
    public string Username { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
    public string DisplayName { get; set; }
}

public class LoginRequest
{
    public string email_username { get; set; }
    public string Password { get; set; }
}
 