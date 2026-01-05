using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Viblog.Shared.Services;

/// <summary>
/// Provides text manipulation and formatting utilities
/// </summary>
public class TextUtilities : ITextUtilities
{
    /// <summary>
    /// Converts text to a URL-friendly slug
    /// </summary>
    /// <param name="text">The text to slugify</param>
    /// <returns>A URL-friendly slug (lowercase, hyphens, alphanumeric only), or null if the resulting slug would be empty</returns>
    public virtual string? Slugify(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        // Remove diacritics (accents) and convert to ASCII
        text = RemoveDiacritics(text);

        // Convert to lowercase
        text = text.ToLowerInvariant();

        // Replace spaces with hyphens
        text = Regex.Replace(text, @"\s+", "-");

        // Remove invalid characters (keep only a-z, 0-9, and hyphens)
        text = Regex.Replace(text, @"[^a-z0-9\-]", "");

        // Remove multiple consecutive hyphens
        text = Regex.Replace(text, @"-+", "-");

        // Trim hyphens from start and end
        text = text.Trim('-');

        // Return null if the result is empty (e.g., input was only unmappable characters)
        return string.IsNullOrEmpty(text) ? null : text;
    }

    /// <summary>
    /// Removes diacritics (accent marks) from characters, converting them to their ASCII equivalents
    /// </summary>
    /// <param name="text">The text to process</param>
    /// <returns>Text with diacritics removed</returns>
    protected virtual string RemoveDiacritics(string text)
    {
        var stringBuilder = new StringBuilder(text.Length);

        // First pass: handle special characters that don't decompose well
        foreach (var c in text)
        {
            var replacement = GetSpecialCharacterReplacement(c);
            if (replacement != null)
            {
                stringBuilder.Append(replacement);
            }
            else
            {
                stringBuilder.Append(c);
            }
        }

        text = stringBuilder.ToString();
        stringBuilder.Clear();

        // Second pass: normalize to Form D (decomposed form) which separates base characters from diacritics
        var normalizedString = text.Normalize(NormalizationForm.FormD);

        // Iterate through each character
        foreach (var c in normalizedString)
        {
            // Get the Unicode category of the character
            var unicodeCategory = CharUnicodeInfo.GetUnicodeCategory(c);
            
            // Skip non-spacing marks (diacritics/accents)
            if (unicodeCategory != UnicodeCategory.NonSpacingMark)
            {
                stringBuilder.Append(c);
            }
        }

        // Normalize back to Form C (composed form)
        return stringBuilder.ToString().Normalize(NormalizationForm.FormC);
    }

    /// <summary>
    /// Gets ASCII replacement for special characters that don't decompose well
    /// </summary>
    /// <param name="c">The character to check</param>
    /// <returns>Replacement string if character is special, null otherwise</returns>
    protected virtual string? GetSpecialCharacterReplacement(char c)
    {
        return c switch
        {
            // Polish
            '\u0141' => "L",  // ? - L with stroke (uppercase)
            '\u0142' => "l",  // ? - l with stroke (lowercase)
            
            // Scandinavian
            '\u00D8' => "O",  // Ø - O with stroke (uppercase)
            '\u00F8' => "o",  // ø - o with stroke (lowercase)
            '\u00C5' => "A",  // Å - A with ring above (uppercase)
            '\u00E5' => "a",  // å - a with ring above (lowercase)
            '\u00C6' => "AE", // Æ - AE ligature (uppercase)
            '\u00E6' => "ae", // æ - ae ligature (lowercase)
            
            // German
            '\u00DF' => "ss", // ß - sharp s
            
            // Other common characters
            '\u0152' => "OE", // Œ - OE ligature (uppercase)
            '\u0153' => "oe", // œ - oe ligature (lowercase)
            '\u00D0' => "D",  // Ð - Eth (uppercase)
            '\u00F0' => "d",  // ð - eth (lowercase)
            '\u00DE' => "TH", // Þ - Thorn (uppercase)
            '\u00FE' => "th", // þ - thorn (lowercase)
            
            _ => null
        };
    }
}
