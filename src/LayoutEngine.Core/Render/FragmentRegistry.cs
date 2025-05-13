namespace LayoutEngine.Core.Render;

using System;
using System.Collections.Generic;
using System.Linq;
using Layout;

/// <summary>
/// Manages the registration and tracking of layout fragments.
/// </summary>
public class FragmentRegistry
{
    private readonly Dictionary<ILayoutFragment, string> _fragmentToIdMap = new();
    private readonly Dictionary<string, ILayoutFragment> _idToFragmentMap = new();

    /// <summary>
    /// Registers a fragment and returns its status.
    /// </summary>
    public FragmentStatus RegisterFragment(ILayoutFragment fragment)
    {
        if (_fragmentToIdMap.TryGetValue(fragment, out var existingId))
        {
            return new FragmentStatus(existingId, false);
        }

        var newId = Guid.NewGuid().ToString();
        _fragmentToIdMap[fragment] = newId;
        _idToFragmentMap[newId] = fragment;

        return new FragmentStatus(newId, true);
    }

    /// <summary>
    /// Gets all fragment IDs that were previously registered but not in the provided set.
    /// </summary>
    public IEnumerable<string> GetRemovedFragmentIds(HashSet<string> currentFragmentIds)
    {
        return _idToFragmentMap.Keys.Where(id => !currentFragmentIds.Contains(id)).ToList();
    }

    /// <summary>
    /// Unregisters a fragment by its ID.
    /// </summary>
    public void UnregisterFragmentById(string id)
    {
        if (_idToFragmentMap.TryGetValue(id, out var fragment))
        {
            _fragmentToIdMap.Remove(fragment);
            _idToFragmentMap.Remove(id);
        }
    }

    /// <summary>
    /// Gets the ID for a fragment, if registered.
    /// </summary>
    public string? GetFragmentId(ILayoutFragment fragment)
    {
        return _fragmentToIdMap.TryGetValue(fragment, out var id) ? id : null;
    }

    /// <summary>
    /// Gets the fragment for an ID, if registered.
    /// </summary>
    public ILayoutFragment? GetFragmentById(string id)
    {
        return _idToFragmentMap.TryGetValue(id, out var fragment) ? fragment : null;
    }
}