using UnityEngine;

/// <summary>
/// Stores character appearance data (sprite, name, etc.)
/// </summary>

[CreateAssetMenu(fileName = "Character", menuName = "Game/Character Data")]
public class CharacterData : ScriptableObject
{
    [Header("Visuals")]
    public Sprite characterSprite;
    public string characterName;

    [Header("Optional - Stats")]
    [Tooltip("Optional: Different starting hydration per character")]
    public int startingHydration = 10;

    [Tooltip("Optional: Character description for UI")]
    [TextArea(2, 4)]
    public string description;
}
