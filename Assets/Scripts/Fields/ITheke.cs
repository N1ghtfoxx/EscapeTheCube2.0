// made by Naomi in collaboration with Claude Ai


/// <summary>
/// Marker interface for all Theke fields
/// Allows the Secret Passage card effect to locate any Theke field
/// without depending on a specific subclass
///
/// Usage: Attach this interface to any Field subclass that represents a Theke
/// Currently implemented by Theke1 and Theke2
/// </summary>
public interface ITheke
{
    // Empty marker interface — no methods required
    // Identification via 'field is ITheke' is sufficient
}