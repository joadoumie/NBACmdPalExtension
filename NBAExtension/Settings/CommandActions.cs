// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace NBAExtension.Settings;

/// <summary>
/// Commands available on a game row in View NBA Games.
/// </summary>
internal enum GameCommandAction
{
    ViewOnEspn,
    TeamLeaders,
    HomeTeamRoster,
    AwayTeamRoster,
}

/// <summary>
/// Commands available on a team row in View NBA Standings.
/// </summary>
internal enum StandingsCommandAction
{
    TeamRoster,
    ViewOnEspn,
}

/// <summary>
/// Commands available on a player row in a team roster.
/// </summary>
internal enum RosterCommandAction
{
    PlayerProfile,
    Stats,
    AdvancedStats,
    GameLog,
    News,
    Biography,
    Splits,
}
