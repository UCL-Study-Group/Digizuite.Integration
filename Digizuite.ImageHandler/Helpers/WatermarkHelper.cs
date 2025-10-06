using ImageMagick;

namespace Digizuite.ImageHandler.Helpers;

public class WatermarkHelper
{
    public static async Task<byte[]> WatermarkImage(byte[] data)
    {
        try
        {
            Console.WriteLine("Watermarking image...");
            
            using var image = new MagickImage(data);

            using var watermark = new MagickImage(await GetWatermark());
            
            watermark.Evaluate(Channels.Alpha, EvaluateOperator.Divide, 4);

            image.Composite(watermark, Gravity.Center, CompositeOperator.Over);

            return image.ToByteArray();
        }
        catch (Exception ex)
        {
            Console.WriteLine("Encountered an error while watermarking, {0}", ex.Message);
            return [];
        }
    }

    private static async Task<byte[]> GetWatermark()
    {
        var files = Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory + "/Files/Watermarks");

        if (files.Length == 0)
            return [];
        
        var file = await File.ReadAllBytesAsync(files[0]);
        
        return file;
    }
}