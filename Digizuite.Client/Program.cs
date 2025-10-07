using Digizuite.Client.Helpers;
using Digizuite.Client.Services;
using Digizuite.Common.Models;

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
            Console.WriteLine("[Client] Type file path or test...");
            
            var input = Console.ReadLine();

            if (input == "exit")
                break;

            if (Path.Exists(input))
            {
                var filePaths = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/Files");
                
                foreach (var filePath in filePaths)
                    await RouteFileAsync(filePath);
            }
            
            if (input == "test")
            {
                await _rabbitService.SendFileAsync(new TransferFile
                {
                    FileName = "Video File",
                    MimeType = "video/mp4",
                    Data = []
                });
            } 
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
            case "image/jpeg":
                await _rabbitService.SendFileAsync(file);
                break;
            default:
                Console.WriteLine("Unsupported file type!");
                break;
        }
    }
}