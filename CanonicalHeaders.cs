#nullable disable

using System.Text;
using Amazon.Util;
using AWSSDK.Core.NetStandard.Amazon.Runtime.Internal.Util;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace aws_sdk_net_benchmarks;

[ShortRunJob]
[MemoryDiagnoser]
public class CanonicalHeaders
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
    public string Before() => CanonicalizeHeaders(sortedHeaders);

    [Benchmark]
    public string ValueStringBuilder() => CanonicalizeHeadersValueStringBuilder(sortedHeaders);

    [Benchmark]
    public string ValueStringBuilderCompressSpace() => CanonicalizeHeadersValueStringBuilderAndCompressSpaces(sortedHeaders);

    protected internal static string CanonicalizeHeaders(IEnumerable<KeyValuePair<string, string>> sortedHeaders)
    {
        if (sortedHeaders == null || sortedHeaders.Count() == 0)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var entry in sortedHeaders)
        {
            // Refer https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html. (Step #4: "To create the canonical headers list, convert all header names to lowercase and remove leading spaces and trailing spaces. Convert sequential spaces in the header value to a single space.").
            builder.Append(entry.Key.ToLowerInvariant());
            builder.Append(":");
            builder.Append(AWSSDKUtils.CompressSpaces(entry.Value)?.Trim());
            builder.Append("\n");
        }
        return builder.ToString();
    }

    protected internal static string CanonicalizeHeadersValueStringBuilder(IEnumerable<KeyValuePair<string, string>> sortedHeaders)
    {
        if (sortedHeaders == null)
            return string.Empty;

        // Majority of the cases we will always have a IDictionary<string, string> for headers which implements ICollection<KeyValuePair<string, string>>.
        var materializedSortedHeaders = sortedHeaders as ICollection<KeyValuePair<string, string>> ?? sortedHeaders.ToList();
        if (materializedSortedHeaders.Count == 0)
            return string.Empty;

        using var builder = new ValueStringBuilder(512);

        foreach (var entry in materializedSortedHeaders)
        {
            // Refer https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html. (Step #4: "To create the canonical headers list, convert all header names to lowercase and remove leading spaces and trailing spaces. Convert sequential spaces in the header value to a single space.").
            builder.Append(entry.Key.ToLowerInvariant());
            builder.Append(':');
            builder.Append(AWSSDKUtils.CompressSpaces(entry.Value)?.Trim());
            builder.Append("\n");
        }
        return builder.ToString();
    }

    protected internal static string CanonicalizeHeadersValueStringBuilderAndCompressSpaces(IEnumerable<KeyValuePair<string, string>> sortedHeaders)
    {
        if (sortedHeaders == null)
            return string.Empty;

        // Majority of the cases we will always have a IDictionary<string, string> for headers which implements ICollection<KeyValuePair<string, string>>.
        var materializedSortedHeaders = sortedHeaders as ICollection<KeyValuePair<string, string>> ?? sortedHeaders.ToList();
        if (materializedSortedHeaders.Count == 0)
            return string.Empty;

        using var builder = new ValueStringBuilder(512);

        foreach (var entry in materializedSortedHeaders)
        {
            // Refer https://docs.aws.amazon.com/general/latest/gr/sigv4-create-canonical-request.html. (Step #4: "To create the canonical headers list, convert all header names to lowercase and remove leading spaces and trailing spaces. Convert sequential spaces in the header value to a single space.").
            builder.Append(entry.Key.ToLowerInvariant());
            builder.Append(':');
            builder.Append(CompressSpace.CompressSpaces(entry.Value)?.Trim());
            builder.Append("\n");
        }
        return builder.ToString();
    }
}