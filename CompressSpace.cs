#nullable disable

using Amazon.Util;
using AWSSDK.Core.NetStandard.Amazon.Runtime.Internal.Util;
using BenchmarkDotNet.Attributes;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class CompressSpace
{
    [Params("Hello, World!", "Hello,   World!", "Hello, World!    Hello, World!      Hello, World!")]
    public string StringToCompress { get; set; }

    [Benchmark(Baseline = true)]
    public string Before() => AWSSDKUtils.CompressSpaces(StringToCompress);

    [Benchmark]
    public string After() => CompressSpaces(StringToCompress);

    public static string CompressSpaces(string data)
    {
        if (data == null)
        {
            return null;
        }

        var dataLength = data.Length;
        if (dataLength == 0)
        {
            return string.Empty;
        }

        var stringBuilder = new ValueStringBuilder(dataLength);
        int index = 0;
        var isWhiteSpace = false;
        foreach (var character in data)
        {
            if (!isWhiteSpace | !(isWhiteSpace = char.IsWhiteSpace(character)))
            {
                stringBuilder.Append(isWhiteSpace ? ' ' : character);
                index++;
            }
        }
        return stringBuilder.ToString(0, index);
    }
}