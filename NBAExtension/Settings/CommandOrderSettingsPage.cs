// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using NBAExtension.Helpers;

namespace NBAExtension.Settings;

/// <summary>
/// The page Command Palette shows under Settings > Extensions > NBA Command Palette Extension.
/// </summary>
internal sealed partial class CommandOrderSettingsPage : ContentPage, ICommandSettings
{
    private readonly CommandOrderSettingsForm _form;

    public CommandOrderSettingsPage(SettingsManager settings)
    {
        Name = "Settings";
        Title = "NBA Extension Settings";
        Icon = NbaIcons.LeagueLogo;

        // The form re-renders itself in place on every click (via TemplateJson). Don't
        // RaiseItemsChanged here: that makes Command Palette rebuild the whole card.
        _form = new CommandOrderSettingsForm(settings);
    }

    public IContentPage SettingsPage => this;

    public override IContent[] GetContent()
    {
        // Called when the settings page opens: start fresh, without a half-finished move
        // or a confirmation note left over from the last visit.
        _form.ResetTransientState();
        return [_form];
    }
}
