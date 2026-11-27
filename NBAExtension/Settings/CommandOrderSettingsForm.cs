// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension.Settings;

/// <summary>
/// Adaptive Card that lets users reorder each list page's commands with a pick-up-and-drop
/// interaction, the closest Adaptive Cards gets to drag and drop.
/// </summary>
/// <remarks>
/// Every command is a row. Clicking a row picks it up: the row highlights and "Move here"
/// drop targets appear in the gaps between the other rows. Clicking a drop target moves the
/// command there and saves immediately; clicking the picked row again cancels. The card has
/// no inputs, so a command can never appear twice. Each click re-renders this form in place
/// by updating <see cref="FormContent.TemplateJson"/>; the page itself is not reloaded.
/// </remarks>
[SuppressMessage("Performance", "CA1859", Justification = "Card helpers return JsonNode on purpose so JsonArray binds to its AOT-safe Add(JsonNode) overload instead of the generic Add<T>.")]
internal sealed partial class CommandOrderSettingsForm : FormContent
{
    private const string Grip = "≡"; // ≡, the classic reorder handle

    private readonly SettingsManager _settings;
    private readonly object _stateLock = new();
    private readonly Dictionary<string, string> _groupNotes = [];
    private string? _pickedGroupId;
    private string? _pickedKey;
    private string? _footerNote;

    public CommandOrderSettingsForm(SettingsManager settings)
    {
        _settings = settings;
        Refresh();
    }

    /// <summary>Drops any in-progress move and confirmation notes, e.g. when the page is reopened.</summary>
    public void ResetTransientState()
    {
        lock (_stateLock)
        {
            ClearTransientState();
            Refresh();
        }
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        var payload = ParseObject(data);
        var op = GetString(payload, "op");
        var group = _settings.Groups.FirstOrDefault(g => g.Id == GetString(payload, "group"));
        var key = GetString(payload, "key");

        lock (_stateLock)
        {
            var wasPicked = group?.Id == _pickedGroupId && key == _pickedKey;
            ClearTransientState();

            switch (op)
            {
                case "pick" when group != null && key != null && !wasPicked:
                    // Clicking the picked row again falls through as a cancel.
                    _pickedGroupId = group.Id;
                    _pickedKey = key;
                    break;

                case "drop" when group != null && key != null && GetInt(payload, "gap") is { } gap:
                    _settings.Update(_ => group.MoveTo(key, gap));
                    var position = group.CurrentKeys.ToList().IndexOf(key);
                    var title = TitleOf(group, key);
                    _groupNotes[group.Id] = position switch
                    {
                        0 => $"✓ “{title}” now runs on Enter.",
                        1 => $"✓ “{title}” now runs on Ctrl+Enter.",
                        _ => $"✓ “{title}” moved to position {position + 1}.",
                    };
                    break;

                case "reset" when group != null:
                    _settings.Update(_ => group.Reset());
                    _groupNotes[group.Id] = "✓ Restored the default order.";
                    break;

                case "resetAll":
                    _settings.Update(s => s.Groups.Aggregate(false, (changed, g) => g.Reset() | changed));
                    _footerNote = "✓ Every list is back to its default order.";
                    break;
            }

            Refresh();
        }

        return CommandResult.KeepOpen();
    }

    private void ClearTransientState()
    {
        _pickedGroupId = null;
        _pickedKey = null;
        _footerNote = null;
        _groupNotes.Clear();
    }

    private void Refresh()
    {
        TemplateJson = BuildCard().ToJsonString();
    }

    private JsonObject BuildCard()
    {
        var body = Nodes(TextBlock(
            "Click a command to pick it up, then choose where to drop it. The first command runs on Enter, " +
            "the second on Ctrl+Enter, and the rest are listed in the context menu.",
            subtle: true));

        foreach (var group in _settings.Groups)
        {
            body.Add(GroupHeader(group));
            foreach (var element in GroupRows(group))
            {
                body.Add(element);
            }
        }

        body.Add(Footer());

        return new JsonObject
        {
            ["$schema"] = "http://adaptivecards.io/schemas/adaptive-card.json",
            ["type"] = "AdaptiveCard",
            ["version"] = "1.5",
            ["body"] = body,
        };
    }

    private JsonNode GroupHeader(IActionOrderGroup group)
    {
        var isPicking = _pickedGroupId == group.Id && _pickedKey != null;

        JsonNode subtitle;
        if (isPicking)
        {
            subtitle = TextBlock($"Moving “{TitleOf(group, _pickedKey!)}”. Choose a spot, or click it again to cancel.", color: "Accent", spacing: "None");
        }
        else if (_groupNotes.TryGetValue(group.Id, out var note))
        {
            subtitle = TextBlock(note, color: "Good", spacing: "None");
        }
        else
        {
            subtitle = TextBlock(group.Description, subtle: true, spacing: "None");
        }

        var columns = Nodes(Column("stretch", TextBlock(group.Title, size: "Medium", weight: "Bolder"), subtitle));

        if (isPicking)
        {
            columns.Add(Column("auto", ActionSet(SubmitAction("Cancel", new JsonObject { ["op"] = "cancel" }))));
        }
        else if (!group.IsDefault)
        {
            columns.Add(Column("auto", ActionSet(SubmitAction(
                "Reset",
                new JsonObject { ["op"] = "reset", ["group"] = group.Id },
                tooltip: $"Restore the default order for {group.Title}"))));
        }

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "ExtraLarge",
            ["columns"] = columns,
        };
    }

    private IEnumerable<JsonNode> GroupRows(IActionOrderGroup group)
    {
        var keys = group.CurrentKeys;
        var picked = _pickedGroupId == group.Id ? keys.ToList().IndexOf(_pickedKey ?? string.Empty) : -1;

        for (var gap = 0; gap <= keys.Count; gap++)
        {
            // Gaps directly above and below the picked row would leave it where it is.
            if (picked >= 0 && gap != picked && gap != picked + 1)
            {
                yield return DropTarget(group, keys[picked], gap, landsAt: gap > picked ? gap - 1 : gap);
            }

            if (gap < keys.Count)
            {
                yield return Row(group, keys[gap], gap, isPicked: gap == picked);
            }
        }
    }

    private static JsonNode Row(IActionOrderGroup group, string key, int position, bool isPicked)
    {
        var title = TitleOf(group, key);
        var hint = isPicked ? "Click to cancel" : KeyHint(position);

        var columns = Nodes(
            Column("auto", TextBlock(Grip, subtle: !isPicked, color: isPicked ? "Accent" : null, size: "Medium")),
            Column("stretch", TextBlock(title, weight: isPicked ? "Bolder" : null)));

        if (hint != null)
        {
            columns.Add(Column("auto", TextBlock(hint, subtle: true, size: "Small")));
        }

        return new JsonObject
        {
            ["type"] = "Container",
            ["style"] = isPicked ? "accent" : "emphasis",
            ["roundedCorners"] = true,
            ["spacing"] = position == 0 ? "Default" : "Small",
            ["selectAction"] = SubmitAction(
                isPicked ? $"Cancel moving {title}" : $"Move {title}",
                new JsonObject { ["op"] = "pick", ["group"] = group.Id, ["key"] = key }),
            ["items"] = Nodes(new JsonObject
            {
                ["type"] = "ColumnSet",
                ["columns"] = columns,
            }),
        };
    }

    private static JsonNode DropTarget(IActionOrderGroup group, string key, int gap, int landsAt)
    {
        var where = KeyHint(landsAt) is { } shortcut ? $"Move here · {shortcut}" : "Move here";

        return new JsonObject
        {
            ["type"] = "Container",
            ["spacing"] = "Small",
            ["roundedCorners"] = true,
            ["showBorder"] = true,
            ["selectAction"] = SubmitAction(
                $"Move {TitleOf(group, key)} to position {landsAt + 1}",
                new JsonObject { ["op"] = "drop", ["group"] = group.Id, ["key"] = key, ["gap"] = gap }),
            ["items"] = Nodes(TextBlock($"↓  {where}", color: "Accent", size: "Small", weight: "Bolder", horizontalAlignment: "Center")),
        };
    }

    private JsonNode Footer()
    {
        var columns = Nodes(Column("auto", ActionSet(SubmitAction(
            "Reset all to defaults",
            new JsonObject { ["op"] = "resetAll" },
            tooltip: "Restore the default command order for every list"))));

        if (_footerNote != null)
        {
            columns.Add(Column("stretch", TextBlock(_footerNote, color: "Good")));
        }

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "ExtraLarge",
            ["columns"] = columns,
        };
    }

    private static string? KeyHint(int position) => position switch
    {
        0 => "Enter",
        1 => "Ctrl+Enter",
        _ => null,
    };

    private static string TitleOf(IActionOrderGroup group, string key) =>
        group.Choices.FirstOrDefault(c => c.Key == key).Title ?? key;

    // Card elements are built from JsonNode-typed values throughout: JsonArray's generic Add<T>
    // (which collection initializers bind to for derived types) is not trim/AOT safe.
    private static JsonArray Nodes(params JsonNode[] items) => new(items);

    private static JsonNode Column(string width, params JsonNode[] items) => new JsonObject
    {
        ["type"] = "Column",
        ["width"] = width,
        ["verticalContentAlignment"] = "Center",
        ["items"] = Nodes(items),
    };

    private static JsonNode ActionSet(JsonNode action) => new JsonObject
    {
        ["type"] = "ActionSet",
        ["actions"] = Nodes(action),
    };

    private static JsonNode SubmitAction(string title, JsonObject data, string? tooltip = null)
    {
        var action = new JsonObject
        {
            ["type"] = "Action.Submit",
            ["title"] = title,
            ["data"] = data,
        };

        if (tooltip != null)
        {
            action["tooltip"] = tooltip;
        }

        return action;
    }

    private static JsonNode TextBlock(
        string text,
        bool subtle = false,
        string? size = null,
        string? weight = null,
        string? spacing = null,
        string? color = null,
        string? horizontalAlignment = null)
    {
        var block = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = text,
            ["wrap"] = true,
        };

        void Set(string name, string? value)
        {
            if (value != null)
            {
                block[name] = value;
            }
        }

        if (subtle)
        {
            block["isSubtle"] = true;
        }

        Set("size", size);
        Set("weight", weight);
        Set("spacing", spacing);
        Set("color", color);
        Set("horizontalAlignment", horizontalAlignment);
        return block;
    }

    private static JsonObject? ParseObject(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(json) as JsonObject;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string? GetString(JsonObject? obj, string name) =>
        obj?[name] is JsonValue value && value.GetValueKind() == JsonValueKind.String ? value.GetValue<string>() : null;

    private static int? GetInt(JsonObject? obj, string name) =>
        obj?[name] is JsonValue value && value.GetValueKind() == JsonValueKind.Number && value.TryGetValue<int>(out var number) ? number : null;
}
