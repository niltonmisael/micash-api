using System.Collections.Concurrent;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Text.Unicode;

namespace MiCash.Infrastructure.Services.Email
{
    /// <summary>
    /// Values of one template render: variables and repeatable/optional sections.
    /// </summary>
    public sealed class EmailTemplateData
    {
        public Dictionary<string , string> Values { get; } = new(StringComparer.Ordinal);

        public Dictionary<string , IReadOnlyList<EmailTemplateData>> Sections { get; } = new(StringComparer.Ordinal);

        public EmailTemplateData Set ( string name , string value )
        {
            Values[name] = value;
            return this;
        }

        /// <summary>The section body is rendered once per item (zero items removes it).</summary>
        public EmailTemplateData Section ( string name , IEnumerable<EmailTemplateData> items )
        {
            Sections[name] = items.ToList();
            return this;
        }

        /// <summary>Optional section: rendered once when <paramref name="visible"/>, removed otherwise.</summary>
        public EmailTemplateData Show ( string name , bool visible ) =>
            Section(name , visible ? [new EmailTemplateData()] : []);
    }

    /// <summary>
    /// Loads the templates embedded in this assembly (Services/Email/Templates, see the .csproj) and fills them.
    /// Embedded resources travel inside MiCash.Infrastructure.dll: no file paths, works the same
    /// locally, after "dotnet publish" and inside the Docker image.
    ///
    /// Syntax (only these two, nothing else is touched):
    ///   {{name}}                      variable; HTML-encoded in .html templates, raw in .txt.
    ///   &lt;!--{{#name}}--&gt; … &lt;!--{{/name}}--&gt;  section repeated per item (the comment wrapper is optional);
    ///                                 inside it, the item's variables come first, then the outer ones.
    /// A variable or section without data is an error: a placeholder never reaches the user.
    /// HTML encoding protects text and attribute values; values placed inside style="" (colors, widths)
    /// must come from the renderer's own constants/numbers, never from user input.
    /// </summary>
    public static class EmailTemplateEngine
    {
        public const string RESOURCE_PREFIX = "EmailTemplates.";

        private static readonly ConcurrentDictionary<string , string> Cache = new(StringComparer.Ordinal);

        private static readonly HtmlEncoder Encoder = HtmlEncoder.Create(UnicodeRanges.All);

        private static readonly Regex SectionPattern = new(
            @"(?:<!--)?\{\{#(?<name>[a-z0-9_]+)\}\}(?:-->)?(?<body>.*?)(?:<!--)?\{\{/\k<name>\}\}(?:-->)?" ,
            RegexOptions.Singleline | RegexOptions.CultureInvariant);

        private static readonly Regex VariablePattern = new(
            @"\{\{(?<name>[a-z0-9_]+)\}\}" ,
            RegexOptions.CultureInvariant);

        /// <summary>File names of all embedded templates (e.g. "03-boas-vindas.html").</summary>
        public static IReadOnlyList<string> AvailableTemplates () =>
            typeof(EmailTemplateEngine).Assembly.GetManifestResourceNames()
                .Where(n => n.StartsWith(RESOURCE_PREFIX , StringComparison.Ordinal))
                .Select(n => n[RESOURCE_PREFIX.Length..])
                .Order(StringComparer.Ordinal)
                .ToList();

        /// <summary>Raw template content. Throws <see cref="InvalidOperationException"/> when it does not exist.</summary>
        public static string Load ( string fileName ) =>
            Cache.GetOrAdd(fileName , static name => {
                using var stream = typeof(EmailTemplateEngine).Assembly.GetManifestResourceStream(RESOURCE_PREFIX + name)
                    ?? throw new InvalidOperationException($"E-mail template '{name}' not found.");

                using var reader = new StreamReader(stream , Encoding.UTF8);
                var template = reader.ReadToEnd();

                EnsureWellFormed(name , template);

                return template;
            });

        public static string Render ( string fileName , EmailTemplateData data )
        {
            var encode = fileName.EndsWith(".html" , StringComparison.OrdinalIgnoreCase);

            return RenderBlock(Load(fileName) , new Scope(data , null) , fileName , encode);
        }

        private static string RenderBlock ( string template , Scope scope , string fileName , bool encode )
        {
            var withSections = SectionPattern.Replace(template , match => {
                var name = match.Groups["name"].Value;
                var items = scope.FindSection(name)
                    ?? throw new InvalidOperationException($"E-mail template '{fileName}': no data for section '{name}'.");

                var body = match.Groups["body"].Value;
                var builder = new StringBuilder();

                foreach ( var item in items )
                    builder.Append(RenderBlock(body , new Scope(item , scope) , fileName , encode));

                return builder.ToString();
            });

            return VariablePattern.Replace(withSections , match => {
                var name = match.Groups["name"].Value;
                var value = scope.FindValue(name)
                    ?? throw new InvalidOperationException($"E-mail template '{fileName}': no value for '{name}'.");

                return encode ? Encoder.Encode(value) : value;
            });
        }

        // Checked on the raw template (once, before any value goes in; a value may contain "{{"):
        // after removing every valid section marker and variable, no "{{" may remain.
        // Catches "{{ nome }}", "{{Nome}}" or a section without its end.
        private static void EnsureWellFormed ( string name , string template )
        {
            var stripped = template;
            string previous;

            do {
                previous = stripped;
                stripped = SectionPattern.Replace(stripped , m => m.Groups["body"].Value);
            } while ( stripped != previous );

            if ( VariablePattern.Replace(stripped , string.Empty).Contains("{{" , StringComparison.Ordinal) )
                throw new InvalidOperationException($"E-mail template '{name}' has malformed placeholders.");
        }

        private sealed record Scope ( EmailTemplateData Data , Scope? Parent )
        {
            public string? FindValue ( string name ) =>
                Data.Values.TryGetValue(name , out var value) ? value : Parent?.FindValue(name);

            public IReadOnlyList<EmailTemplateData>? FindSection ( string name ) =>
                Data.Sections.TryGetValue(name , out var items) ? items : Parent?.FindSection(name);
        }
    }
}
