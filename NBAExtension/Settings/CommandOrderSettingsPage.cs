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

        _form = new CommandOrderSettingsForm(settings);
        _form.Submitted += (_, _) => RaiseItemsChanged();
    }

    public IContentPage SettingsPage => this;

    public override IContent[] GetContent()
    {
        // Rebuild so a stale "Changes saved" note doesn't linger when the page is reopened.
        _form.Refresh();
        return [_form];
    }
}
