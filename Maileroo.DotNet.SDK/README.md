# Maileroo .NET SDK

[Maileroo](https://maileroo.com) is a robust email delivery platform designed for effortless sending of transactional and marketing emails. This .NET SDK offers a straightforward interface for working with the Maileroo API, supporting basic
email formats, templates, bulk sending, and scheduling capabilities.

## Features

- Send basic HTML or plain text emails with ease
- Use pre-defined templates with dynamic data
- Send up to 500 personalized emails in bulk
- Schedule emails for future delivery
- Manage scheduled emails (list & delete)
- Add tags, custom headers, and reference IDs
- Attach files to your emails
- Support for multiple recipients, CC, BCC, and Reply-To
- Enable or disable open and click tracking
- Built-in input validation and error handling

## Requirements

- .NET 6.0 or later

## Installation

Install via NuGet:

```bash
dotnet add package Maileroo.DotNet.SDK
```

## Quick Start

```csharp
var apiKey = "YOUR_API_KEY";
var client = new MailerooClient(apiKey);

var from = new EmailAddress("sender@example.com", "Your Company");
var to = new EmailAddress("example@example.com", "John Doe");

try
{

    var payload = new Dictionary<string, object?>
    {
        ["from"] = from,
        ["to"] = new List<EmailAddress> { to },
        ["subject"] = "Hello from Maileroo .NET 6",
        ["html"] = "<h1>Hello World</h1><p>This came from the C# SDK test.</p>",
        ["plain"] = "Hello World (plain text fallback)",
    };

    var refId = await client.SendBasicEmailAsync(payload);
    Console.WriteLine($"Email queued! Reference ID: {refId}");
    
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
```

## Usage Examples

### 1. Basic Email with Attachments

```csharp
var apiKey = "YOUR_API_KEY";
var client = new MailerooClient(apiKey);

try
{

    var payload = new Dictionary<string, object?>
    {
        ["from"] = new EmailAddress("sender@example.com", "Your Company"),
        ["to"] = new List<EmailAddress> { 
            new EmailAddress("example@example.com", "John Doe"),
            new EmailAddress("example2@example2.com", "Jane Doe")
        },
        ["subject"] = "Hello from Maileroo .NET 6",
        ["html"] = "<h1>Hello World</h1><p>This came from the C# SDK test.</p>",
        ["plain"] = "Hello World (plain text fallback)",
        ["attachments"] = new List<Attachment>
        {
            Attachment.FromFile(@"C:\path\to\file.txt", "text/plain"),
            Attachment.FromContent("data.txt", "Hello World", "text/plain")
        },
        ["tracking"] = false,
        ["tags"] = new Dictionary<string, object>
        {
            ["tag1"] = "value1",
            ["tag2"] = "value2" 
        },
        ["headers"] = new Dictionary<string, object>
        {
            ["X-Custom-Header"] = "value"
        }
    };

    var refId = await client.SendBasicEmailAsync(payload);
    Console.WriteLine($"Email queued! Reference ID: {refId}");

}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
```

### 2. Template Email

```csharp
var apiKey = "YOUR_API_KEY";
var client = new MailerooClient(apiKey);

var from = new EmailAddress("sender@example.com", "Your Company");
var to = new EmailAddress("example@example.com", "John Doe");

try
{

    var payload = new Dictionary<string, object?>
    {
        ["from"] = new EmailAddress("sender@example.com", "Your Company"),
        ["to"] = new List<EmailAddress> { 
            new EmailAddress("example@example.com", "John Doe"),
            new EmailAddress("example2@example2.com", "Jane Doe")
        },
        ["subject"] = "Hello from Maileroo .NET 6",
        ["html"] = "<h1>Hello World</h1><p>This came from the C# SDK test.</p>",
        ["plain"] = "Hello World (plain text fallback)",
        ["template_id"] = 2549,
        ["template_data"] = new Dictionary<string, object?>
        {
            ["company"] = "Test Company",
            ["status"] = "active",
        },
        ["attachments"] = new List<Attachment>
        {
            Attachment.FromFile(@"C:\path\to\file.txt", "text/plain"),
            Attachment.FromContent("data.txt", "Hello World", "text/plain")
        }
    };

    var refId = await client.SendTemplatedEmailAsync(payload);
    Console.WriteLine($"Email queued! Reference ID: {refId}");
    
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
```

### 3. Bulk Email Sending (With Plain and HTML)

```csharp
var apiKey = "YOUR_API_KEY";
var client = new MailerooClient(apiKey);

var from = new EmailAddress("sender@example.com", "Your Company");
var to = new EmailAddress("example@example.com", "John Doe");

try
{
    var payload = new Dictionary<string, object?>
    {
        ["subject"] = "Hello from Maileroo .NET 6",
        ["html"] = "<h1>Hello World</h1><p>This came from the C# SDK test.</p>",
        ["plain"] = "Hello World (plain text fallback)",
        ["messages"] = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["from"] = new EmailAddress("no.reply@mail.botbuddies.com", "John Doe"),
                ["to"] = new EmailAddress("john.doe@example.com", "John Doe"),
                ["template_data"] = new Dictionary<string, object?>
                {
                    ["name"] = "John Doe",
                    ["company"] = "Maileroo"
                }
            },
            new()
            {
                ["from"] = new EmailAddress("no.reply@mail.botbuddies.com", "John Doe"),
                ["to"] = new EmailAddress("jane.doe@example.com", "Jane Doe"),
                ["template_data"] = new Dictionary<string, object?>
                {
                    ["name"] = "Jane Doe",
                    ["company"] = "Maileroo"
                }
            }
        },
        ["tracking"] = false,
    };

    var refIds = await client.SendBulkEmailsAsync(payload);
    foreach (var refId in refIds)
    {
        Console.WriteLine($"Email sent with reference ID: {refId}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
```

### 4. Bulk Email Sending (With Template ID)

```csharp
var apiKey = "YOUR_API_KEY";
var client = new MailerooClient(apiKey);

var from = new EmailAddress("sender@example.com", "Your Company");
var to = new EmailAddress("example@example.com", "John Doe");

try
{
    var payload = new Dictionary<string, object?>
    {
        ["subject"] = "Hello from Maileroo .NET 6",
        ["template_id"] = 2549,
        ["messages"] = new List<Dictionary<string, object?>>
        {
            new()
            {
                ["from"] = new EmailAddress("no.reply@mail.botbuddies.com", "John Doe"),
                ["to"] = new EmailAddress("john.doe@example.com", "John Doe"),
                ["template_data"] = new Dictionary<string, object?>
                {
                    ["name"] = "John Doe",
                    ["company"] = "Maileroo"
                }
            },
            new()
            {
                ["from"] = new EmailAddress("no.reply@mail.botbuddies.com", "John Doe"),
                ["to"] = new EmailAddress("jane.doe@example.com", "Jane Doe"),
                ["template_data"] = new Dictionary<string, object?>
                {
                    ["name"] = "Jane Doe",
                    ["company"] = "Maileroo"
                }
            }
        },
        ["tracking"] = false,
    };

    var refIds = await client.SendBulkEmailsAsync(payload);
    foreach (var refId in refIds)
    {
        Console.WriteLine($"Email sent with reference ID: {refId}");
    }
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
```

### 5. Working with Attachments

```csharp
var attachment1 = Attachment.FromFile(@"C:\path\to\file.txt", "text/plain", false);
var attachment2 = Attachment.FromContent("data.txt", "Hello World", "text/plain", false, false);

var payload = new Dictionary<string, object?>
{
    ["subject"] = "Hello from Maileroo .NET 6",
    ["html"] = "<h1>Hello World</h1><p>This came from the C# SDK test.</p>",
    ["plain"] = "Hello World (plain text fallback)",
    ["messages"] = new List<Dictionary<string, object?>>
    {
        new()
        {
            ["from"] = new EmailAddress("sender@example.com", "Your Company"),
            ["to"] = new EmailAddress("example@example.com", "John Doe"),
            ["attachments"] = new List<Attachment> { attachment1, attachment2 }
        }
    }
};
```

### 6. Scheduling Emails

You can schedule emails for future delivery by adding a `scheduled_at` field with an RFC 3339 formatted datetime string.

```csharp
var apiKey = "YOUR_API_KEY";
var client = new MailerooClient(apiKey);

var from = new EmailAddress("sender@example.com", "Your Company");
var to = new EmailAddress("example@example.com", "John Doe");

try
{

    var payload = new Dictionary<string, object?>
    {
        ["from"] = from,
        ["to"] = new List<EmailAddress> { to },
        ["subject"] = "Hello from Maileroo .NET 6",
        ["html"] = "<h1>Hello World</h1><p>This came from the C# SDK test.</p>",
        ["plain"] = "Hello World (plain text fallback)",
        ["scheduled_at"] = DateTime.UtcNow.AddMinutes(5).ToString("yyyy-MM-ddTHH:mm:ssZ")
    };

    var refId = await client.SendBasicEmailAsync(payload);
    Console.WriteLine($"Email queued! Reference ID: {refId}");
    
}
catch (Exception ex)
{
    Console.WriteLine("Error: " + ex.Message);
}
```

### 7. Managing Scheduled Emails

```csharp
using System.Text.Json;
using Maileroo.DotNet.SDK;

public static class ScheduledEmailExamples
{
    public static async Task Main()
    {
        var client = new MailerooClient("YOUR_API_KEY");

        // Page through results
        var page = 1;
        while (true)
        {
            var data = await client.GetScheduledEmailsAsync(page: page, perPage: 10);

            var currentPage = data["page"] is JsonElement p ? p.GetInt32() : 0;
            var perPage = data["per_page"] is JsonElement pp ? pp.GetInt32() : 0;
            var totalCount = data["total_count"] is JsonElement tc ? tc.GetInt32() : 0;
            var totalPages = data["total_pages"] is JsonElement tp ? tp.GetInt32() : 0;

            if (data["results"] is JsonElement results && results.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in results.EnumerateArray())
                {
                    var referenceId = item.GetProperty("reference_id").GetString();
                    var subject = item.GetProperty("subject").GetString();
                    var scheduledAt = item.GetProperty("scheduled_at").GetString();

                    Console.WriteLine($"- {referenceId} | {scheduledAt} | {subject}");
                }
            }

            if (currentPage >= totalPages) break;
            page++;
        }
    }
}
```

### 8. Deleting Scheduled Email

```csharp
var client = new MailerooClient("YOUR_API_KEY");

var referenceId = "0123456789abcdef012345"; // 24-char hex
var ok = await client.DeleteScheduledEmailAsync(referenceId);
Console.WriteLine(ok ? "Deleted." : "Not deleted.");
```

## MailerooClient

### Constructor

``` csharp
new MailerooClient(string apiKey, TimeSpan? timeout = null, HttpClient? httpClient = null)
```

- Adds `Authorization: Bearer <apiKey>` and `User-Agent: MailerooClient/1.0` to the provided or internal `HttpClient`.
- `timeout` defaults to 30 seconds if not supplied.

### Methods

- `Task<string> SendBasicEmailAsync(Dictionary<string, object?> data, CancellationToken ct = default)`\
  Sends a basic email (HTML and/or plain text). Returns the
  `reference_id`.

- `Task<string> SendTemplatedEmailAsync(Dictionary<string, object?> data, CancellationToken ct = default)`\
  Sends a template-based email (`template_id` + optional
  `template_data`). Returns the `reference_id`.

- `Task<IReadOnlyList<string>> SendBulkEmailsAsync(Dictionary<string, object?> data, CancellationToken ct = default)`\
  Sends up to 500 messages in one call (basic or templated). Returns a
  list of `reference_id`s.

- `Task<bool> DeleteScheduledEmailAsync(string referenceId, CancellationToken ct = default)`\
  Cancels a scheduled email by its `reference_id`. Returns `true` on
  success.

- `Task<Dictionary<string, object?>> GetScheduledEmailsAsync(int page = 1, int perPage = 10, CancellationToken ct = default)`\
  Retrieves scheduled emails with pagination. Returns the decoded
  `data` object from the API.

- `string GetReferenceId()`\
  Generates a 24-char hex `reference_id`.

### Exceptions

- Throws `ArgumentException` for invalid inputs (missing fields, wrong types, invalid `reference_id`, oversized subject, etc.).
- Throws `InvalidOperationException` when the API response is malformed or indicates failure.

## EmailAddress

### Constructor

``` csharp
new EmailAddress(string address, string? displayName = null)
```

- Validates `address` using `System.Net.Mail.MailAddress`.
- `displayName` may be `null` or non-empty.

> Use instances of `EmailAddress` in the `from`, `to`, `cc`, `bcc`, and `reply_to` fields of request dictionaries.

## Attachment

Static methods:

```csharp
static Attachment FromFile(string path, string? contentType = null, bool inline = false)
```

Loads and base64-encodes a file. If `contentType` is `null`, it's detected from the file extension (fallback `application/octet-stream`).

```csharp
static Attachment FromContent(string fileName, string content, string? contentType = null, bool inline = false, bool isBase64 = false) 
````

Uses the provided string as content. If `isBase64` is `true`, `content` must be valid Base64; otherwise it is UTF-8 encoded. `contentType` defaults as above.

> Pass `Attachment` instances via the `attachments` collection in request dictionaries.

## Documentation

For detailed API documentation, including all available endpoints, parameters, and response formats, please refer to the [Maileroo API Documentation](https://maileroo.com/docs).

## License

This SDK is released under the MIT License.

## Support

Please visit our [support page](https://maileroo.com/contact-form) for any issues or questions regarding Maileroo. If you find any bugs or have feature requests, feel free to open an issue on our GitHub repository.