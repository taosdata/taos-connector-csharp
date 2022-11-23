#if NETCOREAPP
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Converters;
using System.Collections.Immutable;

namespace IoTSharp.Data.Taos
{

    public partial class RecordSettings
    {
        private readonly SortedDictionary<string, string> _defaultTags = new SortedDictionary<string, string>(StringComparer.Ordinal);

        private static Regex EnvVariableRegex = new Regex("^(\\${env.)(?<Value>.+)(})$", RegexOptions.ExplicitCapture | RegexOptions.Compiled | RegexOptions.RightToLeft | RegexOptions.CultureInvariant);


        public RecordSettings AddDefaultTag(string key, string expression)
        {
            Arguments.CheckNotNull(key, "tagName");
            _defaultTags[key] = expression;
            return this;
        }

        internal IDictionary<string, string> GetDefaultTags()
        {
            if (_defaultTags.Count == 0)
            {
                return ImmutableDictionary<string, string>.Empty;
            }

            return (from it in _defaultTags
                    select new KeyValuePair<string, string>(it.Key, Evaluation(it.Value)) into it
                    where !string.IsNullOrEmpty(it.Value)
                    select it).ToDictionary((it) => it.Key, (it) => it.Value, StringComparer.Ordinal);
            static string Evaluation(string expression)
            {
                if (string.IsNullOrEmpty(expression))
                {
                    return null;
                }

                Match match = EnvVariableRegex.Match(expression);
                if (match.Success)
                {
                    return Environment.GetEnvironmentVariable(match.Groups["Value"].Value);
                }


                return expression;
            }
        }


    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TimePrecision
    {
        [EnumMember(Value = "ms")]
        Ms = 1,
        [EnumMember(Value = "s")]
        S,
        [EnumMember(Value = "us")]
        Us,
        [EnumMember(Value = "ns")]
        Ns
    }
    public class RecordData : IEquatable<RecordData>
    {
        public sealed class Builder
        {
            private readonly string _tableName;

            private readonly Dictionary<string, string> _tags = new Dictionary<string, string>();

            private readonly Dictionary<string, object> _fields = new Dictionary<string, object>();

            private TimePrecision _precision;

            private BigInteger? _time;

            private Builder(string tableName)
            {
                Arguments.CheckNonEmptyString(tableName, "table name");
                _tableName = tableName;
                _precision = TimePrecision.Ns;
            }

            public static Builder table(string tableName)
            {
                return new Builder(tableName);
            }

            public Builder Tag(string name, string value)
            {
                if (string.IsNullOrEmpty(value))
                {
                    if (_tags.ContainsKey(name))
                    {
                        Trace.TraceWarning("Empty tags will cause deletion of, tag [" + name + "], table [" + _tableName + "]");
                        _tags.Remove(name);
                    }
                    else
                    {
                        Trace.TraceWarning("Empty tags has no effect, tag [" + name + "], table [" + _tableName + "]");
                    }
                }
                else
                {
                    _tags[name] = value;
                }

                return this;
            }

            public Builder Field(string name, byte value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, float value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, double value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, decimal value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, long value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, ulong value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, uint value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, string value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, bool value)
            {
                return PutField(name, value);
            }

            public Builder Field(string name, object value)
            {
                return PutField(name, value);
            }

            public Builder Timestamp(long timestamp, TimePrecision timeUnit)
            {
                _precision = timeUnit;
                _time = timestamp;
                return this;
            }

            public Builder Timestamp(TimeSpan timestamp, TimePrecision timeUnit)
            {
                _time = TimeSpanToBigInteger(timestamp, timeUnit);
                _precision = timeUnit;
                return this;
            }

            public Builder Timestamp(DateTime timestamp, TimePrecision timeUnit)
            {
                TimeSpan timestamp2 = timestamp.Subtract(EpochStart);
                return Timestamp(timestamp2, timeUnit);
            }

            public Builder Timestamp(DateTimeOffset timestamp, TimePrecision timeUnit)
            {
                return Timestamp(timestamp.UtcDateTime, timeUnit);
            }


            public bool HasFields()
            {
                return _fields.Count > 0;
            }

            public RecordData ToPointData()
            {
                return new RecordData(_tableName, _precision, _time, ImmutableSortedDictionary.CreateRange(_tags), ImmutableSortedDictionary.CreateRange(_fields));
            }

            private Builder PutField(string name, object value)
            {
                Arguments.CheckNonEmptyString(name, "Field name");
                _fields[name] = value;
                return this;
            }
        }

        private static readonly DateTime EpochStart = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        private readonly string _tableName;

        private readonly ImmutableSortedDictionary<string, string> _tags = ImmutableSortedDictionary<string, string>.Empty;

        private readonly ImmutableSortedDictionary<string, object> _fields = ImmutableSortedDictionary<string, object>.Empty;

        public readonly TimePrecision Precision;

        private readonly BigInteger? _time;

        private RecordData(string tableName)
        {
            Arguments.CheckNonEmptyString(tableName, "table name");
            _tableName = tableName;
            Precision = TimePrecision.Ns;
        }

        public static RecordData table(string tableName)
        {
            return new RecordData(tableName);
        }

        private RecordData(string tableName, TimePrecision precision, BigInteger? time, ImmutableSortedDictionary<string, string> tags, ImmutableSortedDictionary<string, object> fields)
        {
            _tableName = tableName;
            Precision = precision;
            _time = time;
            _tags = tags;
            _fields = fields;
        }

        public RecordData Tag(string name, string value)
        {
            bool flag = string.IsNullOrEmpty(value);
            ImmutableSortedDictionary<string, string> immutableSortedDictionary = _tags;
            if (flag)
            {
                if (!immutableSortedDictionary.ContainsKey(name))
                {
                    Trace.TraceWarning("Empty tags has no effect, tag [" + name + "], table [" + _tableName + "]");
                    return this;
                }

                Trace.TraceWarning("Empty tags will cause deletion of, tag [" + name + "], table [" + _tableName + "]");
            }

            if (immutableSortedDictionary.ContainsKey(name))
            {
                immutableSortedDictionary = immutableSortedDictionary.Remove(name);
            }

            if (!flag)
            {
                immutableSortedDictionary = immutableSortedDictionary.Add(name, value);
            }

            return new RecordData(_tableName, Precision, _time, immutableSortedDictionary, _fields);
        }

        public RecordData Field(string name, byte value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, float value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, double value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, decimal value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, long value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, ulong value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, uint value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, string value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, bool value)
        {
            return PutField(name, value);
        }

        public RecordData Field(string name, object value)
        {
            return PutField(name, value);
        }

        public RecordData Timestamp(long timestamp, TimePrecision timeUnit)
        {
            return new RecordData(_tableName, timeUnit, timestamp, _tags, _fields);
        }

        public RecordData Timestamp(TimeSpan timestamp, TimePrecision timeUnit)
        {
            BigInteger value = TimeSpanToBigInteger(timestamp, timeUnit);
            return new RecordData(_tableName, timeUnit, value, _tags, _fields);
        }

        public RecordData Timestamp(DateTime timestamp, TimePrecision timeUnit)
        {
            if (timestamp.Kind != DateTimeKind.Utc)
            {
                throw new ArgumentException("Timestamps must be specified as UTC", "timestamp");
            }

            TimeSpan timestamp2 = timestamp.Subtract(EpochStart);
            return Timestamp(timestamp2, timeUnit);
        }

        public RecordData Timestamp(DateTimeOffset timestamp, TimePrecision timeUnit)
        {
            return Timestamp(timestamp.UtcDateTime, timeUnit);
        }

   

        public bool HasFields()
        {
            return _fields.Count > 0;
        }

        public string ToLineProtocol(RecordSettings pointSettings = null)
        {
            StringBuilder stringBuilder = new StringBuilder();
            EscapeKey(stringBuilder, _tableName, escapeEqual: false);
            AppendTags(stringBuilder, pointSettings);
            if (!AppendFields(stringBuilder))
            {
                return "";
            }

            AppendTime(stringBuilder);
            return stringBuilder.ToString();
        }

        private RecordData PutField(string name, object value)
        {
            Arguments.CheckNonEmptyString(name, "Field name");
            ImmutableSortedDictionary<string, object> immutableSortedDictionary = _fields;
            if (immutableSortedDictionary.ContainsKey(name))
            {
                immutableSortedDictionary = immutableSortedDictionary.Remove(name);
            }

            immutableSortedDictionary = immutableSortedDictionary.Add(name, value);
            return new RecordData(_tableName, Precision, _time, _tags, immutableSortedDictionary);
        }

        private static BigInteger TimeSpanToBigInteger(TimeSpan timestamp, TimePrecision timeUnit)
        {
            return timeUnit switch
            {
                TimePrecision.Ns => timestamp.Ticks * 100,
                TimePrecision.Us => (BigInteger)((double)timestamp.Ticks * 0.1),
                TimePrecision.Ms => (BigInteger)timestamp.TotalMilliseconds,
                TimePrecision.S => (BigInteger)timestamp.TotalSeconds,
                _ => throw new ArgumentOutOfRangeException("timeUnit", timeUnit, "WritePrecision value is not supported"),
            };
        }

        //private static BigInteger InstantToBigInteger(Instant timestamp, TimePrecision timeUnit)
        //{
        //    return timeUnit switch
        //    {
        //        TimePrecision.S => timestamp.ToUnixTimeSeconds(),
        //        TimePrecision.Ms => timestamp.ToUnixTimeMilliseconds(),
        //        TimePrecision.Us => (long)((double)timestamp.ToUnixTimeTicks() * 0.1),
        //        TimePrecision.Ns => (timestamp - NodaConstants.UnixEpoch).ToBigIntegerNanoseconds(),
        //        _ => throw new ArgumentOutOfRangeException("timeUnit", timeUnit, "WritePrecision value is not supported"),
        //    };
        //}

        private void AppendTags(StringBuilder writer, RecordSettings pointSettings)
        {
            IReadOnlyDictionary<string, string> readOnlyDictionary;
            if (pointSettings == null)
            {
                readOnlyDictionary = _tags;
            }
            else
            {
                IDictionary<string, string> defaultTags = pointSettings.GetDefaultTags();
                try
                {
                    readOnlyDictionary = _tags.AddRange(defaultTags);
                }
                catch (ArgumentException)
                {
                    ImmutableSortedDictionary<string, string>.Builder builder = _tags.ToBuilder();
                    foreach (KeyValuePair<string, string> item in defaultTags)
                    {
                        string key = item.Key;
                        if (!builder.ContainsKey(key))
                        {
                            builder.Add(key, item.Value);
                        }
                    }

                    readOnlyDictionary = builder;
                }
            }

            foreach (KeyValuePair<string, string> item2 in readOnlyDictionary)
            {
                string key2 = item2.Key;
                string value = item2.Value;
                if (!string.IsNullOrEmpty(key2) && !string.IsNullOrEmpty(value))
                {
                    writer.Append(',');
                    EscapeKey(writer, key2);
                    writer.Append('=');
                    EscapeKey(writer, value);
                }
            }

            writer.Append(' ');
        }

        private bool AppendFields(StringBuilder sb)
        {
            bool flag = false;
            foreach (KeyValuePair<string, object> field in _fields)
            {
                string key = field.Key;
                object value = field.Value;
                if (IsNotDefined(value))
                {
                    continue;
                }

                EscapeKey(sb, key);
                sb.Append('=');
                if (value is double || value is float)
                {
                    sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                }
                else if (value is uint || value is ulong || value is ushort)
                {
                    sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    sb.Append('u');
                }
                else if (value is byte || value is int || value is long || value is sbyte || value is short)
                {
                    sb.Append(((IConvertible)value).ToString(CultureInfo.InvariantCulture));
                    sb.Append('i');
                }
                else if (value is bool)
                {
                    bool flag2 = (bool)value;
                    sb.Append(flag2 ? "true" : "false");
                }
                else
                {
                    string text = value as string;
                    if (text != null)
                    {
                        sb.Append('"');
                        EscapeValue(sb, text);
                        sb.Append('"');
                    }
                    else
                    {
                        IConvertible convertible = value as IConvertible;
                        if (convertible != null)
                        {
                            sb.Append(convertible.ToString(CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append('"');
                            EscapeValue(sb, value.ToString());
                            sb.Append('"');
                        }
                    }
                }

                sb.Append(',');
                flag = true;
            }

            if (flag)
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return flag;
        }

        private void AppendTime(StringBuilder sb)
        {
            if (_time.HasValue)
            {
                sb.Append(' ');
                sb.Append(_time.Value.ToString(CultureInfo.InvariantCulture));
            }
        }

        private void EscapeKey(StringBuilder sb, string key, bool escapeEqual = true)
        {
            foreach (char c in key)
            {
                switch (c)
                {
                    case '\n':
                        sb.Append("\\n");
                        continue;
                    case '\r':
                        sb.Append("\\r");
                        continue;
                    case '\t':
                        sb.Append("\\t");
                        continue;
                    case ' ':
                    case ',':
                        sb.Append("\\");
                        break;
                    case '=':
                        if (escapeEqual)
                        {
                            sb.Append("\\");
                        }

                        break;
                }

                sb.Append(c);
            }
        }

        private void EscapeValue(StringBuilder sb, string value)
        {
            foreach (char c in value)
            {
                if (c == '"' || c == '\\')
                {
                    sb.Append("\\");
                }

                sb.Append(c);
            }
        }

        private bool IsNotDefined(object value)
        {
            bool result = false;
            if (value != null)
            {
                if (value is double)
                {
                    double d = (double)value;
                    if (double.IsInfinity(d) || double.IsNaN(d))
                    {
                        result = true;
                    }
                }
                if (value is float)
                {
                    float f = (float)value;
                    if (!float.IsInfinity(f))
                    {
                        result = float.IsNaN(f);
                    }
                    else
                    {


                        result = true;
                    }
                }
                else
                {
                    result = false;
                }


            }
            else
            {

                result = true;
            }
            return result;
        }

        public override bool Equals(object obj)
        {
            return Equals(obj as RecordData);
        }

        public bool Equals(RecordData other)
        {
            if (other == null)
            {
                return false;
            }

            ImmutableSortedDictionary<string, string> otherTags = other._tags;
            bool num = _tags.Count == otherTags.Count && _tags.All(delegate (KeyValuePair<string, string> pair)
            {
                string key2 = pair.Key;
                string value2 = pair.Value;
                return otherTags.ContainsKey(key2) && otherTags[key2] == value2;
            });
            ImmutableSortedDictionary<string, object> otherFields = other._fields;
            if (num && _fields.Count == otherFields.Count && _fields.All(delegate (KeyValuePair<string, object> pair)
            {
                string key = pair.Key;
                object value = pair.Value;
                return otherFields.ContainsKey(key) && object.Equals(otherFields[key], value);
            }) && _tableName == other._tableName && Precision == other.Precision)
            {
                return EqualityComparer<BigInteger?>.Default.Equals(_time, other._time);
            }

            return false;
        }

        public override int GetHashCode()
        {
            int num = 318335609;
            num = num * -1521134295 + EqualityComparer<string>.Default.GetHashCode(_tableName);
            num = num * -1521134295 + Precision.GetHashCode();
            int num2 = num * -1521134295;
            BigInteger? time = _time;
            num = num2 + time.GetHashCode();
            foreach (KeyValuePair<string, string> tag in _tags)
            {
                num = (num * -1521134295 + tag.Key?.GetHashCode()).GetValueOrDefault();
                num = (num * -1521134295 + tag.Value?.GetHashCode()).GetValueOrDefault();
            }

            foreach (KeyValuePair<string, object> field in _fields)
            {
                num = (num * -1521134295 + field.Key?.GetHashCode()).GetValueOrDefault();
                num = (num * -1521134295 + field.Value?.GetHashCode()).GetValueOrDefault();
            }

            return num;
        }

        public static bool operator ==(RecordData left, RecordData right)
        {
            return EqualityComparer<RecordData>.Default.Equals(left, right);
        }

        public static bool operator !=(RecordData left, RecordData right)
        {
            return !(left == right);
        }
    }
}
#endif