using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace NBAExtension.Data.EspnScheduleResponse;

internal class EspnScheduleResponse
{
    [JsonPropertyName("content")]
    public Content? Content { get; set; }
}

internal class Content
{
    [JsonPropertyName("schedule")]
    public Dictionary<string, ScheduleDate>? Schedule { get; set; }
}

internal class ScheduleDate
{
    [JsonPropertyName("games")]
    public List<Game>? Games { get; set; }
}

internal class Game
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("date")]
    public string? Date { get; set; }

    [JsonPropertyName("competitions")]
    public List<Competition>? Competitions { get; set; }
}

internal class Competition
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("competitors")]
    public List<Competitor>? Competitors { get; set; }

    [JsonPropertyName("status")]
    public GameStatus? Status { get; set; }
}

internal class Competitor
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("homeAway")]
    public string? HomeAway { get; set; }

    [JsonPropertyName("score")]
    public string? Score { get; set; }

    [JsonPropertyName("linescores")]
    public List<LineScore>? LineScores { get; set; }

    [JsonPropertyName("team")]
    public Team? Team { get; set; }

    [JsonPropertyName("records")]
    public List<TeamRecord>? Records { get; set; }

    [JsonPropertyName("leaders")]
    public List<LeaderCategory>? Leaders { get; set; }
}

internal class LineScore
{
    [JsonPropertyName("value")]
    public int Value { get; set; }
}

internal class GameStatus
{
    [JsonPropertyName("clock")]
    public double Clock { get; set; }

    [JsonPropertyName("displayClock")]
    public string? DisplayClock { get; set; }

    [JsonPropertyName("period")]
    public int Period { get; set; }

    [JsonPropertyName("type")]
    public StatusType? Type { get; set; }
}

internal class StatusType
{
    [JsonPropertyName("completed")]
    public bool Completed { get; set; }

    [JsonPropertyName("description")]
    public string? Description { get; set; }

    [JsonPropertyName("detail")]
    public string? Detail { get; set; }

    [JsonPropertyName("shortDetail")]
    public string? ShortDetail { get; set; }
}

internal class Team
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("shortDisplayName")]
    public string? ShortDisplayName { get; set; }

    [JsonPropertyName("logo")]
    public string? Logo { get; set; }

    [JsonPropertyName("logos")]
    public List<Logo>? Logos { get; set; }
}

internal class Logo
{
    [JsonPropertyName("href")]
    public string? Href { get; set; }

    [JsonPropertyName("alt")]
    public string? Alt { get; set; }

    [JsonPropertyName("rel")]
    public List<string>? Rel { get; set; }
}

internal class TeamRecord
{
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    [JsonPropertyName("summary")]
    public string? Summary { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }
}

internal class LeaderCategory
{
    [JsonPropertyName("shortDisplayName")]
    public string? ShortDisplayName { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }

    [JsonPropertyName("leaders")]
    public List<Leader>? Leaders { get; set; }
}

internal class Leader
{
    [JsonPropertyName("displayValue")]
    public string? DisplayValue { get; set; }

    [JsonPropertyName("value")]
    public double Value { get; set; }

    [JsonPropertyName("athlete")]
    public Athlete? Athlete { get; set; }

    [JsonPropertyName("team")]
    public LeaderTeam? Team { get; set; }
}

internal class Athlete
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    [JsonPropertyName("shortName")]
    public string? ShortName { get; set; }

    [JsonPropertyName("fullName")]
    public string? FullName { get; set; }

    [JsonPropertyName("headshot")]
    public string? Headshot { get; set; }

    [JsonPropertyName("jersey")]
    public string? Jersey { get; set; }

    [JsonPropertyName("position")]
    public Position? Position { get; set; }

    [JsonPropertyName("active")]
    public bool Active { get; set; }

    [JsonPropertyName("links")]
    public List<AthleteLink>? Links { get; set; }
}

internal class Position
{
    [JsonPropertyName("abbreviation")]
    public string? Abbreviation { get; set; }
}

internal class AthleteLink
{
    [JsonPropertyName("rel")]
    public List<string>? Rel { get; set; }

    [JsonPropertyName("href")]
    public string? Href { get; set; }
}

internal class LeaderTeam
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }
}
