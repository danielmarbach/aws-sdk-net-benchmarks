using System.Text;
using AWSSDK.Core.NetStandard.Amazon.Runtime.Internal.Util;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class CanonicalHeaderNames
{
    private IDictionary<string,string> sortedHeaders;

    [Params(0, 1, 2, 5, 10)]
    public int NumberOfString { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        sortedHeaders = ValuesGenerator.ArrayOfStrings(NumberOfString, 3, 10).OrderBy(x => x).ToDictionary(x => x, x => x);
    }

    [Benchmark(Baseline = true)]
    public string Before() => CanonicalizeHeaderNames(sortedHeaders);

    [Benchmark]
    public string Join() => string.Join(';', sortedHeaders.Select(x => x.Key.ToLowerInvariant()));

    [Benchmark]
    public string ValueStringBuilder() => CanonicalizeHeaderNamesValueStringBuilder(sortedHeaders);

    static string CanonicalizeHeaderNames(IEnumerable<KeyValuePair<string, string>> sortedHeaders)
    {
        var builder = new StringBuilder();

        foreach (var header in sortedHeaders)
        {
            if (builder.Length > 0)
                builder.Append(";");
            builder.Append(header.Key.ToLowerInvariant());
        }

        return builder.ToString();
    }

    static string CanonicalizeHeaderNamesValueStringBuilder(IEnumerable<KeyValuePair<string, string>> sortedHeaders)
    {
        using var builder = new ValueStringBuilder(512);

        foreach (var header in sortedHeaders)
        {
            if (builder.Length > 0)
                builder.Append(";");
            builder.Append(header.Key.ToLowerInvariant());
        }

        return builder.ToString();
    }
}