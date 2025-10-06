namespace Digizuite.Client.Models;

public class PreparedFile
{
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public required byte[]? Data { get; set; }
}