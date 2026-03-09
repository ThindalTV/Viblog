using ColorCode;
using ColorCode.Common;
using Markdig;
using Viblog.Infrastructure.Services;

namespace Viblog.Shared.Services;

/// <summary>
/// Provides markdown rendering services using the Markdig library
/// </summary>
public class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;
    private readonly HtmlFormatter _codeFormatter;
    private readonly HtmlClassFormatter _codeClassFormatter;

    /// <summary>
    /// Initializes a new instance of the <see cref="MarkdownService"/> class
    /// </summary>
    public MarkdownService()
    {
        // Configure the Markdig pipeline with advanced features
        _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions() // Enables tables, task lists, auto-links, etc.
            .UseEmojiAndSmiley()     // Enables emoji support
            .UseSoftlineBreakAsHardlineBreak() // Converts single line breaks to <br>
            .Build();

        // Initialize ColorCode formatters for syntax highlighting
        _codeFormatter = new HtmlFormatter();
        _codeClassFormatter = new HtmlClassFormatter();
    }

    /// <summary>
    /// Converts markdown text to HTML
    /// </summary>
    /// <param name="markdown">The markdown content to render</param>
    /// <returns>The rendered HTML content, or empty string if markdown is null or whitespace</returns>
    public string RenderToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        var html = Markdown.ToHtml(markdown, _pipeline);

        // Post-process to add syntax highlighting using ColorCode
        return ApplySyntaxHighlighting(html);
    }

    /// <summary>
    /// Applies syntax highlighting to code blocks in the HTML
    /// </summary>
    private string ApplySyntaxHighlighting(string html)
    {
        // Use regex to find code blocks with language specification
        // Pattern: <code class="language-{lang}">...code...</code>
        var pattern = @"<code class=""language-(\w+)"">(.*?)</code>";

        return System.Text.RegularExpressions.Regex.Replace(
            html,
            pattern,
            match =>
            {
                var language = match.Groups[1].Value;
                var code = System.Net.WebUtility.HtmlDecode(match.Groups[2].Value);

                try
                {
                    // Map markdown language names to ColorCode language IDs
                    var languageId = MapLanguageId(language);
                    if (!string.IsNullOrEmpty(languageId))
                    {
                        var colorCodeLanguage = Languages.FindById(languageId);
                        if (colorCodeLanguage != null)
                        {
                            // Use ColorCode to highlight the code
                            return _codeFormatter.GetHtmlString(code, colorCodeLanguage);
                        }
                    }
                }
                catch
                {
                    // If highlighting fails, return original
                }

                // Return original if language not supported
                return match.Value;
            },
            System.Text.RegularExpressions.RegexOptions.Singleline);
    }

    /// <summary>
    /// Maps markdown language identifiers to ColorCode language IDs
    /// </summary>
    private string? MapLanguageId(string language)
    {
        return language.ToLowerInvariant() switch
        {
            "csharp" or "cs" or "c#" => LanguageId.CSharp,
            "html" or "htm" => LanguageId.Html,
            "xml" => LanguageId.Xml,
            "javascript" or "js" => LanguageId.JavaScript,
            "typescript" or "ts" => LanguageId.TypeScript,
            "css" => LanguageId.Css,
            "sql" => LanguageId.Sql,
            "java" => LanguageId.Java,
            "cpp" or "c++" => LanguageId.Cpp,
            "php" => LanguageId.Php,
            "python" or "py" => LanguageId.Python,
            "vb" or "vbnet" => LanguageId.VbDotNet,
            "powershell" or "ps1" => LanguageId.PowerShell,
            "json" => LanguageId.JavaScript, // ColorCode doesn't have JSON, use JS
            "yaml" or "yml" => LanguageId.Xml, // Use XML for YAML
            _ => null
        };
    }
}
