using System.Text;
using System.Text.Json;
using Digizuite.Client.Helpers;
using Digizuite.Client.Services;
using Digizuite.Common.Constants;
using Digizuite.Common.Helpers;
using RabbitMQ.Client;

namespace Digizuite.Client;

class Program
{
    private static RabbitService? _rabbitService;
    
    static async Task Main()
    {
        _rabbitService = new RabbitService();
        
        await _rabbitService.SetupConnectionsAsync();
        await _rabbitService.DeclareConnectionsAsync();

        while (true)
        {
            Console.WriteLine("Searching for files...");

            var filePaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/Files");
            
            foreach (var filePath in filePaths)
                await RouteFileAsync(filePath);
            
            var input = Console.ReadLine();
            
            if (input == "exit")
                break;
            
            Thread.Sleep(3000);
            Console.Clear();
        }
    }

    private static async Task RouteFileAsync(string input)
    {
        if (_rabbitService == null)
            return;

        var file = await FileHelper.GetReadyFileAsync(input);
        
        switch (file.MimeType)
        {
            case "video/mp4":
                await _rabbitService.SendFileAsync(file);
                break;
            case "image/jpeg":
                await _rabbitService.SendFileAsync(file);
                break;
            default:
                Console.WriteLine("Unsupported file type!");
                break;
        }
    }
}