// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NBAExtension.Data.EspnStandingsResponse;

internal class StandingsResponse
{
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("children")]
    public List<Conference>? Children { get; set; }
}

internal class Conference
{
    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("isConference")]
    public bool IsConference { get; set; }

    [JsonPropertyName("standings")]
    public ConferenceStandings? Standings { get; set; }
}

internal class ConferenceStandings
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("season")]
    public int Season { get; set; }

    [JsonPropertyName("seasonType")]
    public int SeasonType { get; set; }

    [JsonPropertyName("seasonDisplayName")]
    public string? SeasonDisplayName { get; set; }

    [JsonPropertyName("entries")]
    public List<StandingsEntry>? Entries { get; set; }
}

internal class StandingsEntry
{
    [JsonPropertyName("team")]
    public StandingsTeam? Team { get; set; }

    [JsonPropertyName("stats")]
    public List<StandingStat>? Stats { get; set; }
}

internal class StandingsTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("uid")]
    public string? Uid { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("shortDisplayName")]
    public string? ShortDisplayName { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; }

    [JsonPropertyName("logos")]
    public List<TeamLogo>? Logos { get; set; }

    [JsonPropertyName("links")]
    public List<TeamLink>? Links { get; set; }
}

internal class TeamLogo
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("width")]
    public int Width { get; set; }

    [JsonPropertyName("height")]
    public int Height { get; set; }

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    [JsonPropertyName("rel")]
    public List<string>? Rel { get; set; }
}

internal class TeamLink
{
    [JsonPropertyName("language")]
    public string? Language { get; set; }

    [JsonPropertyName("rel")]
    public List<string>? Rel { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("shortText")]
    public string? ShortText { get; set; }

    [JsonPropertyName("isExternal")]
    public bool IsExternal { get; set; }

    [JsonPropertyName("isPremium")]
    public bool IsPremium { get; set; }
}

internal class StandingStat
{
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("shortDisplayName")]
    public string? ShortDisplayName { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("displayValue")]
    public string? DisplayValue { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
