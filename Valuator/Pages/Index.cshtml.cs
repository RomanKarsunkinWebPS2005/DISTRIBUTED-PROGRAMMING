using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using StackExchange.Redis;

namespace Valuator.Pages;

public class IndexModel : PageModel
{
    private const string ProcessedTextHashesKey = "PROCESSED-TEXT-HASHES";

    private readonly ILogger<IndexModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public IndexModel(ILogger<IndexModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public void OnGet()
    {

    }

    public IActionResult OnPost(string text)
    {
        text ??= string.Empty;
        _logger.LogDebug(text);

        string id = Guid.NewGuid().ToString();
        var db = _redis.GetDatabase();

        string textKey = "TEXT-" + id;
        db.StringSet(textKey, text);

        string rankKey = "RANK-" + id;
        double rank = CalculateRank(text);
        db.StringSet(rankKey, rank.ToString(CultureInfo.InvariantCulture));

        string similarityKey = "SIMILARITY-" + id;
        string hashHex = ComputeSha256Hex(text);
        bool wasDuplicate = db.SetContains(ProcessedTextHashesKey, hashHex);
        double similarity = wasDuplicate ? 1.0 : 0.0;
        db.SetAdd(ProcessedTextHashesKey, hashHex);
        db.StringSet(similarityKey, similarity.ToString(CultureInfo.InvariantCulture));

        return Redirect($"summary?id={id}");
    }

    /// <summary>
    /// Доля символов, которые не являются буквами латинского или русского алфавита.
    /// </summary>
    private static double CalculateRank(string text)
    {
        if (text.Length == 0)
            return 0;

        int nonAlphabetic = 0;
        foreach (char c in text)
        {
            if (!IsLatinOrRussianLetter(c))
                nonAlphabetic++;
        }

        return (double)nonAlphabetic / text.Length;
    }

    private static bool IsLatinOrRussianLetter(char c)
    {
        if (c is >= 'A' and <= 'Z' or >= 'a' and <= 'z')
            return true;

        if (c is >= '\u0400' and <= '\u04FF' && char.IsLetter(c))
            return true;

        return false;
    }

    private static string ComputeSha256Hex(string text)
    {
        byte[] hash = SHA256.HashData(Encoding.UTF8.GetBytes(text));
        return Convert.ToHexString(hash);
    }
}
