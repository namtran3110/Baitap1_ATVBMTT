using System.IO;
using System;
using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Bai1_AES_WinForms;

public sealed record FileCryptResult(long InputBytes, long OutputBytes);

public sealed record FileComparisonResult(
    bool AreEqual,
    long ComparedBytes,
    long? FirstDifferenceOffset,
    long FirstLength,
    long SecondLength);

/// <summary>
/// AES-256-GCM cho văn bản và tệp lớn.
/// Tệp được chia thành các khối độc lập 1 MiB; mỗi khối có authentication tag riêng.
/// Điều này giúp giới hạn RAM và phát hiện khối bị sửa, bị đảo thứ tự hoặc bị cắt cụt.
/// </summary>
public static class AesGcmService
{
    public const int KeySizeBytes = 32; // AES-256 = 256 bit = 32 byte
    public const int DefaultChunkSize = 1024 * 1024; // 1 MiB

    private const int NonceSizeBytes = 12;
    private const int TagSizeBytes = 16;
    private const int FileHeaderSize = 24;
    private const int RecordHeaderSize = 5; // Int32 little-endian length + 1 byte flags
    private const int MaxAllowedChunkSize = 4 * 1024 * 1024;
    private const byte FinalChunkFlag = 0x01;
    private static readonly byte[] FileMagic = Encoding.ASCII.GetBytes("AESGCM02");
    private static readonly byte[] TextMagic = Encoding.ASCII.GetBytes("AGT1");
    private static readonly UTF8Encoding StrictUtf8 = new(false, true);

    public static byte[] GenerateKey() => RandomNumberGenerator.GetBytes(KeySizeBytes);

    public static string EncryptText(string plaintext, byte[] key)
    {
        ValidateKey(key);
        plaintext ??= string.Empty;

        byte[] plaintextBytes = Encoding.UTF8.GetBytes(plaintext);
        byte[] nonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
        byte[] ciphertext = new byte[plaintextBytes.Length];
        byte[] tag = new byte[TagSizeBytes];

        using (var aes = new AesGcm(key, TagSizeBytes))
        {
            aes.Encrypt(nonce, plaintextBytes, ciphertext, tag, TextMagic);
        }

        // Định dạng dữ liệu văn bản: magic (4) | nonce (12) | tag (16) | ciphertext (N).
        byte[] payload = new byte[TextMagic.Length + NonceSizeBytes + TagSizeBytes + ciphertext.Length];
        TextMagic.CopyTo(payload, 0);
        nonce.CopyTo(payload, TextMagic.Length);
        tag.CopyTo(payload, TextMagic.Length + NonceSizeBytes);
        ciphertext.CopyTo(payload, TextMagic.Length + NonceSizeBytes + TagSizeBytes);

        CryptographicOperations.ZeroMemory(plaintextBytes);
        CryptographicOperations.ZeroMemory(ciphertext);
        CryptographicOperations.ZeroMemory(nonce);
        CryptographicOperations.ZeroMemory(tag);
        return Convert.ToBase64String(payload);
    }

    public static string DecryptText(string base64Payload, byte[] key)
    {
        ValidateKey(key);
        if (string.IsNullOrWhiteSpace(base64Payload))
            throw new CryptographicException("Bạn chưa nhập dữ liệu mã hóa dạng Base64.");

        byte[] payload;
        try
        {
            payload = Convert.FromBase64String(base64Payload.Trim());
        }
        catch (FormatException ex)
        {
            throw new CryptographicException("Dữ liệu nhập không phải chuỗi Base64 hợp lệ.", ex);
        }

        int minimumLength = TextMagic.Length + NonceSizeBytes + TagSizeBytes;
        if (payload.Length < minimumLength)
            throw new CryptographicException("Dữ liệu mã hóa quá ngắn hoặc không đúng định dạng của ứng dụng.");

        if (!payload.AsSpan(0, TextMagic.Length).SequenceEqual(TextMagic))
            throw new CryptographicException("Không nhận diện được định dạng dữ liệu AES-GCM này.");

        ReadOnlySpan<byte> nonce = payload.AsSpan(TextMagic.Length, NonceSizeBytes);
        ReadOnlySpan<byte> tag = payload.AsSpan(TextMagic.Length + NonceSizeBytes, TagSizeBytes);
        ReadOnlySpan<byte> ciphertext = payload.AsSpan(minimumLength);
        byte[] plaintext = new byte[ciphertext.Length];

        try
        {
            using var aes = new AesGcm(key, TagSizeBytes);
            aes.Decrypt(nonce, ciphertext, tag, plaintext, TextMagic);
            return StrictUtf8.GetString(plaintext);
        }
        catch (AuthenticationTagMismatchException ex)
        {
            throw new CryptographicException("Xác thực thất bại. Dữ liệu có thể đã bị sửa hoặc bạn đang dùng sai khóa AES.", ex);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plaintext);
            CryptographicOperations.ZeroMemory(payload);
        }
    }

    public static FileCryptResult EncryptFile(string inputPath, string outputPath, byte[] key)
    {
        ValidateKey(key);
        ValidatePaths(inputPath, outputPath);

        long inputLength = new FileInfo(inputPath).Length;
        string temporaryPath = CreateTemporaryPathBeside(outputPath);

        try
        {
            using (var input = new FileStream(
                       inputPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                       DefaultChunkSize, FileOptions.SequentialScan))
            using (var output = new FileStream(
                       temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                       DefaultChunkSize, FileOptions.SequentialScan))
            using (var aes = new AesGcm(key, TagSizeBytes))
            {
                byte[] header = new byte[FileHeaderSize];
                FileMagic.CopyTo(header, 0);
                BinaryPrimitives.WriteInt32LittleEndian(header.AsSpan(8, 4), DefaultChunkSize);
                byte[] baseNonce = RandomNumberGenerator.GetBytes(NonceSizeBytes);
                baseNonce.CopyTo(header, 12);
                output.Write(header);

                byte[] currentBuffer = new byte[DefaultChunkSize];
                byte[] nextBuffer = new byte[DefaultChunkSize];
                byte[] cipherBuffer = new byte[DefaultChunkSize];
                byte[] tag = new byte[TagSizeBytes];
                byte[] nonce = new byte[NonceSizeBytes];
                byte[] recordHeader = new byte[RecordHeaderSize];
                byte[] aad = new byte[FileHeaderSize + 9];
                header.CopyTo(aad, 0);

                int currentCount = ReadChunk(input, currentBuffer);
                ulong chunkIndex = 0;

                while (true)
                {
                    if (chunkIndex > uint.MaxValue)
                        throw new CryptographicException("Tệp có quá nhiều khối để biểu diễn bộ đếm nonce của định dạng này.");

                    // Đọc trước một khối để biết khối hiện tại có phải khối cuối cùng không.
                    int nextCount = currentCount > 0 ? ReadChunk(input, nextBuffer) : 0;
                    bool isFinal = nextCount == 0;
                    byte flags = isFinal ? FinalChunkFlag : (byte)0;

                    BinaryPrimitives.WriteInt32LittleEndian(recordHeader.AsSpan(0, 4), currentCount);
                    recordHeader[4] = flags;
                    BinaryPrimitives.WriteUInt32BigEndian(aad.AsSpan(FileHeaderSize, 4), (uint)chunkIndex);
                    BinaryPrimitives.WriteInt32LittleEndian(aad.AsSpan(FileHeaderSize + 4, 4), currentCount);
                    aad[FileHeaderSize + 8] = flags;
                    CreateNonce(baseNonce, (uint)chunkIndex, nonce);

                    aes.Encrypt(
                        nonce,
                        currentBuffer.AsSpan(0, currentCount),
                        cipherBuffer.AsSpan(0, currentCount),
                        tag,
                        aad);

                    output.Write(recordHeader);
                    if (currentCount > 0)
                        output.Write(cipherBuffer, 0, currentCount);
                    output.Write(tag);

                    if (isFinal)
                        break;

                    (currentBuffer, nextBuffer) = (nextBuffer, currentBuffer);
                    currentCount = nextCount;
                    chunkIndex++;
                }

                output.Flush(flushToDisk: true);
                CryptographicOperations.ZeroMemory(baseNonce);
                CryptographicOperations.ZeroMemory(currentBuffer);
                CryptographicOperations.ZeroMemory(nextBuffer);
                CryptographicOperations.ZeroMemory(cipherBuffer);
                CryptographicOperations.ZeroMemory(tag);
                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(aad);
            }

            File.Move(temporaryPath, outputPath, overwrite: true);
            return new FileCryptResult(inputLength, new FileInfo(outputPath).Length);
        }
        finally
        {
            TryDelete(temporaryPath);
        }
    }

    public static FileCryptResult DecryptFile(string inputPath, string outputPath, byte[] key)
    {
        ValidateKey(key);
        ValidatePaths(inputPath, outputPath);

        long encryptedLength = new FileInfo(inputPath).Length;
        string temporaryPath = CreateTemporaryPathBeside(outputPath);

        try
        {
            long decryptedLength = 0;
            using (var input = new FileStream(
                       inputPath, FileMode.Open, FileAccess.Read, FileShare.Read,
                       DefaultChunkSize, FileOptions.SequentialScan))
            {
                byte[] header = new byte[FileHeaderSize];
                ReadExactly(input, header);

                if (!header.AsSpan(0, FileMagic.Length).SequenceEqual(FileMagic))
                    throw new CryptographicException("Đây không phải tệp AES-GCM do ứng dụng này tạo ra, hoặc phần đầu tệp đã bị thay đổi.");

                int chunkSize = BinaryPrimitives.ReadInt32LittleEndian(header.AsSpan(8, 4));
                if (chunkSize < 4096 || chunkSize > MaxAllowedChunkSize)
                    throw new CryptographicException("Kích thước khối trong tệp mã hóa không hợp lệ.");

                byte[] baseNonce = header.AsSpan(12, NonceSizeBytes).ToArray();
                byte[] recordHeader = new byte[RecordHeaderSize];
                byte[] cipherBuffer = new byte[chunkSize];
                byte[] plaintextBuffer = new byte[chunkSize];
                byte[] tag = new byte[TagSizeBytes];
                byte[] nonce = new byte[NonceSizeBytes];
                byte[] aad = new byte[FileHeaderSize + 9];
                header.CopyTo(aad, 0);

                using var aes = new AesGcm(key, TagSizeBytes);
                using (var output = new FileStream(
                           temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None,
                           DefaultChunkSize, FileOptions.SequentialScan))
                {
                    ulong chunkIndex = 0;
                    while (true)
                    {
                        if (chunkIndex > uint.MaxValue)
                            throw new CryptographicException("Tệp có quá nhiều khối để giải mã bằng định dạng này.");

                        int recordBytes = input.Read(recordHeader, 0, recordHeader.Length);
                        if (recordBytes == 0)
                            throw new CryptographicException("Tệp mã hóa bị cắt cụt: không tìm thấy khối cuối cùng đã xác thực.");
                        if (recordBytes < recordHeader.Length)
                            ReadExactly(input, recordHeader.AsSpan(recordBytes));

                        int count = BinaryPrimitives.ReadInt32LittleEndian(recordHeader.AsSpan(0, 4));
                        byte flags = recordHeader[4];
                        if (count < 0 || count > chunkSize)
                            throw new CryptographicException("Độ dài một khối trong tệp mã hóa không hợp lệ.");
                        if (flags != 0 && flags != FinalChunkFlag)
                            throw new CryptographicException("Cờ định dạng khối trong tệp mã hóa không hợp lệ.");

                        bool isFinal = flags == FinalChunkFlag;
                        if (count == 0 && !isFinal)
                            throw new CryptographicException("Chỉ khối cuối cùng mới được phép có độ dài bằng 0.");

                        ReadExactly(input, cipherBuffer.AsSpan(0, count));
                        ReadExactly(input, tag);

                        BinaryPrimitives.WriteUInt32BigEndian(aad.AsSpan(FileHeaderSize, 4), (uint)chunkIndex);
                        BinaryPrimitives.WriteInt32LittleEndian(aad.AsSpan(FileHeaderSize + 4, 4), count);
                        aad[FileHeaderSize + 8] = flags;
                        CreateNonce(baseNonce, (uint)chunkIndex, nonce);

                        try
                        {
                            aes.Decrypt(
                                nonce,
                                cipherBuffer.AsSpan(0, count),
                                tag,
                                plaintextBuffer.AsSpan(0, count),
                                aad);
                        }
                        catch (AuthenticationTagMismatchException ex)
                        {
                            throw new CryptographicException(
                                $"Xác thực khối dữ liệu số {chunkIndex + 1} thất bại. Tệp có thể đã bị sửa hoặc khóa không đúng.", ex);
                        }

                        if (count > 0)
                            output.Write(plaintextBuffer, 0, count);
                        decryptedLength += count;
                        chunkIndex++;

                        if (isFinal)
                        {
                            if (input.ReadByte() != -1)
                                throw new CryptographicException("Tệp mã hóa có dữ liệu thừa sau khối cuối cùng.");
                            break;
                        }
                    }

                    output.Flush(flushToDisk: true);
                }

                CryptographicOperations.ZeroMemory(baseNonce);
                CryptographicOperations.ZeroMemory(cipherBuffer);
                CryptographicOperations.ZeroMemory(plaintextBuffer);
                CryptographicOperations.ZeroMemory(tag);
                CryptographicOperations.ZeroMemory(nonce);
                CryptographicOperations.ZeroMemory(aad);
            }

            File.Move(temporaryPath, outputPath, overwrite: true);
            return new FileCryptResult(encryptedLength, decryptedLength);
        }
        finally
        {
            // Nếu xác thực thất bại giữa chừng, không để lại tệp giải mã chưa hoàn chỉnh.
            TryDelete(temporaryPath);
        }
    }

    public static FileComparisonResult CompareFiles(string firstPath, string secondPath)
    {
        if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
            throw new ArgumentException("Cần chọn đủ hai tệp để so sánh.");

        long firstLength = new FileInfo(firstPath).Length;
        long secondLength = new FileInfo(secondPath).Length;
        const int compareBufferSize = 64 * 1024;
        byte[] firstBuffer = new byte[compareBufferSize];
        byte[] secondBuffer = new byte[compareBufferSize];
        long offset = 0;

        using var first = new FileStream(firstPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            compareBufferSize, FileOptions.SequentialScan);
        using var second = new FileStream(secondPath, FileMode.Open, FileAccess.Read, FileShare.Read,
            compareBufferSize, FileOptions.SequentialScan);

        while (true)
        {
            int firstCount = ReadChunk(first, firstBuffer);
            int secondCount = ReadChunk(second, secondBuffer);
            int commonCount = Math.Min(firstCount, secondCount);

            for (int i = 0; i < commonCount; i++)
            {
                if (firstBuffer[i] != secondBuffer[i])
                {
                    return new FileComparisonResult(
                        false, offset + i + 1, offset + i, firstLength, secondLength);
                }
            }

            if (firstCount != secondCount)
            {
                return new FileComparisonResult(
                    false, offset + commonCount, offset + commonCount, firstLength, secondLength);
            }

            if (firstCount == 0)
                return new FileComparisonResult(true, offset, null, firstLength, secondLength);

            offset += firstCount;
        }
    }

    private static void ValidateKey(byte[] key)
    {
        if (key is null || key.Length != KeySizeBytes)
            throw new ArgumentException("Cần khóa AES-256 hợp lệ gồm đúng 32 byte. Hãy sinh khóa hoặc import tệp khóa.", nameof(key));
    }

    private static void ValidatePaths(string inputPath, string outputPath)
    {
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            throw new FileNotFoundException("Không tìm thấy tệp nguồn.", inputPath);
        if (string.IsNullOrWhiteSpace(outputPath))
            throw new ArgumentException("Đường dẫn tệp đầu ra không được để trống.", nameof(outputPath));

        string fullInput = Path.GetFullPath(inputPath);
        string fullOutput = Path.GetFullPath(outputPath);
        if (string.Equals(fullInput, fullOutput, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Tệp nguồn và tệp đầu ra phải khác nhau; thao tác này không được phép ghi đè tệp nguồn.");
    }

    private static string CreateTemporaryPathBeside(string outputPath)
    {
        string fullOutputPath = Path.GetFullPath(outputPath);
        string? directory = Path.GetDirectoryName(fullOutputPath);
        if (string.IsNullOrWhiteSpace(directory) || !Directory.Exists(directory))
            throw new DirectoryNotFoundException("Thư mục lưu tệp đầu ra không tồn tại.");

        string temporaryName = $".{Path.GetFileName(fullOutputPath)}.{Guid.NewGuid():N}.tmp";
        return Path.Combine(directory, temporaryName);
    }

    private static int ReadChunk(Stream stream, byte[] buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = stream.Read(buffer, total, buffer.Length - total);
            if (read == 0)
                break;
            total += read;
        }
        return total;
    }

    private static void ReadExactly(Stream stream, Span<byte> buffer)
    {
        int total = 0;
        while (total < buffer.Length)
        {
            int read = stream.Read(buffer[total..]);
            if (read == 0)
                throw new EndOfStreamException("Tệp kết thúc đột ngột hoặc bị cắt cụt.");
            total += read;
        }
    }

    /// <summary>
    /// Tạo nonce cho khối bằng cách cộng chỉ số khối vào nonce gốc 96 bit.
    /// Việc dùng chỉ số khối giúp nonce khác nhau trong cùng một tệp.
    /// </summary>
    private static void CreateNonce(ReadOnlySpan<byte> baseNonce, uint counter, Span<byte> destination)
    {
        if (baseNonce.Length != NonceSizeBytes || destination.Length != NonceSizeBytes)
            throw new ArgumentException("Kích thước nonce không hợp lệ.");

        baseNonce.CopyTo(destination);
        ulong carry = counter;
        for (int i = destination.Length - 1; i >= 0 && carry > 0; i--)
        {
            ulong sum = destination[i] + (carry & 0xFFUL);
            destination[i] = (byte)(sum & 0xFF);
            carry = (carry >> 8) + (sum >> 8);
        }

        if (carry != 0)
            throw new CryptographicException("Bộ đếm nonce bị tràn.");
    }

    private static void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Không che khuất lỗi gốc chỉ vì không xóa được tệp tạm.
        }
    }
}
