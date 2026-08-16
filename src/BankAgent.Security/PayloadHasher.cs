using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace BankAgent.Security;

/// <summary>
/// Đóng băng payload bằng hash — SPEC FR-023.
/// Hash phải ổn định trước thay đổi thứ tự khoá JSON, nếu không mọi lần serialize lại
/// sẽ tạo ra hash khác và RM bị bắt duyệt lại một payload không hề đổi.
/// </summary>
public interface IPayloadHasher
{
    string Hash(string canonicalOrRawJson);

    string HashObject<T>(T value);

    bool Matches(string expectedHash, string actualHash);
}

public sealed class Sha256PayloadHasher : IPayloadHasher
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public string Hash(string canonicalOrRawJson)
    {
        var canonical = Canonicalize(canonicalOrRawJson);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(canonical));
        return "sha256:" + Convert.ToHexStringLower(bytes);
    }

    public string HashObject<T>(T value) => Hash(JsonSerializer.Serialize(value, SerializerOptions));

    /// <summary>So sánh chống timing attack — hash là chứng cứ phê duyệt, không phải chuỗi thường.</summary>
    public bool Matches(string expectedHash, string actualHash)
    {
        if (string.IsNullOrEmpty(expectedHash) || string.IsNullOrEmpty(actualHash))
        {
            return false;
        }

        var a = Encoding.UTF8.GetBytes(expectedHash);
        var b = Encoding.UTF8.GetBytes(actualHash);
        return a.Length == b.Length && CryptographicOperations.FixedTimeEquals(a, b);
    }

    /// <summary>
    /// Sắp xếp khoá object theo thứ tự ordinal, giữ nguyên thứ tự phần tử mảng
    /// (thứ tự mảng là dữ liệu, thứ tự khoá thì không).
    /// </summary>
    internal static string Canonicalize(string json)
    {
        JsonNode? node;
        try
        {
            node = JsonNode.Parse(json);
        }
        catch (JsonException)
        {
            // Không phải JSON: hash nguyên văn. Chuỗi thường vốn đã xác định.
            return json;
        }

        return node is null ? "null" : Write(node);
    }

    private static string Write(JsonNode node)
    {
        var buffer = new StringBuilder();
        Append(node, buffer);
        return buffer.ToString();
    }

    private static void Append(JsonNode? node, StringBuilder buffer)
    {
        switch (node)
        {
            case null:
                buffer.Append("null");
                break;

            case JsonObject obj:
                buffer.Append('{');
                var first = true;
                foreach (var pair in obj.OrderBy(p => p.Key, StringComparer.Ordinal))
                {
                    if (!first)
                    {
                        buffer.Append(',');
                    }

                    first = false;
                    buffer.Append(JsonSerializer.Serialize(pair.Key)).Append(':');
                    Append(pair.Value, buffer);
                }

                buffer.Append('}');
                break;

            case JsonArray array:
                buffer.Append('[');
                for (var i = 0; i < array.Count; i++)
                {
                    if (i > 0)
                    {
                        buffer.Append(',');
                    }

                    Append(array[i], buffer);
                }

                buffer.Append(']');
                break;

            default:
                buffer.Append(node.ToJsonString());
                break;
        }
    }
}
