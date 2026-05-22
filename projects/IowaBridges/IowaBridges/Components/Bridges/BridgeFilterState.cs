using System.Text;

namespace IowaBridges.Components.Bridges;

/// <summary>
/// Current values of the sidebar filters. Always combines with the
/// Iowa state-code clause when producing a definition expression.
/// </summary>
public sealed class BridgeFilterState
{
    public bool ShowGood { get; set; } = true;
    public bool ShowFair { get; set; } = true;
    public bool ShowPoor { get; set; } = true;

    public int YearMin { get; set; } = 1900;
    public int YearMax { get; set; } = 2025;

    /// <summary>3-digit Iowa county FIPS subcode, or null for all counties.</summary>
    public string? CountyFips { get; set; }

    public const string IowaStateClause = "STATE_CODE_001 = '19'";

    /// <summary>
    /// Builds a single SQL WHERE expression suitable for FeatureLayer.SetDefinitionExpression.
    /// Always prefixed with the Iowa state clause so the layer never shows other states.
    /// </summary>
    public string ToDefinitionExpression()
    {
        var sb = new StringBuilder();
        sb.Append('(').Append(IowaStateClause).Append(')');

        var conditionClause = BuildConditionClause();
        if (conditionClause is not null)
        {
            sb.Append(" AND (").Append(conditionClause).Append(')');
        }

        // Year (always applied since the slider has a baseline range)
        sb.Append(" AND (YEAR_BUILT_027 BETWEEN ")
          .Append(YearMin).Append(" AND ").Append(YearMax).Append(')');

        if (!string.IsNullOrEmpty(CountyFips))
        {
            sb.Append(" AND (COUNTY_CODE_003 = '").Append(CountyFips).Append("')");
        }

        return sb.ToString();
    }

    private string? BuildConditionClause()
    {
        // If all three are off, return a clause that matches nothing (so user sees empty state).
        if (!ShowGood && !ShowFair && !ShowPoor)
        {
            return "1 = 0";
        }
        // If all three are on, don't constrain on condition.
        if (ShowGood && ShowFair && ShowPoor)
        {
            return null;
        }

        // Build allowed code list from condition fields. Composite condition = min of the three,
        // where 'N' means "not applicable" (commonly culverts) — we treat as 99/unknown and skip.
        // The Arcade renderer maps:
        //   0-4 -> Poor, 5-6 -> Fair, 7-9 -> Good
        // For server-side filtering, mirror that by requiring at least one of the three condition
        // fields to fall in the matching code set. (Approximate but readable; exact min requires
        // server Arcade which the NBI service does not support.)

        var allowed = new List<string>();
        if (ShowPoor) allowed.AddRange(new[] { "0", "1", "2", "3", "4" });
        if (ShowFair) allowed.AddRange(new[] { "5", "6" });
        if (ShowGood) allowed.AddRange(new[] { "7", "8", "9" });

        var quoted = string.Join(",", allowed.Select(c => $"'{c}'"));
        return
            $"DECK_COND_058 IN ({quoted}) OR " +
            $"SUPERSTRUCTURE_COND_059 IN ({quoted}) OR " +
            $"SUBSTRUCTURE_COND_060 IN ({quoted})";
    }
}
