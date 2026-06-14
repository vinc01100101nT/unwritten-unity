using UnityEngine;

/// <summary>
/// Marks a unit the player can RIGHT-CLICK-TO-FOLLOW (an ally now, another player
/// later) instead of attacking. Add this to any friendly character; monsters must
/// NOT have it. <see cref="PlayerAttacker.FriendlyUnitUnderCursor"/> looks for this
/// component so <see cref="PlayerCommander"/> can route a right-click to "follow"
/// rather than "attack".
/// </summary>
public class FriendlyUnit : MonoBehaviour { }
