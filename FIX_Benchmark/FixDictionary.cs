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
            // Thuật toán xử lý chuỗi FIX message:
            // 1. Duyệt qua chuỗi dữ liệu đầu vào, tách từng cặp key-value dựa trên ký tự splitter.
            // 2. Nếu key là RptSeq (83), khởi tạo một nhóm mới và thêm vào danh sách groups.
            // 3. Gán key-value vào dictionary phù hợp:
            //    - Nếu key là 10 (Checksum), lưu vào _dict chính.
            //    - Nếu đang trong một nhóm, lưu vào dictionary của nhóm hiện tại.
            //    - Nếu không, lưu vào _dict chính.
            // 4. Tiếp tục quá trình cho đến khi không còn ký tự splitter trong chuỗi đầu vào.

            groups = [];

            FixDictionaryBase currentGroup = null;

            // Các ký tự đặc biệt được sử dụng để tách dữ liệu
            const char splitter = '';
            const char equalChar = '=';
            const int rptSeq = 83;

            // Tìm vị trí đầu tiên của ký tự phân tách (splitter)
            int splitterIndex = inputSpan.IndexOf(splitter);
            var hasGroup = false;

            while (splitterIndex != -1)
            {
                // Cắt phần bên trái của splitter để lấy key-value
                var leftPart = inputSpan[..splitterIndex];

                // Tìm vị trí dấu '=' để tách key và value
                var equalIndex = leftPart.IndexOf(equalChar);

                // Lấy key từ phần trước dấu '='
                var key = int.Parse(leftPart[..equalIndex]);

                // Lấy value từ phần sau dấu '='
                var value = leftPart[(equalIndex + 1)..].ToString();

                // Nếu key là RptSeq (83), bắt đầu một nhóm mới và thêm vào danh sách groups
                if (key == rptSeq)
                {
                    hasGroup = true;
                    currentGroup = new();
                    groups.Add(currentGroup);
                }

                // Xác định dictionary thích hợp để lưu trữ dữ liệu
                // - Nếu key là 10 (Checksum), luôn lưu vào _dict chính
                // - Nếu đã xác định nhóm (hasGroup == true), lưu vào dictionary của nhóm hiện tại
                // - Nếu không, lưu vào _dict chính
                var isChecksum = key == 10;
                var currentDict = isChecksum ? _dict : hasGroup ? currentGroup._dict : _dict;
                currentDict[key] = value;

                // Loại bỏ phần đã xử lý và tiếp tục tìm kiếm splitter tiếp theo
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
