namespace ChatRPG.Services;

public enum SystemPromptType
{
    // For example, CombatHitMiss defines the systemprompt where the player hits with their attack and the opponent misses.
    Initial, DoAction, SayAction, CombatHitHit, CombatHitMiss, CombatMissHit, CombatMissMiss, CombatOpponentDescription, HurtOrHeal
}
