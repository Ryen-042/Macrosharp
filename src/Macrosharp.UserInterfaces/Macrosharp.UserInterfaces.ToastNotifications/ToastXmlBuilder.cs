using Windows.Data.Xml.Dom;

namespace Macrosharp.UserInterfaces.ToastNotifications;

/// <summary>
/// Converts a <see cref="ToastNotificationContent"/> model into a WinRT
/// <see cref="XmlDocument"/> conforming to the Windows toast XML schema.
/// </summary>
internal static class ToastXmlBuilder
{
    /// <summary>
    /// Builds a toast XML document from the given content model.
    /// </summary>
    public static XmlDocument Build(ToastNotificationContent content)
    {
        var doc = new XmlDocument();
        doc.LoadXml(BuildXmlString(content));
        return doc;
    }

    private static string BuildXmlString(ToastNotificationContent content)
    {
        var sb = new System.Text.StringBuilder();

        // <toast> root element
        sb.Append("<toast");
        AppendDurationAttribute(sb, content.Duration);
        AppendScenarioAttribute(sb, content.Scenario);
        AppendTimestampAttribute(sb, content.Timestamp);
        sb.Append('>');

        // <visual>
        sb.Append("<visual><binding template=\"ToastGeneric\">");

        // Title and body text
        sb.Append("<text>").Append(EscapeXml(content.Title)).Append("</text>");
        sb.Append("<text>").Append(EscapeXml(content.Body)).Append("</text>");

        // Attribution text
        if (!string.IsNullOrWhiteSpace(content.Attribution))
        {
            sb.Append("<text placement=\"attribution\">")
              .Append(EscapeXml(content.Attribution))
              .Append("</text>");
        }

        // App logo override image
        if (!string.IsNullOrWhiteSpace(content.AppLogoPath))
        {
            sb.Append("<image placement=\"appLogoOverride\" src=\"file:///")
              .Append(EscapeXml(content.AppLogoPath!.Replace('\\', '/')))
              .Append("\" hint-crop=\"circle\" />");
        }

        // Hero image
        if (!string.IsNullOrWhiteSpace(content.HeroImagePath))
        {
            sb.Append("<image placement=\"hero\" src=\"file:///")
              .Append(EscapeXml(content.HeroImagePath!.Replace('\\', '/')))
              .Append("\" />");
        }

        // Progress bar
        if (content.ProgressBar is not null)
        {
            AppendProgressBar(sb, content.ProgressBar);
        }

        sb.Append("</binding></visual>");

        // <actions> — action buttons
        if (content.Actions is { Count: > 0 })
        {
            sb.Append("<actions>");
            foreach (var action in content.Actions)
            {
                sb.Append("<action content=\"")
                  .Append(EscapeXml(action.Label))
                  .Append("\" arguments=\"")
                  .Append(EscapeXml(action.Argument))
                  .Append("\" />");
            }
            sb.Append("</actions>");
        }

        sb.Append("</toast>");
        return sb.ToString();
    }

    private static void AppendDurationAttribute(System.Text.StringBuilder sb, ToastDuration duration)
    {
        if (duration == ToastDuration.Long)
        {
            sb.Append(" duration=\"long\"");
        }
    }

    private static void AppendScenarioAttribute(System.Text.StringBuilder sb, ToastScenario scenario)
    {
        string? value = scenario switch
        {
            ToastScenario.Alarm => "alarm",
            ToastScenario.Reminder => "reminder",
            ToastScenario.IncomingCall => "incomingCall",
            _ => null
        };

        if (value is not null)
        {
            sb.Append(" scenario=\"").Append(value).Append('"');
        }
    }

    private static void AppendTimestampAttribute(System.Text.StringBuilder sb, DateTimeOffset? timestamp)
    {
        if (timestamp.HasValue)
        {
            sb.Append(" displayTimestamp=\"")
              .Append(timestamp.Value.ToString("yyyy-MM-ddTHH:mm:ssZ"))
              .Append('"');
        }
    }

    private static void AppendProgressBar(System.Text.StringBuilder sb, ToastProgressBar progressBar)
    {
        sb.Append("<progress");

        if (!string.IsNullOrWhiteSpace(progressBar.Title))
        {
            sb.Append(" title=\"").Append(EscapeXml(progressBar.Title)).Append('"');
        }

        if (progressBar.Value.HasValue)
        {
            sb.Append(" value=\"").Append(progressBar.Value.Value.ToString("F2")).Append('"');
        }
        else
        {
            sb.Append(" value=\"indeterminate\"");
        }

        if (!string.IsNullOrWhiteSpace(progressBar.ValueStringOverride))
        {
            sb.Append(" valueStringOverride=\"")
              .Append(EscapeXml(progressBar.ValueStringOverride))
              .Append('"');
        }

        if (!string.IsNullOrWhiteSpace(progressBar.Status))
        {
            sb.Append(" status=\"").Append(EscapeXml(progressBar.Status)).Append('"');
        }

        sb.Append(" />");
    }

    private static string EscapeXml(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        return value
            .Replace("&", "&amp;")
            .Replace("<", "&lt;")
            .Replace(">", "&gt;")
            .Replace("\"", "&quot;")
            .Replace("'", "&apos;");
    }
}
