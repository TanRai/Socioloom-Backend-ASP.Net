using Microsoft.AspNetCore.SignalR;
using MySql.Data.MySqlClient;
using Mysqlx.Crud;
using System.Threading.Tasks;

public class ChatHub : Hub
{
    private readonly IConfiguration _configuration;

    public ChatHub(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public async Task SendMessage(string chatId, string message, string sender)
    {
        Console.WriteLine("Sending message to chatId: " + chatId);
        string query = @"INSERT INTO message (chat_id, message_text, sender) VALUES(@chatId, @message, @sender)";
        MySqlConnection connection = new MySqlConnection(_configuration.GetConnectionString("DefaultConnection"));
        connection.Open();

        MySqlCommand insertCommand = new MySqlCommand(query, connection);
        insertCommand.Parameters.AddWithValue("@chatId", chatId);
        insertCommand.Parameters.AddWithValue("@message", message);
        insertCommand.Parameters.AddWithValue("@sender", sender);

        insertCommand.ExecuteNonQuery();

        await Clients.Group(chatId).SendAsync("ReceiveMessage", new { message, sender });
    }

    public async Task JoinChat(string chatId)
    {
        Console.WriteLine("Joining chat: " + chatId);
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
    }
}