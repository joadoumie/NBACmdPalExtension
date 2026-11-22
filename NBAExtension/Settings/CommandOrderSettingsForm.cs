// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension.Settings;

/// <summary>
/// Adaptive Card form that lets users choose the command order for each list page.
/// </summary>
/// <remarks>
/// Each list page gets its own group, laid out like Command Palette's own settings cards:
/// one row per command slot, label on the left and a dropdown on the right. Choosing a
/// command that already sits in another slot swaps the two, so the order is always valid
/// and the user never has to juggle duplicates. Edits apply when the user clicks Save.
/// </remarks>
[SuppressMessage("Performance", "CA1859", Justification = "Card helpers return JsonNode on purpose so JsonArray binds to its AOT-safe Add(JsonNode) overload instead of the generic Add<T>.")]
internal sealed partial class CommandOrderSettingsForm : FormContent
{
    private const string SaveOp = "save";
    private const string ResetOp = "reset";
    private static readonly TimeSpan StatusLifetime = TimeSpan.FromSeconds(10);
    private static readonly string[] OrdinalWords = ["Third", "Fourth", "Fifth", "Sixth", "Seventh", "Eighth", "Ninth", "Tenth"];

    private readonly SettingsManager _settings;
    private string? _status;
    private DateTime _statusAt;

    public CommandOrderSettingsForm(SettingsManager settings)
    {
        _settings = settings;
        Refresh();
    }

    /// <summary>Raised after the user submits the form, once settings are applied.</summary>
    public event EventHandler? Submitted;

    /// <summary>Rebuilds the card from the current settings.</summary>
    public void Refresh()
    {
        TemplateJson = BuildCard().ToJsonString();
    }

    public override ICommandResult SubmitForm(string inputs, string data)
    {
        var inputValues = ParseObject(inputs);
        var dataValues = ParseObject(data);
        var op = GetString(dataValues, "op") ?? SaveOp;
        var resetGroupId = op == ResetOp ? GetString(dataValues, "group") : null;
        var baseline = dataValues?["baseline"] as JsonObject;

        _settings.Update(settings =>
        {
            var changed = false;
            foreach (var group in settings.Groups)
            {
                // Always keep pending edits from every group, including on Reset, so
                // clicking one group's Reset never throws away work in another group.
                var requested = Enumerable.Range(0, group.Choices.Count)
                    .Select(slot => GetString(inputValues, InputId(group, slot)))
                    .ToArray();
                var baselineKeys = (baseline?[group.Id] as JsonArray)?
                    .Select(node => node?.GetValueKind() == JsonValueKind.String ? node.GetValue<string>() : null)
                    .ToArray();

                changed |= group.Apply(baselineKeys, requested);

                if (group.Id == resetGroupId)
                {
                    changed |= group.Reset();
                }
            }

            return changed;
        });

        var resetGroup = _settings.Groups.FirstOrDefault(g => g.Id == resetGroupId);
        _status = resetGroup != null ? $"{resetGroup.Title} restored to defaults" : "Changes saved";
        _statusAt = DateTime.UtcNow;

        Refresh();
        Submitted?.Invoke(this, EventArgs.Empty);
        return CommandResult.KeepOpen();
    }

    private JsonObject BuildCard()
    {
        var body = Nodes(TextBlock(
            "Pick the order of commands for each list. The first command runs on Enter, the second on Ctrl+Enter, " +
            "and the rest appear in the context menu. Choosing a command that's in another slot swaps the two.",
            subtle: true));

        foreach (var group in _settings.Groups)
        {
            body.Add(GroupHeader(group));
            body.Add(GroupRows(group));
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

    private static JsonNode GroupHeader(IActionOrderGroup group)
    {
        var columns = Nodes(Column(
            "stretch",
            TextBlock(group.Title, size: "Medium", weight: "Bolder"),
            TextBlock(group.Description, subtle: true, spacing: "None")));

        if (!group.IsDefault)
        {
            columns.Add(Column("auto", ActionSet(SubmitAction(
                "Reset to default",
                new JsonObject { ["op"] = ResetOp, ["group"] = group.Id },
                tooltip: $"Restore the default command order for {group.Title}"))));
        }

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "ExtraLarge",
            ["columns"] = columns,
        };
    }

    private static JsonNode GroupRows(IActionOrderGroup group)
    {
        var current = group.CurrentKeys;
        var rows = new JsonArray();

        for (var slot = 0; slot < current.Count; slot++)
        {
            var (label, hint) = SlotText(slot);

            var choices = new JsonArray();
            foreach (var choice in group.Choices)
            {
                choices.Add((JsonNode)new JsonObject { ["title"] = choice.Title, ["value"] = choice.Key });
            }

            JsonNode row = new JsonObject
            {
                ["type"] = "ColumnSet",
                ["spacing"] = "Medium",
                ["columns"] = Nodes(
                    Column(
                        "stretch",
                        TextBlock(label, weight: slot == 0 ? "Bolder" : null),
                        TextBlock(hint, subtle: true, spacing: "None", size: "Small")),
                    Column(
                        "260px",
                        new JsonObject
                        {
                            ["type"] = "Input.ChoiceSet",
                            ["id"] = InputId(group, slot),
                            ["style"] = "compact",
                            ["value"] = current[slot],
                            ["choices"] = choices,
                        })),
            };

            if (slot > 0)
            {
                row["separator"] = true;
            }

            rows.Add(row);
        }

        return new JsonObject
        {
            ["type"] = "Container",
            ["style"] = "emphasis",
            ["spacing"] = "Small",
            ["items"] = rows,
        };
    }

    private JsonNode Footer()
    {
        // The order each group was rendered with, so Save can tell which slots the user changed.
        var baseline = new JsonObject();
        foreach (var group in _settings.Groups)
        {
            baseline[group.Id] = Nodes(group.CurrentKeys.Select(k => (JsonNode)JsonValue.Create(k)).ToArray());
        }

        var columns = Nodes(Column("auto", ActionSet(SubmitAction(
            "Save",
            new JsonObject { ["op"] = SaveOp, ["baseline"] = baseline },
            style: "positive"))));

        if (_status != null && DateTime.UtcNow - _statusAt < StatusLifetime)
        {
            columns.Add(Column("stretch", TextBlock($"✓ {_status}", subtle: true)));
        }

        return new JsonObject
        {
            ["type"] = "ColumnSet",
            ["spacing"] = "ExtraLarge",
            ["columns"] = columns,
        };
    }

    private static (string Label, string Hint) SlotText(int slot) => slot switch
    {
        0 => ("Primary command", "Runs on Enter or click"),
        1 => ("Secondary command", "Runs on Ctrl+Enter"),
        _ => ($"{(slot - 2 < OrdinalWords.Length ? OrdinalWords[slot - 2] : $"#{slot + 1}")} command", "Shown in the context menu"),
    };

    private static string InputId(IActionOrderGroup group, int slot) => $"{group.Id}.{slot}";

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

    private static JsonNode SubmitAction(string title, JsonObject data, string? style = null, string? tooltip = null)
    {
        var action = new JsonObject
        {
            ["type"] = "Action.Submit",
            ["title"] = title,
            ["data"] = data,
        };

        if (style != null)
        {
            action["style"] = style;
        }

        if (tooltip != null)
        {
            action["tooltip"] = tooltip;
        }

        return action;
    }

    private static JsonNode TextBlock(string text, bool subtle = false, string? size = null, string? weight = null, string? spacing = null)
    {
        var block = new JsonObject
        {
            ["type"] = "TextBlock",
            ["text"] = text,
            ["wrap"] = true,
        };

        if (subtle)
        {
            block["isSubtle"] = true;
        }

        if (size != null)
        {
            block["size"] = size;
        }

        if (weight != null)
        {
            block["weight"] = weight;
        }

        if (spacing != null)
        {
            block["spacing"] = spacing;
        }

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
}
