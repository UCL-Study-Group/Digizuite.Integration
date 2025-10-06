using Digizuite.Common.Models;
using Microsoft.AspNetCore.StaticFiles;

namespace Digizuite.Client.Helpers;

public static class FileHelper
{
    public static async Task<TransferFile> GetReadyFileAsync(string filePath)
    {
        var provider = new FileExtensionContentTypeProvider();

        if (provider.TryGetContentType(filePath, out var contentType))
        {
            return new TransferFile()
            {
                FileName = Path.GetFileName(filePath),
                MimeType = contentType,
                Data = await File.ReadAllBytesAsync(filePath),
            };
        }
        else
        {
            return new TransferFile()
            {
                FileName = filePath,
                MimeType = filePath,
                Data = null,
            };
        }
    }
}