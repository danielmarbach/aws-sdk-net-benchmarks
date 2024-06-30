using System.Globalization;
using System.Text;
using Amazon.Runtime;
using Amazon.Util;
using AWSSDK.Core.NetStandard.Amazon.Runtime.Internal.Util;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Extensions;

namespace aws_sdk_net_benchmarks;

[SimpleJob]
[MemoryDiagnoser]
public class ParameterAsString
{
    private ParameterCollection parameterCollection;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var sortedHeaders = ValuesGenerator.ArrayOfStrings(10, 3, 10).OrderBy(x => x).ToDictionary(x => x, x => x);
        parameterCollection = new ParameterCollection();
        foreach (var header in sortedHeaders)
        {
            parameterCollection.Add(header.Key, header.Value);
        }
    }

    [Benchmark(Baseline = true)]
    public string Before() => GetParametersAsStringBefore(parameterCollection);

    [Benchmark]
    public string After() => GetParametersAsStringAfter(parameterCollection);

    internal static string GetParametersAsStringBefore(ParameterCollection parameterCollection)
    {
        var sortedParameters = parameterCollection.GetSortedParametersList();

        StringBuilder data = new StringBuilder(512);
        foreach (var kvp in sortedParameters)
        {
            var key = kvp.Key;
            var value = kvp.Value;
            if (value != null)
            {
                data.Append(key);
                data.Append('=');
                data.Append(AWSSDKUtils.UrlEncode(value, false));
                data.Append('&');
            }
        }
        string result = data.ToString();
        if (result.Length == 0)
            return string.Empty;

        return result.Remove(result.Length - 1);
    }

    internal static string GetParametersAsStringAfter(ParameterCollection parameterCollection)
    {
        var parameterBuilder = new ValueStringBuilder(512);
        foreach (var kvp in parameterCollection.GetParametersEnumerable())
        {
            var value = kvp.Value;
            if (value == null)
                continue;
            parameterBuilder.Append(kvp.Key);
            parameterBuilder.Append('=');
            parameterBuilder.Append(AWSSDKUtils.UrlEncode(value, false));
            parameterBuilder.Append('&');
        }

        var length = parameterBuilder.Length;
        return length == 0 ? string.Empty : parameterBuilder.ToString(0, length - 1);
    }

        /// <summary>
    /// Collection of parameters that an SDK client will send to a service.
    /// </summary>
    public class ParameterCollection : SortedDictionary<string, ParameterValue>
    {
        /// <summary>
        /// Constructs empty ParameterCollection.
        /// </summary>
        public ParameterCollection()
            : base(comparer: StringComparer.Ordinal) { }

        /// <summary>
        /// Adds a parameter with a string value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public void Add(string key, string value)
        {
            Add(key, new StringParameterValue(value));
        }

        /// <summary>
        /// Adds a parameter with a list-of-strings value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public void Add(string key, List<string> values)
        {
            Add(key, new StringListParameterValue(values));
        }

        /// <summary>
        /// Adds a parameter with a list-of-doubles value.
        /// </summary>
        /// <param name="key"></param>
        /// <param name="values"></param>
        public void Add(string key, List<double> values)
        {
            Add(key, new DoubleListParameterValue(values));
        }

        /// <summary>
        /// Converts the current parameters into a list of key-value pairs.
        /// </summary>
        /// <returns></returns>
        public List<KeyValuePair<string,string>> GetSortedParametersList()
        {
            return GetParametersEnumerable().ToList();
        }

        internal IEnumerable<KeyValuePair<string, string>> GetParametersEnumerable()
        {
            foreach (var kvp in this)
            {
                var name = kvp.Key;
                var value = kvp.Value;

                switch (value)
                {
                    case StringParameterValue stringParameterValue:
                        yield return new KeyValuePair<string, string>(name, stringParameterValue.Value);
                        break;
                    case StringListParameterValue stringListParameterValue:
                        var sortedStringListParameterValue = stringListParameterValue.Value;
                        sortedStringListParameterValue.Sort(StringComparer.Ordinal);
                        foreach (var listValue in sortedStringListParameterValue)
                            yield return new KeyValuePair<string, string>(name, listValue);
                        break;
                    case DoubleListParameterValue doubleListParameterValue:
                        var sortedDoubleListParameterValue = doubleListParameterValue.Value;
                        sortedDoubleListParameterValue.Sort();
                        foreach (var listValue in sortedDoubleListParameterValue)
                            yield return new KeyValuePair<string, string>(name, listValue.ToString(CultureInfo.InvariantCulture));
                        break;
                    default:
                        throw new AmazonClientException("Unsupported parameter value type '" + value.GetType().FullName + "'");
                }
            }
        }
    }
}