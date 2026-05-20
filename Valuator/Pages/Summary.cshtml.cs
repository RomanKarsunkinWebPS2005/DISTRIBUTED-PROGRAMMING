using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Valuator.Pages;
public class SummaryModel : PageModel
{
    private readonly ILogger<SummaryModel> _logger;
    private readonly IConnectionMultiplexer _redis;

    public SummaryModel(ILogger<SummaryModel> logger, IConnectionMultiplexer redis)
    {
        _logger = logger;
        _redis = redis;
    }

    public double Rank { get; set; }
    public double Similarity { get; set; }

    public void OnGet(string id)
    {
        _logger.LogDebug(id);

        if (string.IsNullOrEmpty(id))
            return;

        var db = _redis.GetDatabase();

        RedisValue rankVal = db.StringGet("RANK-" + id);
        if (rankVal.HasValue && double.TryParse(rankVal!, NumberStyles.Float, CultureInfo.InvariantCulture, out double r))
            Rank = r;

        RedisValue simVal = db.StringGet("SIMILARITY-" + id);
        if (simVal.HasValue && double.TryParse(simVal!, NumberStyles.Float, CultureInfo.InvariantCulture, out double s))
            Similarity = s;
    }
}
