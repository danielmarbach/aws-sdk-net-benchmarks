using Amazon.Util;
using BenchmarkDotNet.Attributes;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class DetermineService
{
    [Params("queue.amazonaws.com", "https://sns.us-west-2.amazonaws.com", "https://s3.amazonaws.com", "https://s3-external-1.amazonaws.com", "", "notanurl")]
    public string Url { get; set; }

    [Benchmark(Baseline = true)]
    public string Before() => AWSSDKUtils.DetermineService(Url);

    [Benchmark]
    public string After() => DetermineServiceAfter(Url);

    public static string DetermineServiceAfter(string url)
    {
        var urlSpan = url.AsSpan();

        var doubleSlashIndex = urlSpan.IndexOf(DoubleSlash, StringComparison.Ordinal);
        if (doubleSlashIndex >= 0)
            urlSpan = urlSpan.Slice(doubleSlashIndex + 2);

        var dotIndex = urlSpan.IndexOf('.');

        if (dotIndex < 0)
            return string.Empty;

        var servicePartSpan = urlSpan.Slice(0, dotIndex);
        var hyphenIndex = servicePartSpan.IndexOf('-');
        if (hyphenIndex > 0)
        {
            servicePartSpan = servicePartSpan.Slice(0, hyphenIndex);
        }

        // Check for SQS : return "sqs" in case service is determined to be "queue" as per the URL.
        return servicePartSpan.Equals(Queue, StringComparison.OrdinalIgnoreCase) ? "sqs" : servicePartSpan.ToString();
    }

    // Compiler trick to directly refer to static data in the assembly
    private static ReadOnlySpan<char> DoubleSlash => new[] { '/', '/' };
    private static ReadOnlySpan<char> Queue => new[] { 'q', 'u', 'e', 'u', 'e' };
}