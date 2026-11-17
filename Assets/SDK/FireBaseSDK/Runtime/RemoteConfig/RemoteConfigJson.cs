// Assets/SDK/FireBaseSDK/Runtime/RemoteConfig/RemoteConfigJson.cs
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace LSDK.Firebase
{
    /// <summary>
    /// Đổi List, mảng và Dictionary với chuỗi JSON của Firebase Remote Config.
    /// Copy Value, default gửi lên Firebase và giá trị đọc về dùng cùng một chuỗi.
    /// </summary>
    public static class RemoteConfigJson
    {
        /// <summary>JSON một dòng. Số không có dấu nháy, chuỗi có dấu nháy.</summary>
        public static string ToJson(object value)
        {
            return ToJson(value, false);
        }

        /// <summary>JSON. <paramref name="pretty"/> bật thì xuống dòng cho preview.</summary>
        public static string ToJson(object value, bool pretty)
        {
            var sb = new StringBuilder();
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        /// <summary>Đọc chuỗi JSON thành đúng <paramref name="type"/> (List, mảng, Dictionary hoặc kiểu đơn).</summary>
        public static bool TryConvert(string json, Type type, out object value)
        {
            value = null;
            if (type == null || string.IsNullOrWhiteSpace(json)) return false;

            int index = 0;
            if (!TryParse(json, ref index, out object parsed)) return false;
            Skip(json, ref index);
            if (index != json.Length) return false;
            return TryCast(parsed, type, out value);
        }

        private sealed class JsonNumber
        {
            public string Text;
        }

        private static void Write(StringBuilder sb, object value, bool pretty, int depth)
        {
            if (value == null)
            {
                sb.Append("null");
                return;
            }

            if (value is string text)
            {
                WriteString(sb, text);
                return;
            }

            if (value is bool flag)
            {
                sb.Append(flag ? "true" : "false");
                return;
            }

            if (IsNumber(value.GetType()))
            {
                sb.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            if (value is IDictionary dictionary)
            {
                WriteDictionary(sb, dictionary, pretty, depth);
                return;
            }

            if (value is IEnumerable sequence)
            {
                WriteSequence(sb, sequence, pretty, depth);
                return;
            }

            WriteString(sb, value.ToString());
        }

        private static void WriteDictionary(StringBuilder sb, IDictionary dictionary, bool pretty, int depth)
        {
            sb.Append(pretty ? "{\n" : "{");
            bool first = true;
            foreach (object key in dictionary.Keys)
            {
                if (!first) sb.Append(pretty ? ",\n" : ",");
                first = false;
                if (pretty) Indent(sb, depth + 1);
                WriteString(sb, key == null ? string.Empty : Convert.ToString(key, CultureInfo.InvariantCulture));
                sb.Append(pretty ? ": " : ":");
                Write(sb, dictionary[key], pretty, depth + 1);
            }

            if (!first && pretty)
            {
                sb.Append('\n');
                Indent(sb, depth);
            }

            sb.Append('}');
        }

        private static void WriteSequence(StringBuilder sb, IEnumerable sequence, bool pretty, int depth)
        {
            sb.Append(pretty ? "[\n" : "[");
            bool first = true;
            foreach (object item in sequence)
            {
                if (!first) sb.Append(pretty ? ",\n" : ",");
                first = false;
                if (pretty) Indent(sb, depth + 1);
                Write(sb, item, pretty, depth + 1);
            }

            if (!first && pretty)
            {
                sb.Append('\n');
                Indent(sb, depth);
            }

            sb.Append(']');
        }

        private static void WriteString(StringBuilder sb, string text)
        {
            sb.Append('"');
            if (text != null)
            {
                for (int i = 0; i < text.Length; i++)
                {
                    char c = text[i];
                    switch (c)
                    {
                        case '\\': sb.Append("\\\\"); break;
                        case '"': sb.Append("\\\""); break;
                        case '\n': sb.Append("\\n"); break;
                        case '\r': sb.Append("\\r"); break;
                        case '\t': sb.Append("\\t"); break;
                        default: sb.Append(c); break;
                    }
                }
            }

            sb.Append('"');
        }

        private static void Indent(StringBuilder sb, int depth)
        {
            for (int i = 0; i < depth; i++) sb.Append("  ");
        }

        private static bool IsNumber(Type type)
        {
            return type == typeof(byte) || type == typeof(sbyte)
                || type == typeof(short) || type == typeof(ushort)
                || type == typeof(int) || type == typeof(uint)
                || type == typeof(long) || type == typeof(ulong)
                || type == typeof(float) || type == typeof(double)
                || type == typeof(decimal);
        }

        private static bool TryParse(string json, ref int index, out object value)
        {
            Skip(json, ref index);
            value = null;
            if (index >= json.Length) return false;

            char c = json[index];
            if (c == '"') return TryParseString(json, ref index, out value);
            if (c == '{') return TryParseObject(json, ref index, out value);
            if (c == '[') return TryParseArray(json, ref index, out value);
            if (c == 't') return TryLiteral(json, ref index, "true", true, out value);
            if (c == 'f') return TryLiteral(json, ref index, "false", false, out value);
            if (c == 'n') return TryLiteral(json, ref index, "null", null, out value);
            if (c == '-' || (c >= '0' && c <= '9')) return TryParseNumber(json, ref index, out value);
            return false;
        }

        private static bool TryLiteral(string json, ref int index, string word, object literal, out object value)
        {
            value = null;
            if (index + word.Length > json.Length) return false;
            if (string.CompareOrdinal(json, index, word, 0, word.Length) != 0) return false;
            index += word.Length;
            value = literal;
            return true;
        }

        private static bool TryParseNumber(string json, ref int index, out object value)
        {
            int start = index;
            if (json[index] == '-') index++;
            while (index < json.Length)
            {
                char c = json[index];
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                {
                    index++;
                    continue;
                }

                break;
            }

            value = new JsonNumber { Text = json.Substring(start, index - start) };
            return index > start;
        }

        private static bool TryParseString(string json, ref int index, out object value)
        {
            value = null;
            index++;
            var sb = new StringBuilder();
            while (index < json.Length)
            {
                char c = json[index++];
                if (c == '"')
                {
                    value = sb.ToString();
                    return true;
                }

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (index >= json.Length) return false;
                char esc = json[index++];
                switch (esc)
                {
                    case '"':
                    case '\\':
                    case '/': sb.Append(esc); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (index + 4 > json.Length) return false;
                        int code;
                        if (!int.TryParse(json.Substring(index, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out code))
                            return false;
                        sb.Append((char)code);
                        index += 4;
                        break;
                    default:
                        return false;
                }
            }

            return false;
        }

        private static bool TryParseArray(string json, ref int index, out object value)
        {
            value = null;
            index++;
            var list = new List<object>();
            Skip(json, ref index);
            if (index < json.Length && json[index] == ']')
            {
                index++;
                value = list;
                return true;
            }

            while (index < json.Length)
            {
                object item;
                if (!TryParse(json, ref index, out item)) return false;
                list.Add(item);
                Skip(json, ref index);
                if (index >= json.Length) return false;
                if (json[index] == ',')
                {
                    index++;
                    continue;
                }

                if (json[index] == ']')
                {
                    index++;
                    value = list;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static bool TryParseObject(string json, ref int index, out object value)
        {
            value = null;
            index++;
            var map = new Dictionary<string, object>();
            Skip(json, ref index);
            if (index < json.Length && json[index] == '}')
            {
                index++;
                value = map;
                return true;
            }

            while (index < json.Length)
            {
                Skip(json, ref index);
                object key;
                if (!TryParseString(json, ref index, out key)) return false;
                Skip(json, ref index);
                if (index >= json.Length || json[index] != ':') return false;
                index++;
                object item;
                if (!TryParse(json, ref index, out item)) return false;
                map[(string)key] = item;
                Skip(json, ref index);
                if (index >= json.Length) return false;
                if (json[index] == ',')
                {
                    index++;
                    continue;
                }

                if (json[index] == '}')
                {
                    index++;
                    value = map;
                    return true;
                }

                return false;
            }

            return false;
        }

        private static void Skip(string json, ref int index)
        {
            while (index < json.Length && char.IsWhiteSpace(json[index])) index++;
        }

        private static bool TryCast(object parsed, Type type, out object value)
        {
            value = null;
            if (parsed == null)
            {
                if (type.IsValueType && Nullable.GetUnderlyingType(type) == null) return false;
                return true;
            }

            if (type == typeof(string))
            {
                value = parsed as string ?? ToJson(parsed);
                return true;
            }

            if (parsed is JsonNumber number) return TryCastNumber(number.Text, type, out value);
            if (parsed is string numberText && IsNumber(type)) return TryCastNumber(numberText, type, out value);
            if (type == typeof(bool) && parsed is bool) { value = parsed; return true; }

            if (type.IsArray && parsed is List<object> arrayItems)
                return TryCastArray(arrayItems, type.GetElementType(), out value);

            if (parsed is List<object> listItems && IsListType(type))
                return TryCastList(listItems, type, out value);

            if (parsed is Dictionary<string, object> map && IsDictionaryType(type))
                return TryCastDictionary(map, type, out value);

            if (type.IsInstanceOfType(parsed))
            {
                value = parsed;
                return true;
            }

            return false;
        }

        private static bool TryCastNumber(string text, Type type, out object value)
        {
            value = null;
            if (type == typeof(float) || type == typeof(double) || type == typeof(decimal))
            {
                double number;
                if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number)) return false;
                value = Convert.ChangeType(number, type, CultureInfo.InvariantCulture);
                return true;
            }

            if (!IsNumber(type)) return false;
            if (text.IndexOf('.') >= 0 || text.IndexOf('e') >= 0 || text.IndexOf('E') >= 0) return false;
            long integer;
            if (long.TryParse(text, NumberStyles.Integer, CultureInfo.InvariantCulture, out integer))
            {
                value = Convert.ChangeType(integer, type, CultureInfo.InvariantCulture);
                return true;
            }

            double fallback;
            if (!double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out fallback)) return false;
            value = Convert.ChangeType(fallback, type, CultureInfo.InvariantCulture);
            return true;
        }

        private static bool TryCastArray(List<object> items, Type elementType, out object value)
        {
            value = null;
            Array array = Array.CreateInstance(elementType, items.Count);
            for (int i = 0; i < items.Count; i++)
            {
                object item;
                if (!TryCast(items[i], elementType, out item)) return false;
                array.SetValue(item, i);
            }

            value = array;
            return true;
        }

        private static bool TryCastList(List<object> items, Type type, out object value)
        {
            value = null;
            Type elementType = type.GetGenericArguments()[0];
            Type concrete = type.IsInterface ? typeof(List<>).MakeGenericType(elementType) : type;
            var list = Activator.CreateInstance(concrete) as IList;
            if (list == null) return false;
            for (int i = 0; i < items.Count; i++)
            {
                object item;
                if (!TryCast(items[i], elementType, out item)) return false;
                list.Add(item);
            }

            value = list;
            return true;
        }

        private static bool TryCastDictionary(Dictionary<string, object> map, Type type, out object value)
        {
            value = null;
            Type[] args = type.GetGenericArguments();
            Type concrete = type.IsInterface ? typeof(Dictionary<,>).MakeGenericType(args) : type;
            var dictionary = Activator.CreateInstance(concrete) as IDictionary;
            if (dictionary == null) return false;

            foreach (KeyValuePair<string, object> pair in map)
            {
                object key;
                object item;
                if (!TryCast(pair.Key, args[0], out key)) return false;
                if (!TryCast(pair.Value, args[1], out item)) return false;
                dictionary.Add(key, item);
            }

            value = dictionary;
            return true;
        }

        private static bool IsListType(Type type)
        {
            if (!type.IsGenericType) return false;
            Type definition = type.GetGenericTypeDefinition();
            return definition == typeof(List<>);
        }

        private static bool IsDictionaryType(Type type)
        {
            if (!type.IsGenericType) return false;
            Type definition = type.GetGenericTypeDefinition();
            return definition == typeof(Dictionary<,>);
        }
    }
}
