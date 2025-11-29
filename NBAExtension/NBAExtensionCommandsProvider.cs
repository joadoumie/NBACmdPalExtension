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
            new CommandItem(new NBAExtensionPage()) { Title = DisplayName },
        ];
    }

    private async Task FetchGamesAsync()
    {
        _lastGames.Clear();
        _gameDates.Clear(); // Clear the dates dictionary
        _lastFetch = DateTime.UtcNow;

        try
        {
            var jsonString = await _httpClient.GetStringAsync(EspnApiUrl);

            // Write to temp file for debugging
            var tempFile = Path.Combine(Path.GetTempPath(), "nba_api_response.json");
            await File.WriteAllTextAsync(tempFile, jsonString);
            System.Diagnostics.Debug.WriteLine($"JSON written to: {tempFile}");

            using var doc = JsonDocument.Parse(jsonString);
            var root = doc.RootElement;

            // Try to navigate the JSON structure
            if (!root.TryGetProperty("content", out var content))
            {
                System.Diagnostics.Debug.WriteLine("No 'content' property found in response");
                return;
            }

            if (!content.TryGetProperty("schedule", out var schedule))
            {
                System.Diagnostics.Debug.WriteLine("No 'schedule' property found in content");
                return;
            }

            // Iterate through each date in the schedule
            foreach (var dateProperty in schedule.EnumerateObject())
            {
                var dateKey = dateProperty.Name;
                var dateValue = dateProperty.Value;

                if (!dateValue.TryGetProperty("games", out var games))
                {
                    continue;
                }

                foreach (var gameElement in games.EnumerateArray())
                {
                    try
                    {
                        var game = JsonSerializer.Deserialize<Game>(gameElement.GetRawText(), _jsonOptions);
                        if (game != null)
                        {
                            var listItem = CreateListItemFromGame(game);
                            if (listItem != null)
                            {
                                _lastGames.Add(listItem);

                                // Store the game date for sorting
                                if (DateTime.TryParse(game.Date, out var gameDate))
                                {
                                    _gameDates[listItem] = gameDate;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Error processing game: {ex.Message}");
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Successfully loaded {_lastGames.Count} games");
        }
        catch (JsonException jsonEx)
        {
            System.Diagnostics.Debug.WriteLine($"JSON Parse Error: {jsonEx.Message}");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Error fetching NBA schedule: {ex.Message}");
        }
    }

    public override ICommandItem[] TopLevelCommands()
    {
        return _commands;
    }

}
