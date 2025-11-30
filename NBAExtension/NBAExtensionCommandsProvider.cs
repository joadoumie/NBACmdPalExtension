// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension;

public partial class NBAExtensionCommandsProvider : CommandProvider
{
    private readonly ICommandItem[] _commands;

    public NBAExtensionCommandsProvider()
    {
        DisplayName = "View NBA Games";
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        _commands = [
            new CommandItem(new ViewGamesDynamicPage()) { Title = DisplayName },
            new CommandItem(new SampleGalleryListPage()) { Title = "Sample Gallery List Page" },
        ];
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
