using Digizuite.Client.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace Digizuite.Client.Helpers;

public static class FileHelper
{
    public static async Task<PreparedFile> GetReadyFileAsync(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();

        if (provider.TryGetContentType(filePath, out var contentType))
        {
            return new PreparedFile()
            {
                FileName = Path.GetFileName(filePath),
                MimeType = contentType,
                Data = await File.ReadAllBytesAsync(filePath),
            };
        }
        else
        {
            return new PreparedFile()
            {
                FileName = filePath,
                MimeType = filePath,
                Data = null,
            };
        }
    }
}