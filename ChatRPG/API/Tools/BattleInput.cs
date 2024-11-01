namespace ChatRPG.API.Tools;

public class BattleInput
{
    private static readonly HashSet<string> ValidChancesToHit = ["high", "medium", "low", "impossible"];
    private static readonly HashSet<string> ValidDamageSeverities = ["low", "medium", "high", "extraordinary"];

    public CharacterInput? Participant1 { get; set; }
    public CharacterInput? Participant2 { get; set; }
    public string? Participant1HitChance { get; set; }
    public string? Participant2HitChance { get; set; }
    public string? Participant1DamageSeverity { get; set; }
    public string? Participant2DamageSeverity { get; set; }

    public bool IsValid(out List<string> validationErrors)
    {
        validationErrors = [];

        if (Participant1 == null)
        {
            validationErrors.Add("Participant1 is required.");
        }
        else if (!Participant1.IsValidForBattle(out var participant1Errors))
        {
            validationErrors.AddRange(participant1Errors.Select(e => $"Participant1: {e}"));
        }

        if (Participant2 == null)
        {
            validationErrors.Add("Participant2 is required.");
        }
        else if (!Participant2.IsValidForBattle(out var participant2Errors))
        {
            validationErrors.AddRange(participant2Errors.Select(e => $"Participant2: {e}"));
        }

        if (Participant1HitChance != null && !ValidChancesToHit.Contains(Participant1HitChance))
            validationErrors.Add("Participant1ChanceToHit must be one of the following: high, medium, low, impossible.");

        if (Participant2HitChance != null && !ValidChancesToHit.Contains(Participant2HitChance))
            validationErrors.Add("Participant2ChanceToHit must be one of the following: high, medium, low, impossible.");

        if (Participant1DamageSeverity != null && !ValidDamageSeverities.Contains(Participant1DamageSeverity))
            validationErrors.Add("Participant1DamageSeverity must be one of the following: low, medium, high, extraordinary.");

        if (Participant2DamageSeverity != null && !ValidDamageSeverities.Contains(Participant2DamageSeverity))
            validationErrors.Add("Participant2DamageSeverity must be one of the following: low, medium, high, extraordinary.");

        return validationErrors.Count == 0;
    }
}
