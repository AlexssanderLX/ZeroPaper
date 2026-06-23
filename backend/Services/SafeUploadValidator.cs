using Microsoft.AspNetCore.Http;

namespace ZeroPaper.Services;

internal static class SafeUploadValidator
{
    private const int HeaderLength = 12;

    public static async Task<string> GetImageExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = await ReadHeaderAsync(file, cancellationToken);

        if (IsJpeg(header)) return ".jpg";
        if (IsPng(header)) return ".png";
        if (IsWebp(header)) return ".webp";

        throw new ArgumentException("O arquivo enviado nao possui uma assinatura valida de imagem.", nameof(file));
    }

    public static async Task<string> GetAudioExtensionAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var header = await ReadHeaderAsync(file, cancellationToken);

        if (IsWav(header)) return ".wav";
        if (IsMp3(header)) return ".mp3";
        if (IsOgg(header)) return ".ogg";

        throw new ArgumentException("O arquivo enviado nao possui uma assinatura valida de audio.", nameof(file));
    }

    private static async Task<byte[]> ReadHeaderAsync(IFormFile file, CancellationToken cancellationToken)
    {
        await using var stream = file.OpenReadStream();
        var header = new byte[HeaderLength];
        var bytesRead = await stream.ReadAsync(header.AsMemory(0, HeaderLength), cancellationToken);
        return header[..bytesRead];
    }

    private static bool IsJpeg(byte[] header) =>
        header.Length >= 3 &&
        header[0] == 0xff &&
        header[1] == 0xd8 &&
        header[2] == 0xff;

    private static bool IsPng(byte[] header) =>
        header.Length >= 8 &&
        header[0] == 0x89 &&
        header[1] == 0x50 &&
        header[2] == 0x4e &&
        header[3] == 0x47 &&
        header[4] == 0x0d &&
        header[5] == 0x0a &&
        header[6] == 0x1a &&
        header[7] == 0x0a;

    private static bool IsWebp(byte[] header) =>
        header.Length >= 12 &&
        HasAscii(header, 0, "RIFF") &&
        HasAscii(header, 8, "WEBP");

    private static bool IsWav(byte[] header) =>
        header.Length >= 12 &&
        HasAscii(header, 0, "RIFF") &&
        HasAscii(header, 8, "WAVE");

    private static bool IsMp3(byte[] header) =>
        header.Length >= 3 &&
        (HasAscii(header, 0, "ID3") ||
         (header[0] == 0xff && (header[1] & 0xe0) == 0xe0));

    private static bool IsOgg(byte[] header) =>
        header.Length >= 4 &&
        HasAscii(header, 0, "OggS");

    private static bool HasAscii(byte[] header, int offset, string expected)
    {
        if (header.Length < offset + expected.Length)
        {
            return false;
        }

        for (var index = 0; index < expected.Length; index++)
        {
            if (header[offset + index] != expected[index])
            {
                return false;
            }
        }

        return true;
    }
}
