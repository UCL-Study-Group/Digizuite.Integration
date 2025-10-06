namespace Digizuite.Common.Models;

public class TransferFile
{
    public required string FileName { get; set; }
    public required string MimeType { get; set; }
    public required byte[]? Data { get; set; }
}