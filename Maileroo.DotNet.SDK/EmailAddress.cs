using System.Net.Mail;

namespace Maileroo.DotNet.SDK;

public sealed class EmailAddress
{
    private string Address { get; }
    private string? DisplayName { get; }

    public EmailAddress(string address, string? displayName = null)
    {
        if (string.IsNullOrWhiteSpace(address))
            throw new ArgumentException("Email address must be a non-empty string.", nameof(address));

        // Use MailAddress for basic validation (does not guarantee deliverability)
        try { _ = new MailAddress(address); }
        catch { throw new ArgumentException($"Invalid email address format: {address}", nameof(address)); }

        if (displayName != null && string.IsNullOrWhiteSpace(displayName))
            throw new ArgumentException("Display name must be a non-empty string or null.", nameof(displayName));

        Address = address;
        DisplayName = displayName;
    }

    internal Dictionary<string, object?> ToApi()
    {
        var dict = new Dictionary<string, object?> { ["address"] = Address };
        if (DisplayName != null) dict["display_name"] = DisplayName;
        return dict;
    }
}