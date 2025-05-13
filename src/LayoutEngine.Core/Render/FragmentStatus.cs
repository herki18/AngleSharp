namespace LayoutEngine.Core.Render;

/// <summary>
/// Represents the status of a fragment after registration.
/// </summary>
public class FragmentStatus
{
    /// <summary>
    /// The unique identifier for the fragment.
    /// </summary>
    public string Id { get; }

    /// <summary>
    /// Whether this is a newly registered fragment.
    /// </summary>
    public bool IsNew { get; }

    public FragmentStatus(string id, bool isNew)
    {
        Id = id;
        IsNew = isNew;
    }
}