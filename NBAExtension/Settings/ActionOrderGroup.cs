// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Linq;

namespace NBAExtension.Settings;

/// <summary>
/// A command a user can place in a list page's command order.
/// </summary>
/// <param name="Key">Stable identifier persisted to settings.json.</param>
/// <param name="Title">Display name shown in the settings dropdowns.</param>
internal readonly record struct ActionChoice(string Key, string Title);

/// <summary>
/// Non-generic view of an <see cref="ActionOrderGroup{T}"/> used by the settings form.
/// </summary>
internal interface IActionOrderGroup
{
    /// <summary>Gets the stable id of the group (also the settings.json key).</summary>
    string Id { get; }

    /// <summary>Gets the list page name shown as the group heading.</summary>
    string Title { get; }

    /// <summary>Gets the subtitle shown under the group heading.</summary>
    string Description { get; }

    /// <summary>Gets every command in the group, in default order.</summary>
    IReadOnlyList<ActionChoice> Choices { get; }

    /// <summary>Gets the keys of the current order.</summary>
    IReadOnlyList<string> CurrentKeys { get; }

    /// <summary>Gets a value indicating whether the current order matches the default.</summary>
    bool IsDefault { get; }

    /// <summary>
    /// Moves a command into a gap between commands, like dropping a dragged item.
    /// </summary>
    /// <param name="key">The command to move.</param>
    /// <param name="gap">The gap to drop into: 0 is before the first command, Count is after the last.</param>
    /// <returns>True if the order changed.</returns>
    bool MoveTo(string key, int gap);

    /// <summary>Restores the default order.</summary>
    /// <returns>True if the order changed.</returns>
    bool Reset();

    /// <summary>Loads a persisted order, tolerating unknown, duplicate, or missing keys.</summary>
    void Load(IEnumerable<string?>? keys);
}

/// <summary>
/// The user-configurable order of the commands on one list page's items. The first command
/// is the item's primary command (Enter), the second is the secondary command (Ctrl+Enter),
/// and the rest follow in the context menu.
/// </summary>
/// <typeparam name="T">The command enum for the list page.</typeparam>
internal sealed class ActionOrderGroup<T> : IActionOrderGroup
    where T : struct, Enum
{
    private readonly (T Action, ActionChoice Choice)[] _definitions;
    private T[] _order;

    public ActionOrderGroup(string id, string title, string description, params (T Action, string Key, string Title)[] definitions)
    {
        ArgumentException.ThrowIfNullOrEmpty(id);
        if (definitions.Length == 0)
        {
            throw new ArgumentException("A group needs at least one command.", nameof(definitions));
        }

        Id = id;
        Title = title;
        Description = description;
        _definitions = definitions.Select(d => (d.Action, new ActionChoice(d.Key, d.Title))).ToArray();
        Choices = _definitions.Select(d => d.Choice).ToArray();
        _order = DefaultOrder();
    }

    public string Id { get; }

    public string Title { get; }

    public string Description { get; }

    public IReadOnlyList<ActionChoice> Choices { get; }

    /// <summary>Gets the current command order. The array is replaced, never mutated, so readers on other threads see a consistent snapshot.</summary>
    public IReadOnlyList<T> Order => _order;

    public IReadOnlyList<string> CurrentKeys => _order.Select(KeyOf).ToArray();

    public bool IsDefault => _order.SequenceEqual(DefaultOrder());

    /// <summary>
    /// Arranges the commands an item actually has according to the current order. Commands the
    /// item does not offer (e.g. no leader data for a game) are skipped, so the next available
    /// command moves up into their place.
    /// </summary>
    public List<TItem> Arrange<TItem>(IReadOnlyDictionary<T, TItem> available)
    {
        var order = _order;
        var result = new List<TItem>(available.Count);
        foreach (var action in order)
        {
            if (available.TryGetValue(action, out var item))
            {
                result.Add(item);
            }
        }

        return result;
    }

    public bool MoveTo(string key, int gap)
    {
        if (TryParse(key) is not { } action)
        {
            return false;
        }

        var order = _order.ToList();
        var from = order.IndexOf(action);
        gap = Math.Clamp(gap, 0, order.Count);

        // Removing the item first shifts every later gap up by one.
        var to = gap > from ? gap - 1 : gap;
        if (to == from)
        {
            return false;
        }

        order.RemoveAt(from);
        order.Insert(to, action);
        return SetOrder(order.ToArray());
    }

    public bool Reset() => SetOrder(DefaultOrder());

    public void Load(IEnumerable<string?>? keys)
    {
        _order = keys == null ? DefaultOrder() : Normalize(keys);
    }

    private bool SetOrder(T[] order)
    {
        if (order.SequenceEqual(_order))
        {
            return false;
        }

        _order = order;
        return true;
    }

    private T[] Normalize(IEnumerable<string?> keys)
    {
        var result = new List<T>(_definitions.Length);
        foreach (var key in keys)
        {
            if (TryParse(key) is { } action && !result.Contains(action))
            {
                result.Add(action);
            }
        }

        // Commands added in a later version (or missing from a hand-edited file) go at the end.
        foreach (var (action, _) in _definitions)
        {
            if (!result.Contains(action))
            {
                result.Add(action);
            }
        }

        return result.ToArray();
    }

    private T[] DefaultOrder() => _definitions.Select(d => d.Action).ToArray();

    private T? TryParse(string? key)
    {
        foreach (var (action, choice) in _definitions)
        {
            if (string.Equals(choice.Key, key, StringComparison.Ordinal))
            {
                return action;
            }
        }

        return null;
    }

    private string KeyOf(T action)
    {
        foreach (var (candidate, choice) in _definitions)
        {
            if (EqualityComparer<T>.Default.Equals(candidate, action))
            {
                return choice.Key;
            }
        }

        throw new ArgumentOutOfRangeException(nameof(action));
    }
}
