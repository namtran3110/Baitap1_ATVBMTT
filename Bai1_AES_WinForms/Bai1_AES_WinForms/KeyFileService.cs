using System.IO;
using System;
using System.Security.Cryptography;
using System.Text.Json;

namespace Bai1_AES_WinForms;

/// <summary>
/// Đọc/ghi khóa AES-256 ra tệp JSON để người học dễ quan sát.
/// Lưu ý: tệp khóa này không được mã hóa; không dùng cách lưu này cho dữ liệu nhạy cảm thực tế.
/// </summary>
internal static class KeyFileService
{
    private const string AlgorithmName = "AES-256-GCM";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private sealed class KeyFileDocument
    {
        public int Version { get; set; } = 1;
        public string Algorithm { get; set; } = AlgorithmName;
        public string KeyBase64 { get; set; } = string.Empty;
    }

    public static void Save(string filePath, byte[] key)
    {
        if (key is null || key.Length != AesGcmService.KeySizeBytes)
            throw new ArgumentException("Khóa AES-256 phải có đúng 32 byte.", nameof(key));

        var document = new KeyFileDocument
        {
            Version = 1,
            Algorithm = AlgorithmName,
            KeyBase64 = Convert.ToBase64String(key)
        };

        string json = JsonSerializer.Serialize(document, JsonOptions);
        File.WriteAllText(filePath, json);
    }

    public static byte[] Load(string filePath)
    {
        string json = File.ReadAllText(filePath);
        KeyFileDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<KeyFileDocument>(json);
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException("Tệp khóa không phải JSON hợp lệ hoặc không đúng định dạng của ứng dụng.", ex);
        }

        if (document is null)
            throw new InvalidDataException("Không đọc được thông tin khóa trong tệp.");

        if (document.Version != 1 || !string.Equals(document.Algorithm, AlgorithmName, StringComparison.Ordinal))
            throw new InvalidDataException("Phiên bản hoặc thuật toán trong tệp khóa không được hỗ trợ.");

        byte[] key;
        try
        {
            key = Convert.FromBase64String(document.KeyBase64);
        }
        catch (FormatException ex)
        {
            throw new InvalidDataException("Giá trị KeyBase64 trong tệp khóa không hợp lệ.", ex);
        }

        if (key.Length != AesGcmService.KeySizeBytes)
        {
            CryptographicOperations.ZeroMemory(key);
            throw new InvalidDataException("Khóa trong tệp không có đúng 32 byte, không phải khóa AES-256 hợp lệ.");
        }

        return key;
    }
}
