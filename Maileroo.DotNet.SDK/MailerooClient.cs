using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace Maileroo.DotNet.SDK;

public class MailerooClient
{
    private const string ApiBaseUrl = "https://smtp.maileroo.com/api/v2/";

    private const int MaxAssociativeMapKeyLength = 128;
    private const int MaxAssociativeMapValueLength = 768;

    private const int MaxSubjectLength = 255;
    private const int ReferenceIdLength = 24; // hex chars

    private readonly string _apiKey;
    private readonly HttpClient _http;

    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
    };

    public MailerooClient(string apiKey, TimeSpan? timeout = null, HttpClient? httpClient = null)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
            throw new ArgumentException("API key must be a non-empty string.", nameof(apiKey));

        _apiKey = apiKey;

        _http = httpClient ?? new HttpClient();
        _http.Timeout = timeout ?? TimeSpan.FromSeconds(30);
        if (!_http.DefaultRequestHeaders.Contains("Authorization"))
            _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
        if (!_http.DefaultRequestHeaders.Contains("User-Agent"))
            _http.DefaultRequestHeaders.Add("User-Agent", "MailerooClient/1.0");
    }

    public string GetReferenceId()
    {
        var bytes = RandomNumberGenerator.GetBytes(ReferenceIdLength / 2);
        var sb = new StringBuilder(ReferenceIdLength);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    public async Task<string> SendBasicEmailAsync(Dictionary<string, object?> data, CancellationToken ct = default)
    {
        var payload = BuildBasePayload(data);

        if (!data.ContainsKey("html") && !data.ContainsKey("plain"))
            throw new ArgumentException("Either html or plain body is required.", nameof(data));

        payload["html"] = data.TryGetValue("html", out var h) ? h : null;
        payload["plain"] = data.TryGetValue("plain", out var p) ? p : null;

        var resp = await SendRequestAsync(HttpMethod.Post, "emails", payload, ct);
        return Success(resp) ? GetData<string>(resp, "reference_id") : throw new InvalidOperationException("The API returned an error: " + GetMessage(resp));
    }

    public async Task<string> SendTemplatedEmailAsync(Dictionary<string, object?> data, CancellationToken ct = default)
    {
        var payload = BuildBasePayload(data);

        if (!data.TryGetValue("template_id", out var tid) || (tid is not int && tid is not string))
            throw new ArgumentException("template_id must be an integer or a string.", nameof(data));

        payload["template_id"] = Convert.ToInt32(tid);

        if (data.TryGetValue("template_data", out var tdata))
            payload["template_data"] = ValidateTemplateData(tdata);

        var resp = await SendRequestAsync(HttpMethod.Post, "emails/template", payload, ct);
        return Success(resp) ? GetData<string>(resp, "reference_id") : throw new InvalidOperationException("The API returned an error: " + GetMessage(resp));
    }

    public async Task<IReadOnlyList<string>> SendBulkEmailsAsync(Dictionary<string, object?> data, CancellationToken ct = default)
    {
        if (!data.TryGetValue("subject", out var subjObj) || subjObj is not string subject || string.IsNullOrWhiteSpace(subject) || subject.Length > MaxSubjectLength)
            throw new ArgumentException($"Subject must be a non-empty string with a maximum length of {MaxSubjectLength} characters.", nameof(data));

        var hasHtml = data.TryGetValue("html", out var html) && html is string;
        var hasPlain = data.TryGetValue("plain", out var plain) && plain is string;
        var hasTemplate = data.TryGetValue("template_id", out var tid) && (tid is int || tid is string);

        if ((!hasHtml && !hasPlain) && !hasTemplate)
            throw new ArgumentException("You must provide either html, plain, or template_id.");

        if (hasTemplate && (hasHtml || hasPlain))
            throw new ArgumentException("template_id cannot be combined with html or plain.");

        if (!data.TryGetValue("messages", out var msgsObj) || msgsObj is not IEnumerable<object?>)
            throw new ArgumentException("messages must be a non-empty array.", nameof(data));

        var msgsList = ((IEnumerable<object?>)msgsObj).ToList();
        switch (msgsList.Count)
        {
            case 0:
                throw new ArgumentException("messages must be a non-empty array.", nameof(data));
            case > 500:
                throw new ArgumentException("messages cannot contain more than 500 items.", nameof(data));
        }

        var payload = new Dictionary<string, object?> { ["subject"] = subject };

        if (hasHtml) payload["html"] = html;
        if (hasPlain) payload["plain"] = plain;
        if (hasTemplate) payload["template_id"] = Convert.ToInt32(tid);

        if (data.TryGetValue("tracking", out var tracking))
        {
            if (tracking is not bool) throw new ArgumentException("Tracking must be a boolean value.");
            payload["tracking"] = tracking;
        }

        if (data.TryGetValue("tags", out var tags) && tags is IDictionary<string, object?> tdict)
        {
            ValidateAssociativeMap(tdict, "tags");
            payload["tags"] = tdict;
        }

        if (data.TryGetValue("headers", out var headers) && headers is IDictionary<string, object?> hdict)
        {
            ValidateAssociativeMap(hdict, "headers");
            payload["headers"] = hdict;
        }

        if (data.TryGetValue("attachments", out var atts) && atts is IEnumerable<object?> attList)
            payload["attachments"] = NormalizeAttachments(attList);

        payload["messages"] = NormalizeBulkMessages(msgsList);

        var resp = await SendRequestAsync(HttpMethod.Post, "emails/bulk", payload, ct);
        if (Success(resp))
            return GetData<List<string>>(resp, "reference_ids");

        throw new InvalidOperationException("The API returned an error: " + GetMessage(resp));
    }

    public async Task<bool> DeleteScheduledEmailAsync(string referenceId, CancellationToken ct = default)
    {
        referenceId = ValidateReferenceId(referenceId);
        var resp = await SendRequestAsync(HttpMethod.Delete, $"emails/scheduled/{referenceId}", null, ct);
        if (Success(resp)) return true;
        throw new InvalidOperationException("The API returned an error: " + GetMessage(resp));
    }

    public async Task<Dictionary<string, object?>> GetScheduledEmailsAsync(int page = 1, int perPage = 10, CancellationToken ct = default)
    {
        if (page < 1) throw new ArgumentException("page must be a positive integer (>= 1).", nameof(page));

        switch (perPage)
        {
            case < 1:
                throw new ArgumentException("per_page must be a positive integer (>= 1).", nameof(perPage));
            case > 100:
                throw new ArgumentException("per_page cannot be greater than 100.", nameof(perPage));
        }

        var qs = new Dictionary<string, object?> { ["page"] = page, ["per_page"] = perPage };
        var resp = await SendRequestAsync(HttpMethod.Get, "emails/scheduled", qs, ct);

        if (Success(resp))
            return GetField<Dictionary<string, object?>>(resp, "data");

        throw new InvalidOperationException("The API returned an error: " + GetMessage(resp));
    }
        
    private Dictionary<string, object?> BuildBasePayload(Dictionary<string, object?> data)
    {
        var payload = GetParsedEmailItems(data);

        if (!data.TryGetValue("subject", out var subjObj) || subjObj is not string subject || string.IsNullOrWhiteSpace(subject) || subject.Length > MaxSubjectLength)
            throw new ArgumentException($"Subject must be a non-empty string with a maximum length of {MaxSubjectLength} characters.", nameof(data));

        payload["subject"] = subject;

        if (data.TryGetValue("tracking", out var tracking))
        {
            if (tracking is not bool) throw new ArgumentException("Tracking must be a boolean value.");
            payload["tracking"] = tracking;
        }

        if (data.TryGetValue("tags", out var tags) && tags is IDictionary<string, object?> tdict)
        {
            ValidateAssociativeMap(tdict, "tags");
            payload["tags"] = tdict;
        }

        if (data.TryGetValue("headers", out var headers) && headers is IDictionary<string, object?> hdict)
        {
            ValidateAssociativeMap(hdict, "headers");
            payload["headers"] = hdict;
        }

        if (data.TryGetValue("attachments", out var atts) && atts is IEnumerable<object?> attList)
            payload["attachments"] = NormalizeAttachments(attList);

        if (data.TryGetValue("scheduled_at", out var sched) && sched is string s)
            payload["scheduled_at"] = s;

        if (data.TryGetValue("reference_id", out var rid) && rid is string ridStr)
        {
            payload["reference_id"] = ValidateReferenceId(ridStr);
        }
        else
        {
            payload["reference_id"] = GetReferenceId();
        }

        return payload;
    }

    private static object? NormalizeAttachments(IEnumerable<object?> list)
    {
        var outList = new List<Dictionary<string, object?>>();
        foreach (var a in list)
        {
            if (a is Attachment att)
                outList.Add(att.ToApi());
            else
                throw new ArgumentException("Each attachment must be an instance of Attachment.");
        }

        return outList.Count > 0 ? outList : null;
    }

    private static object? GetEmailArrays(object? emailList)
    {
        if (emailList is null) return null;

        if (emailList is IEnumerable<EmailAddress> many)
            return many.Select(e => e.ToApi()).ToList();

        if (emailList is EmailAddress single)
            return single.ToApi();

        throw new ArgumentException("Email field must be EmailAddress or IEnumerable<EmailAddress>.");
    }

    private Dictionary<string, object?> GetParsedEmailItems(Dictionary<string, object?> data)
    {
        var required = new[] { "from", "to" };
        foreach (var f in required)
            if (!data.ContainsKey(f))
                throw new ArgumentException($"Field {f} is required.");

        var normalized = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        var from = NormalizeEmailField(Expect<EmailAddress>(data, "from"));
        var to = NormalizeEmailFieldOrArray(Expect<object>(data, "to"));

        normalized["from"] = GetEmailArrays(from);
        normalized["to"] = GetEmailArrays(to);

        if (data.TryGetValue("cc", out var cc))
            normalized["cc"] = GetEmailArrays(NormalizeEmailFieldOrArray(cc));

        if (data.TryGetValue("bcc", out var bcc))
            normalized["bcc"] = GetEmailArrays(NormalizeEmailFieldOrArray(bcc));

        if (data.TryGetValue("reply_to", out var replyTo))
            normalized["reply_to"] = GetEmailArrays(NormalizeEmailFieldOrArray(replyTo));

        return normalized;
    }

    private static EmailAddress NormalizeEmailField(object field)
    {
        if (field is EmailAddress ea) return ea;
        throw new ArgumentException("Email field must be an instance of EmailAddress.");
    }

    private static object? NormalizeEmailFieldOrArray(object? field)
    {
        return field switch
        {
            null => null,
            EmailAddress ea => ea,
            IEnumerable<EmailAddress> list => list.ToList(),
            _ => throw new ArgumentException("Email field must be EmailAddress or IEnumerable<EmailAddress>.")
        };
    }

    private List<Dictionary<string, object?>> NormalizeBulkMessages(IEnumerable<object?> messages)
    {
        var normalized = new List<Dictionary<string, object?>>();
        var idx = 0;

        foreach (var msgObj in messages)
        {
            if (msgObj is not Dictionary<string, object?> msg)
                throw new ArgumentException($"Each message must be a dictionary (message index {idx}).");

            if (!msg.ContainsKey("from") || !msg.ContainsKey("to"))
                throw new ArgumentException($"Each message must include 'from' and 'to' (message index {idx}).");

            var from = NormalizeEmailField(Expect<EmailAddress>(msg, "from"));
            var to = NormalizeEmailFieldOrArray(Expect<object>(msg, "to"));

            object? cc = null, bcc = null, replyTo = null;
            if (msg.TryGetValue("cc", out var ccObj)) cc = NormalizeEmailFieldOrArray(ccObj);
            if (msg.TryGetValue("bcc", out var bccObj)) bcc = NormalizeEmailFieldOrArray(bccObj);
            if (msg.TryGetValue("reply_to", out var rtObj)) replyTo = NormalizeEmailFieldOrArray(rtObj);

            var item = new Dictionary<string, object?>
            {
                ["from"] = GetEmailArrays(from),
                ["to"] = GetEmailArrays(to),
                ["cc"] = GetEmailArrays(cc),
                ["bcc"] = GetEmailArrays(bcc),
                ["reply_to"] = GetEmailArrays(replyTo),
            };

            if (msg.TryGetValue("reference_id", out var rid) && rid is string rids)
                item["reference_id"] = ValidateReferenceId(rids);
            else
                item["reference_id"] = GetReferenceId();

            if (msg.TryGetValue("template_data", out var tdata))
                item["template_data"] = ValidateTemplateData(tdata);

            normalized.Add(item);
            idx++;
        }

        return normalized;
    }

    private static void ValidateAssociativeMap(IDictionary<string, object?> map, string label)
    {
        foreach (var kv in map)
        {
            var key = kv.Key;
            var val = kv.Value;

            if (key is null || key.Length == 0)
                throw new ArgumentException($"{label} keys must be strings.");

            if (!IsAcceptableTagOrHeaderValue(val))
                throw new ArgumentException($"{label} must be an associative map with string keys and scalar values.");

            if (key.Length > MaxAssociativeMapKeyLength || Convert.ToString(val)?.Length > MaxAssociativeMapValueLength)
                throw new ArgumentException($"{label} key must not exceed {MaxAssociativeMapKeyLength} characters and value must not exceed {MaxAssociativeMapValueLength} characters.");
        }
    }

    private static bool IsAcceptableTagOrHeaderValue(object? value) =>
        value is null || value is string || value is int || value is long || value is double || value is float || value is bool || value is decimal;

    private static object ValidateTemplateData(object? templateData)
    {
        return templateData switch
        {
            null => new Dictionary<string, object?>(),
            IDictionary<string, object?> dict => dict,
            _ => throw new ArgumentException("template_data must be a dictionary if provided.")
        };
    }

    private static T Expect<T>(IDictionary<string, object?> dict, string key)
    {
        if (!dict.TryGetValue(key, out var v) || v is not T t)
            throw new ArgumentException($"Field '{key}' has an unexpected type or is missing.");
        return t;
    }

    private static string ValidateReferenceId(string referenceId)
    {
        if (referenceId is null) throw new ArgumentException("reference_id must be a string.");
        if (!referenceId.Equals(referenceId.Trim(), StringComparison.Ordinal))
            throw new ArgumentException("reference_id must not contain whitespace.");

        if (!Regex.IsMatch(referenceId, @"^[0-9a-fA-F]{" + ReferenceIdLength + "}$"))
            throw new ArgumentException($"reference_id must be a {ReferenceIdLength}-character hexadecimal string.");

        return referenceId.ToLowerInvariant();
    }

    // ----- HTTP -----

    private async Task<Dictionary<string, object?>> SendRequestAsync(HttpMethod method, string endpoint, object? data, CancellationToken ct)
    {
        var url = ApiBaseUrl.TrimEnd('/') + "/" + endpoint.TrimStart('/');

        HttpRequestMessage req;

        if (method == HttpMethod.Get && data is IDictionary<string, object?> q && q.Count > 0)
        {
            var qs = string.Join("&", q.Select(kv => $"{Uri.EscapeDataString(kv.Key)}={Uri.EscapeDataString(Convert.ToString(kv.Value) ?? string.Empty)}"));
            url += (url.Contains('?') ? "&" : "?") + qs;
            req = new HttpRequestMessage(HttpMethod.Get, url);
        }
        else
        {
            req = new HttpRequestMessage(method, url);
            if (method != HttpMethod.Get)
            {
                var json = data is null ? "" : JsonSerializer.Serialize(data, JsonOpts);
                req.Content = new StringContent(json, Encoding.UTF8, "application/json");
            }
        }

        using var resp = await _http.SendAsync(req, ct);
        var raw = await resp.Content.ReadAsStringAsync(ct);

        Dictionary<string, object?>? decoded;
        try
        {
            decoded = JsonSerializer.Deserialize<Dictionary<string, object?>>(raw, JsonOpts);
        }
        catch (Exception)
        {
            throw new InvalidOperationException("The API response is not valid JSON.");
        }

        if (decoded is null || !decoded.ContainsKey("success"))
            throw new InvalidOperationException("The API response is missing the \"success\" field.");

        bool success = decoded["success"] switch
        {
            bool b => b,
            JsonElement { ValueKind: JsonValueKind.True } => true,
            JsonElement { ValueKind: JsonValueKind.False } => false,
            _ => throw new InvalidOperationException("The API response 'success' field is not a boolean.")
        };

        decoded["success"] = success; // normalize for later

        if (!decoded.ContainsKey("message") || decoded["message"] is null)
            decoded["message"] = "Unknown";

        return decoded;
    }

    private static bool Success(Dictionary<string, object?> resp) =>
        resp.TryGetValue("success", out var s) && s is bool b && b;

    private static T GetData<T>(Dictionary<string, object?> resp, string field)
    {
        var data = GetField<Dictionary<string, object?>>(resp, "data");
        if (!data.TryGetValue(field, out var v) || v is not JsonElement je)
            throw new InvalidOperationException($"The API response is missing the data.{field} field.");

        // Handle System.Text.Json element conversion to T
        var typed = je.Deserialize<T>(JsonOpts);
        if (typed == null)
            throw new InvalidOperationException($"Failed to parse data.{field}.");
        return typed;
    }

    private static T GetField<T>(Dictionary<string, object?> resp, string field)
    {
        if (!resp.TryGetValue(field, out var v))
            throw new InvalidOperationException($"The API response is missing the \"{field}\" field.");
        if (v is JsonElement el)
            return el.Deserialize<T>(JsonOpts)!;
        return (T)v!;
    }

    private static string GetMessage(Dictionary<string, object?> resp) =>
        Convert.ToString(resp.TryGetValue("message", out var m) ? m : "Unknown") ?? "Unknown";
}