using Microsoft.Extensions.Logging;
using System.Drawing.Imaging;
using System.Drawing;
using System.Runtime.Versioning;
using System.Text.Json;
using System.Text;

namespace ITunesShortcuts.Services;

public class ImageUploader
{
    public static readonly string ArtworksDirctory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Artworks");


    readonly ILogger<ImageUploader> logger;
    readonly JsonConverter converter;

    public ImageUploader(
        ILogger<ImageUploader> logger,
        JsonConverter converter)
    {
        this.logger = logger;
        this.converter = converter;

        Directory.CreateDirectory(ArtworksDirctory);
        onlineImageCache = File.Exists(Path.Combine(ArtworksDirctory, "onlineImageCache.json")) ? converter.ToObject<Dictionary<string, string>>(File.ReadAllText(Path.Combine(ArtworksDirctory, "onlineImageCache.json"))) ?? new() : new();

        logger.LogInformation("[ImageUploader-.ctor] ImageUploader has been initialized.");
    }


    readonly Dictionary<string, string> onlineImageCache;
    readonly HttpClient client = new();


    public void SaveOnlineCache()
    {
        string cache = converter.ToString(onlineImageCache);
        File.WriteAllText(Path.Combine(ArtworksDirctory, "onlineImageCache.json"), cache);

        logger.LogInformation("[ImageUploader-SaveOnlineCache] Saved online image cache.");
    }


    async Task<string> UploadAsync(
        string filePath,
        int resolution)
    {
        logger.LogInformation("[ImageUploader-UploadAsync] Uploading image...");

        byte[] image = await File.ReadAllBytesAsync(filePath);
        HttpRequestMessage request = new(HttpMethod.Post, "https://freeimage.host/api/1/upload")
        {
            Content = new StringContent($"key=6d207e02198a847aa98d0a2a901485a5&image={Uri.EscapeDataString(Convert.ToBase64String(image))}", Encoding.UTF8, "application/x-www-form-urlencoded")
        };

        HttpResponseMessage response = await client.SendAsync(request);
        response.EnsureSuccessStatusCode();

        string json = await response.Content.ReadAsStringAsync();
        string? url = JsonDocument.Parse(json).RootElement.GetProperty("image").GetProperty("url").GetString();

        return url!;
    }

    public async Task<string?> GetAsync(
        string filePath)
    {
        if (onlineImageCache.TryGetValue(filePath, out string? result))
            return result;

        try
        {
            string url = await UploadAsync(filePath, 128);
            onlineImageCache[filePath] = url;

            return url;
        }
        catch (Exception ex)
        {
            logger.LogInformation("[ImageUploader-GetAsync] Failed to upload image: {exception}", ex.Message);
            return null;
        }
    }
}