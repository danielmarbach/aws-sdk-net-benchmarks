using System.Buffers;
using System.Runtime.CompilerServices;
using System.Text;
using Amazon.Util;
using BenchmarkDotNet.Attributes;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class UrlEncode
{
    [Params("Hello, World!", "Hello, World! Hello, World! Hello, World! Hello, World!", "Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World! Hello, World!")]
    public string StringToEncode { get; set; }
    
    [Params(true, false)]
    public bool Path { get; set; }
    
    [Benchmark(Baseline = true)]
    public string Before() => AWSSDKUtils.UrlEncode(3986, StringToEncode, Path);
    
    [Benchmark()]
    public string After() => After(3986, StringToEncode, Path);
    
    /// <summary>
    /// The Set of accepted and valid Url characters per RFC3986. 
    /// Characters outside of this set will be encoded.
    /// </summary>
    public const string ValidUrlCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.~";

    /// <summary>
    /// The Set of accepted and valid Url characters per RFC1738. 
    /// Characters outside of this set will be encoded.
    /// </summary>
    public const string ValidUrlCharactersRFC1738 = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-_.";

    /// <summary>
    /// The set of accepted and valid Url path characters per RFC3986.
    /// </summary>
    private static string ValidPathCharacters = DetermineValidPathCharacters();
    
    private static string DetermineValidPathCharacters()
    {
        const string basePathCharacters = "/:'()!*[]$";

        var sb = new StringBuilder();
        foreach (var c in basePathCharacters)
        {
            var escaped = Uri.EscapeUriString(c.ToString());
            if (escaped.Length == 1 && escaped[0] == c)
                sb.Append(c);
        }
        return sb.ToString();
    }

    internal static bool TryGetRFCEncodingSchemes(int rfcNumber, out string? encodingScheme)
    {
        if (rfcNumber == 3986)
        {
            encodingScheme = ValidUrlCharacters;
            return true;
        }

        if (rfcNumber == 1738)
        {
            encodingScheme = ValidUrlCharactersRFC1738;
            return true;
        }

        encodingScheme = null;
        return false;
    }

    [SkipLocalsInit]
    public static string After(int rfcNumber, string data, bool path)
    {
        byte[] sharedDataBuffer = null;
        // Put this elsewhere?
        const int MaxStackLimit = 256;
        try
        {
            string validUrlCharacters;
            if (!TryGetRFCEncodingSchemes(rfcNumber, out validUrlCharacters))
                validUrlCharacters = ValidUrlCharacters;

            var unreservedChars = string.Concat(validUrlCharacters, path ? ValidPathCharacters : "");

            var dataAsSpan = data.AsSpan();
            var encoding = Encoding.UTF8;

            var dataByteLength = encoding.GetMaxByteCount(dataAsSpan.Length);
            var encodedByteLength = 2 * dataByteLength;
            var dataBuffer = encodedByteLength <= MaxStackLimit
                ? stackalloc byte[MaxStackLimit]
                : sharedDataBuffer = ArrayPool<byte>.Shared.Rent(encodedByteLength);
            var encodingBuffer = dataBuffer.Slice(dataBuffer.Length - dataByteLength);
            var bytesWritten = encoding.GetBytes(dataAsSpan, encodingBuffer);
            
            var index = 0;
            foreach (var symbol in encodingBuffer.Slice(0, bytesWritten))
                if (unreservedChars.IndexOf((char)symbol) != -1)
                {
                    dataBuffer[index++] = symbol;
                }
                else
                {
                    dataBuffer[index++] = (byte)'%';

                    // Break apart the byte into two four-bit components and
                    // then convert each into their hexadecimal equivalent.
                    var hiNibble = symbol >> 4;
                    var loNibble = symbol & 0xF;
                    dataBuffer[index++] = (byte)ToUpperHex(hiNibble);
                    dataBuffer[index++] = (byte)ToUpperHex(loNibble);
                }

            return encoding.GetString(dataBuffer.Slice(0, index));
        }
        finally
        {
            if (sharedDataBuffer != null) ArrayPool<byte>.Shared.Return(sharedDataBuffer);
        }
    }

    private static char ToUpperHex(int value)
    {
        // Maps 0-9 to the Unicode range of '0' - '9' (0x30 - 0x39).
        if (value <= 9) return (char)(value + '0');
        // Maps 10-15 to the Unicode range of 'A' - 'F' (0x41 - 0x46).
        return (char)(value - 10 + 'A');
    }
}