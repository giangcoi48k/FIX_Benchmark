using System.Collections.Generic;
using System.Globalization;

namespace FIX_Benchmark
{
    public class FixDictionaryBase_3
    {
        private readonly Dictionary<int, string> _dict;
        private static readonly CultureInfo _culture = CultureInfo.CreateSpecificCulture("en-US");

        protected FixDictionaryBase_3()
        {
            _dict = new Dictionary<int, string>();
        }

        public string GetString(int tag) => _dict.TryGetValue(tag, out var value) ? value : null;

        public int GetInt(int tag) => TryParseInt(GetTag(tag));

        public int? GetNullableInt(int tag) => TryParseInt(_dict.GetValueOrDefault(tag), out var value) ? value : null;

        public long GetLong(int tag) => TryParseLong(GetTag(tag));

        public long? GetNullableLong(int tag) => TryParseLong(_dict.GetValueOrDefault(tag), out var value) ? value : null;

        public decimal GetDecimal(int tag) => TryParseDecimal(GetTag(tag));

        public decimal? GetNullableDecimal(int tag) => TryParseDecimal(_dict.GetValueOrDefault(tag), out var value) ? value : null;

        public double GetDouble(int tag) => TryParseDouble(GetTag(tag));

        public double? GetNullableDouble(int tag) => TryParseDouble(_dict.GetValueOrDefault(tag), out var value) ? value : null;

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

        private string GetTag(int tag) => _dict.TryGetValue(tag, out var value) ? value : null;

        // Các phương thức TryParse để giảm chi phí và không cần chuyển đổi nhiều lần
        private static int TryParseInt(string value) => int.TryParse(value, out var result) ? result : throw new InvalidCastException("Invalid integer format");
        private static bool TryParseInt(string value, out int result) => int.TryParse(value, out result);

        private static long TryParseLong(string value) => long.TryParse(value, out var result) ? result : throw new InvalidCastException("Invalid long format");
        private static bool TryParseLong(string value, out long result) => long.TryParse(value, out result);

        private static decimal TryParseDecimal(string value) => decimal.TryParse(value, NumberStyles.Any, _culture, out var result) ? result : throw new InvalidCastException("Invalid decimal format");
        private static bool TryParseDecimal(string value, out decimal result) => decimal.TryParse(value, NumberStyles.Any, _culture, out result);

        private static double TryParseDouble(string value) => double.TryParse(value, NumberStyles.Any, _culture, out var result) ? result : throw new InvalidCastException("Invalid double format");
        private static bool TryParseDouble(string value, out double result) => double.TryParse(value, NumberStyles.Any, _culture, out result);

        // Tối ưu Parse bằng cách giảm việc tạo đối tượng mới không cần thiết
        protected void Parse(ReadOnlySpan<char> inputSpan, out List<FixDictionaryBase_3> groups)
        {
            groups = new List<FixDictionaryBase_3>();
            FixDictionaryBase_3 currentGroup = null;

            const char splitter = '';
            const char equalChar = '=';

            int startIndex = 0;

            while (inputSpan.Length > 0)
            {
                var splitterIndex = inputSpan.IndexOf(splitter);
                if (splitterIndex == -1) break;

                var leftPart = inputSpan.Slice(0, splitterIndex);
                var equalIndex = leftPart.IndexOf(equalChar);
                if (equalIndex == -1) continue;

                var key = int.Parse(leftPart.Slice(0, equalIndex));

                var value = leftPart.Slice(equalIndex + 1).ToString();

                if (key == 83)
                {
                    currentGroup = new FixDictionaryBase_3();
                    groups.Add(currentGroup);
                }

                var isChecksum = key == 10;
                var currentDict = isChecksum ? _dict : currentGroup != null ? currentGroup._dict : _dict;
                currentDict[key] = value;

                inputSpan = inputSpan.Slice(splitterIndex + 1);
            }
        }
    }

    public sealed class FixDictionary_3 : FixDictionaryBase
    {
        private readonly string _fixString;

        public FixDictionary_3(string fixString) : base()
        {
            _fixString = fixString;
            Parse(fixString.AsSpan(), out var groups);
            Groups = groups;
        }

        public IReadOnlyList<FixDictionaryBase> Groups { get; }

        public string GetFixString() => _fixString;
    }
}
