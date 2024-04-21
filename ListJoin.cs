using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class ListJoin
{
    private List<string> stringValues;

    [Params(1, 2, 10)] 
    public int NumberOfString { get; set; }

    [GlobalSetup]
    public void GlobalSetup()
    {
        stringValues = ValuesGenerator.ArrayOfStrings(NumberOfString, 3, 10).ToList();
    }

    [Benchmark(Baseline = true)]
    public string JoinSdk() => Join(stringValues);
    
    [Benchmark()]
    public string Join() => string.Join(", ", stringValues);

    private static String Join(List<String> strings)
    {
        StringBuilder result = new StringBuilder();
            
        Boolean first = true;
        foreach (String s in strings)
        {
            if (!first) result.Append(", ");

            result.Append(s);
            first = false;
        }

        return result.ToString();
    }
}