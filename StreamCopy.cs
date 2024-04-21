using System.Text;
using BenchmarkDotNet.Attributes;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class StreamCopy
{
    private MemoryStream stream;

    [GlobalSetup]
    public void GlobalSetup()
    {
        stream = new MemoryStream("Hello world Hello world Hello world Hello world Hello world Hello world"u8.ToArray());
    }

    [Benchmark(Baseline = true)]
    public MemoryStream Sdk()
    {
        var destination = new MemoryStream();
        CopyStreamSDk(stream, destination, 8192);
        return destination;
    }
    
    [Benchmark]
    public MemoryStream Net()
    {
        var destination = new MemoryStream();
        CopyStream(stream, destination, 8192);
        return destination;
    }

    private static void CopyStreamSDk(Stream source, Stream destination, int bufferSize)
    {
        if (source == null)
            throw new ArgumentNullException("source");
        if (destination == null)
            throw new ArgumentNullException("destination");
        if (bufferSize <= 0)
            throw new ArgumentOutOfRangeException("bufferSize");

        byte[] array = new byte[bufferSize];
        int count;
        while ((count = source.Read(array, 0, array.Length)) != 0)
        {
            destination.Write(array, 0, count);
        }
    }

    private static void CopyStream(Stream source, Stream destination, int bufferSize)
    {
        source.CopyTo(destination, bufferSize);
    }
}