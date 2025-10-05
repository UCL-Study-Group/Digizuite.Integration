using System.Text;
using System.Text.Json;

namespace Digizuite.Common.Helpers;

public static class EncodingHelper
{
    public static byte[] EncodeMessage(object body)
    {
        var json = JsonSerializer.Serialize(body);
        
        var encoded = Encoding.UTF8.GetBytes(json);
        
        return encoded;
    }
}