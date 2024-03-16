using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class AuthMiddleware
{
    private readonly RequestDelegate _next;
    private readonly IConfiguration _configuration;

    public AuthMiddleware(RequestDelegate next, IConfiguration configuration)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
    }

    public async Task Invoke(HttpContext context)
    {
        var token = context.Request.Headers["x-auth-token"].ToString();

        if (string.IsNullOrEmpty(token))
        {
            Console.WriteLine("No token provided 22");
            context.Response.StatusCode = 401;
            await context.Response.WriteAsync("Access denied. No token provided. 22");
            return;
        }

        try
        {
            var key = Encoding.ASCII.GetBytes("secretkey");

            var secretKey = Encoding.ASCII.GetBytes("secretkey");

            string json = Jose.JWT.Decode(token, secretKey);

            JObject json3 = JObject.Parse(json);

            context.Items["User"] = json3.ToObject<User>(); ;

            await _next(context);
        }
        catch (Exception ex)
        {
            Console.WriteLine("Message = "+ex.Message);
            Console.WriteLine("Invalid token");
            context.Response.StatusCode = 400;
            await context.Response.WriteAsync("Invalid token.");
        }
    }
}

public class User
{
    public string username { get; set; }
    public string email { get; set; }
    public int id { get; set; }
}