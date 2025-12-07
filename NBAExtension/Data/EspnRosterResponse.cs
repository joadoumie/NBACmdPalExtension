// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NBAExtension.Data.EspnRosterResponse;

internal class EspnRosterResponse
{
    [JsonPropertyName("timestamp")]
    public string? Timestamp { get; set; }

    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("athletes")]
    public List<RosterAthlete>? Athletes { get; set; }

    [JsonPropertyName("team")]
    public RosterTeam? Team { get; set; }
}

internal class RosterAthlete
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("jersey")]
    public string? Jersey { get; set; }

    [JsonPropertyName("headshot")]
    public Headshot? Headshot { get; set; }

    [JsonPropertyName("position")]
    public Position? Position { get; set; }

    [JsonPropertyName("height")]
    public double Height { get; set; }

    [JsonPropertyName("displayHeight")]
    public string? DisplayHeight { get; set; }

    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    [JsonPropertyName("displayWeight")]
    public string? DisplayWeight { get; set; }

    [JsonPropertyName("age")]
    public int Age { get; set; }

    [JsonPropertyName("links")]
    public List<AthleteLink>? Links { get; set; }

    [JsonPropertyName("injuries")]
    public List<Injury>? Injuries { get; set; }

    [JsonPropertyName("experience")]
    public Experience? Experience { get; set; }
}

internal class Headshot
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }
}

internal class Injury
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }
}

internal class Experience
{
    [JsonPropertyName("years")]
    public int Years { get; set; }
}

internal class RosterTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("location")]
    public string? Location { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("color")]
    public string? Color { get; set; }
}

internal class Position
{
    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }
    [JsonPropertyName("name")]
    public string? Name { get; set; }
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }
}

internal class AthleteLink
{
    [JsonPropertyName("rel")]
    public List<string>? Rel { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

