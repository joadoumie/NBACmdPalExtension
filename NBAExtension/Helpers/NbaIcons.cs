// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension.Helpers;

/// <summary>
/// Icons bundled with the package so the palette's top-level entries and page headers
/// render without network access. Team logos and player headshots still load from ESPN,
/// like the data they accompany.
/// </summary>
internal static class NbaIcons
{
    private const string LeagueLogoPath = "Assets\\NBALogo.png";

    /// <summary>
    /// Gets the NBA league logo from the bundled <c>Assets\NBALogo.png</c>.
    /// </summary>
    public static IconInfo LeagueLogo => IconHelpers.FromRelativePath(LeagueLogoPath);
}
