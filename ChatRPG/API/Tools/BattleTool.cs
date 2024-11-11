using System.Text;
using System.Text.Json;
using ChatRPG.Data.Models;
using LangChain.Chains.StackableChains.Agents.Tools;

namespace ChatRPG.API.Tools;

public class BattleTool(
    IConfiguration configuration,
    Campaign campaign,
    ToolUtilities utilities,
    string name,
    string? description = null) : AgentTool(name, description)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private static readonly Dictionary<string, double> HitChance = new()
    {
        { "high", 0.9 },
        { "medium", 0.5 },
        { "low", 0.3 },
        { "impossible", 0.01 }
    };

    private static readonly Dictionary<string, (int, int)> DamageRanges = new()
    {
        { "harmless", (0, 1) },
        { "low", (5, 10) },
        { "medium", (10, 20) },
        { "high", (15, 25) },
        { "extraordinary", (25, 80) }
    };

    public override async Task<string> ToolTask(string input, CancellationToken token = new CancellationToken())
    {
        try
        {
            var battleInput = JsonSerializer.Deserialize<BattleInput>(ResponseCleaner.RemoveMarkdown(input), JsonOptions) ??
                              throw new JsonException("Failed to deserialize");
            var instruction = configuration.GetSection("SystemPrompts").GetValue<string>("BattleInstruction")!;

            if (!battleInput.IsValid(out var errors))
            {
                var errorMessage = new StringBuilder();
                errorMessage.Append(
                    "Invalid input provided for the battle. Please provide valid input and correct the following errors:\n");
                foreach (var validationError in errors)
                {
                    errorMessage.Append(validationError + "\n");
                }

                return errorMessage.ToString();
            }

            var participant1 = await utilities.FindCharacter(campaign,
                $"{{\"name\": \"{battleInput.Participant1!.Name}\", " +
                $"\"description\": \"{battleInput.Participant1.Description}\"}}", instruction);
            var participant2 = await utilities.FindCharacter(campaign,
                $"{{\"name\": \"{battleInput.Participant2!.Name}\", " +
                $"\"description\": \"{battleInput.Participant2.Description}\"}}", instruction);

            // Create dummy characters if they do not exist and pray that the archive chain will update them
            participant1 ??= new Character(campaign, campaign.Player.Environment, CharacterType.Humanoid,
                battleInput.Participant1.Name!, battleInput.Participant1.Description!, false);
            participant2 ??= new Character(campaign, campaign.Player.Environment, CharacterType.Humanoid,
                battleInput.Participant2.Name!, battleInput.Participant2.Description!, false);

            var firstHitter = DetermineFirstHitter(participant1, participant2);

            Character secondHitter;
            string firstHitChance;
            string secondHitChance;
            string firstHitSeverity;
            string secondHitSeverity;

            if (firstHitter == participant1)
            {
                secondHitter = participant2;
                firstHitChance = battleInput.Participant1HitChance!;
                secondHitChance = battleInput.Participant2HitChance!;
                firstHitSeverity = battleInput.Participant1DamageSeverity!;
                secondHitSeverity = battleInput.Participant2DamageSeverity!;
            }
            else
            {
                secondHitter = participant1;
                firstHitChance = battleInput.Participant2HitChance!;
                secondHitChance = battleInput.Participant1HitChance!;
                firstHitSeverity = battleInput.Participant2DamageSeverity!;
                secondHitSeverity = battleInput.Participant1DamageSeverity!;
            }

            return ResolveCombat(firstHitter, secondHitter, firstHitChance, secondHitChance, firstHitSeverity,
                       secondHitSeverity) + $" {firstHitter.Name} and {secondHitter.Name}'s battle has " +
                   "been resolved and this pair can not be used for the battle tool again.";
        }
        catch (Exception)
        {
            return
                "Could not execute the battle. Tool input format was invalid. " +
                "Please provide the input in valid JSON.";
        }
    }

    private static string ResolveCombat(Character firstHitter, Character secondHitter, string firstHitChance,
        string secondHitChance, string firstHitSeverity, string secondHitSeverity)
    {
        var resultString = $"{firstHitter.Name} described as \"{firstHitter.Description}\" fights " +
                           $"{secondHitter.Name} described as \"{secondHitter.Description}\"\n";


        resultString += ResolveAttack(firstHitter, secondHitter, firstHitChance, firstHitSeverity);
        if (secondHitter.CurrentHealth <= 0)
        {
            return resultString;
        }

        resultString += " " + ResolveAttack(secondHitter, firstHitter, secondHitChance, secondHitSeverity);

        return resultString;
    }

    private static string ResolveAttack(Character damageDealer, Character damageTaker, string hitChance,
        string hitSeverity)
    {
        var resultString = string.Empty;
        Random rand = new Random();
        var doesAttackHit = rand.NextDouble() <= HitChance[hitChance];

        if (doesAttackHit)
        {
            var (minDamage, maxDamage) = DamageRanges[hitSeverity];
            var damage = rand.Next(minDamage, maxDamage);
            resultString += $"{damageDealer.Name} deals {damage} damage to {damageTaker.Name}. ";
            if (damageTaker.AdjustHealth(-damage))
            {
                if (damageTaker.IsPlayer)
                {
                    return resultString + $"The player {damageTaker.Name} has no remaining health points. " +
                           "Their adventure is over. No more actions can be taken.";
                }

                return resultString +
                       $"With no health points remaining, {damageTaker.Name} dies and can no longer " +
                       "perform actions in the narrative.";
            }

            return resultString +
                   $"They have {damageTaker.CurrentHealth} health points out of {damageTaker.MaxHealth} remaining.";
        }

        return $"{damageDealer.Name} misses their attack on {damageTaker.Name}.";
    }

    private static Character DetermineFirstHitter(Character participant1, Character participant2)
    {
        var rand = new Random();
        var firstHitRoll = rand.NextDouble();

        return (participant1.Type - participant2.Type) switch
        {
            0 => firstHitRoll <= 0.5 ? participant1 : participant2,
            1 => firstHitRoll <= 0.4 ? participant1 : participant2,
            2 => firstHitRoll <= 0.3 ? participant1 : participant2,
            >= 3 => firstHitRoll <= 0.2 ? participant1 : participant2,
            -1 => firstHitRoll <= 0.6 ? participant1 : participant2,
            -2 => firstHitRoll <= 0.7 ? participant1 : participant2,
            <= -3 => firstHitRoll <= 0.8 ? participant1 : participant2
        };
    }
}
