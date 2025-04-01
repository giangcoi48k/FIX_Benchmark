using System.Collections.Immutable;
using System.Globalization;

namespace FIX_Benchmark
{
    public class FixDictionaryBase
    {
        private readonly Dictionary<int, string> _dict;
        private static readonly CultureInfo _culture = CultureInfo.CreateSpecificCulture("en-US");

        protected FixDictionaryBase()
        {
            _dict = [];
        }

        public string GetString(int tag) => GetTag(tag);

        public int GetInt(int tag) => Convert.ToInt32(GetTag(tag), _culture);

        public int? GetNullableInt(int tag) => _dict.TryGetValue(tag, out var value) ? Convert.ToInt32(value, _culture) : null;

        public long GetLong(int tag) => Convert.ToInt64(GetTag(tag), _culture);

        public long? GetNullableLong(int tag) => _dict.TryGetValue(tag, out var value) ? Convert.ToInt64(value, _culture) : null;

        public decimal GetDecimal(int tag) => Convert.ToDecimal(GetTag(tag), _culture);

        public decimal? GetNullableDecimal(int tag) => _dict.TryGetValue(tag, out var value) ? Convert.ToDecimal(value, _culture) : null;

        public double GetDouble(int tag) => Convert.ToDouble(GetTag(tag), _culture);

        public double? GetNullableDouble(int tag) => _dict.TryGetValue(tag, out var value) ? Convert.ToDouble(value, _culture) : null;

        public bool GetBoolean(int tag) => GetTag(tag) switch
        {
            "Y" => true,
            "N" => false,
            _ => throw new InvalidCastException("Cannot convert string to boolean")
        };

        public bool? GetNullableBoolean(int tag)
        {
            if (_dict.TryGetValue(tag, out var value))
            {
                return value switch
                {
                    "Y" => true,
                    "N" => false,
                    _ => throw new InvalidCastException("Cannot convert string to boolean")
                };
            }
            return null;
        }

        private string GetTag(int tag) => _dict.GetValueOrDefault(tag);

        protected void Parse(ReadOnlySpan<char> inputSpan, out List<FixDictionaryBase> groups)
        {
            // Algorithm for processing FIX message string:
            // 1. Iterate through the input string and extract key-value pairs based on the splitter character.
            // 2. If the key is RptSeq (83), initialize a new group and add it to the groups list.
            // 3. Assign key-value pairs to the appropriate dictionary:
            //    - If the key is 10 (Checksum), store it in the main _dict.
            //    - If currently inside a group, store it in the dictionary of the current group.
            //    - Otherwise, store it in the main _dict.
            // 4. Continue processing until no more splitter characters are found in the input string.

            groups = [];

            FixDictionaryBase currentGroup = null;

            // Special characters used to separate data
            const char splitter = '';
            const char equalChar = '=';
            const int rptSeq = 83;

            // Find the first occurrence of the splitter character
            int splitterIndex = inputSpan.IndexOf(splitter);
            var hasGroup = false;

            while (splitterIndex != -1)
            {
                // Extract the part before the splitter to get the key-value pair
                var leftPart = inputSpan[..splitterIndex];

                // Find the position of '=' to separate key and value
                var equalIndex = leftPart.IndexOf(equalChar);

                // Extract key from the part before '='
                var key = int.Parse(leftPart[..equalIndex]);

                // Extract value from the part after '='
                var value = leftPart[(equalIndex + 1)..].ToString();

                // If the key is RptSeq (83), start a new group and add it to the groups list
                if (key == rptSeq)
                {
                    hasGroup = true;
                    currentGroup = new();
                    groups.Add(currentGroup);
                }

                // Determine the appropriate dictionary to store data
                // - If the key is 10 (Checksum), always store it in the main _dict
                // - If a group has been identified (hasGroup == true), store it in the current group's dictionary
                // - Otherwise, store it in the main _dict
                var isChecksum = key == 10;
                var currentDict = isChecksum ? _dict : hasGroup ? currentGroup._dict : _dict;
                currentDict[key] = value;

                // Remove the processed part and continue searching for the next splitter
                inputSpan = inputSpan[(splitterIndex + 1)..];
                splitterIndex = inputSpan.IndexOf(splitter);
            }
        }
    }

    public sealed class FixDictionary : FixDictionaryBase
    {
        private readonly string _fixString;

        public FixDictionary(string fixString) : base()
        {
            _fixString = fixString;
            Parse(fixString, out var groups);
            Groups = groups;
        }

        public IReadOnlyList<FixDictionaryBase> Groups { get; }

        public string GetFixString() => _fixString;
    }
}
