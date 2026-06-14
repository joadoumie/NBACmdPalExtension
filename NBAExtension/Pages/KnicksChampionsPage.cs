// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Text;
using System.Threading;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension.Pages;

/// <summary>
/// A celebration <see cref="ContentPage"/> for the New York Knicks' 2026 NBA championship.
/// Rendered in place of the schedule while the NBA is in the offseason.
///
/// Design notes — the Command Palette markdown renderer (a WinUI MarkdownTextBlock) does NOT
/// center markdown, honor HTML <c>&lt;img width&gt;</c>, render SVG <c>&lt;text&gt;</c>, or play
/// animated images. So:
///   * The hero is a wide, shapes-only SVG banner written to a temp file and referenced as a
///     <c>file:</c> URI with <c>?--x-cmdpal-fit=fit&amp;--x-cmdpal-upscale=true</c> so it spans
///     the full view width. The design is centered inside the banner, so fitting it to width
///     reads as a centered, designed graphic (data: URIs can't do this — they ignore the size
///     hints, which is why the first attempt rendered small).
///   * All words come from markdown text + the Knicks logo (SVG text won't render).
///   * Motion comes from one observable <see cref="MarkdownContent.Body"/> we mutate on a timer
///     (the host's ContentMarkdownViewModel re-renders when <c>Body</c> raises PropChanged).
/// </summary>
internal sealed partial class KnicksChampionsPage : ContentPage, IDisposable
{
    private const string KnicksLogo = "https://a.espncdn.com/i/teamlogos/nba/500/ny.png";
    private const string SpursLogo = "https://a.espncdn.com/i/teamlogos/nba/500/sa.png";
    private const string BrunsonHeadshot = "https://a.espncdn.com/i/headshots/nba/players/full/3934672.png";
    private const string NbaLogo = "https://a.espncdn.com/combiner/i?img=/i/teamlogos/leagues/500/nba.png&w=64&h=64&transparent=true";
    private const string RecapUrl = "https://www.espn.com/nba/game/_/gameId/401859967/knicks-spurs";

    // The Knicks' ESPN team id (used to reuse the existing roster page).
    private const string KnicksTeamId = "18";

    // Shared render width (px) for every celebration image, so the hero logo, the clincher
    // matchup logos, and the MVP headshot all appear at the same size.
    private const int ImageWidth = 200;

    // Confetti cells for the animated band. Defined as escapes (not raw glyphs) for clarity.
    private static readonly string[] ConfettiPalette =
    {
        "\U0001F389", // 🎉 party popper
        "\U0001F3C0", // 🏀 basketball
        "\U0001F9E1", // 🧡 orange heart
        "\U0001F499", // 💙 blue heart
        "\U0001F38A", // 🎊 confetti ball
        "\U0001F3C6", // 🏆 trophy
        "\U0001F5FD", // 🗽 Statue of Liberty
        "✨",     // ✨ sparkles
    };

    // The single content block we mutate on a timer to create motion.
    private readonly MarkdownContent _confetti;
    private readonly IContent[] _content;

    private Timer? _timer;
    private int _tick;
    private bool _disposed;

    public KnicksChampionsPage()
    {
        Icon = new IconInfo(KnicksLogo);
        Title = "New York Knicks — 2026 NBA Champions";
        Name = "Celebrate";

        Commands =
        [
            new CommandContextItem(new OpenUrlCommand(RecapUrl)
            {
                Name = "View championship recap on ESPN",
                Result = CommandResult.Dismiss(),
            })
            { Icon = new IconInfo("") }, // Globe
            new CommandContextItem(new TeamRosterListPage(KnicksTeamId, "New York Knicks", KnicksLogo)
            {
                Name = "View Knicks Roster",
                Icon = new IconInfo(KnicksLogo),
            }),
            new CommandContextItem(new ViewStandingsDynamicPage { Name = "View NBA Standings" })
            { Icon = new IconInfo(NbaLogo) },
        ];

        _confetti = new MarkdownContent(BuildConfetti(0));
        _content =
        [
            new MarkdownContent(BuildBanner()),
            _confetti,
            new MarkdownContent(BuildHero()),
            new MarkdownContent(BuildStory()),
            new MarkdownContent(BuildClincher()),
            new MarkdownContent(BuildMvp()),
            new MarkdownContent(BuildClosing()),
        ];
    }

    public override IContent[] GetContent()
    {
        // Start the animation lazily the first time the page is actually opened.
        StartConfetti();
        return _content;
    }

    private void StartConfetti()
    {
        if (_timer != null || _disposed)
        {
            return;
        }

        // ~320ms per frame: a lively confetti march that stays cheap. Each tick rewrites the
        // band body, which raises PropChanged on the observable MarkdownContent and re-renders.
        _timer = new Timer(_ => Tick(), null, TimeSpan.FromMilliseconds(320), TimeSpan.FromMilliseconds(320));
    }

    private void Tick()
    {
        if (_disposed)
        {
            return;
        }

        try
        {
            _confetti.Body = BuildConfetti(unchecked(++_tick));
        }
        catch (Exception ex)
        {
            // The host may have torn down the page; stop animating rather than spin on errors.
            System.Diagnostics.Debug.WriteLine($"KnicksChampionsPage confetti stopped: {ex.Message}");
            _timer?.Dispose();
            _timer = null;
        }
    }

    // The animated emoji band that marches left one cell per frame, directly beneath the banner.
    private static string BuildConfetti(int tick)
    {
        const int width = 30;
        var band = new StringBuilder("## ");
        for (var i = 0; i < width; i++)
        {
            band.Append(ConfettiPalette[(i + tick) % ConfettiPalette.Length]);
        }

        return band.ToString();
    }

    // Writes the wide, shapes-only SVG banner to a temp file and returns a markdown image that
    // fits it to the full view width. Returns empty (block is skipped) if the write fails.
    private static string BuildBanner()
    {
        try
        {
            var path = Path.Combine(Path.GetTempPath(), "nba_champions_banner_2026.svg");
            File.WriteAllText(path, BuildBannerSvg());
            var uri = new Uri(path).AbsoluteUri; // file:///C:/.../nba_champions_banner_2026.svg
            return $"![2026 NBA Champions]({uri}?--x-cmdpal-fit=fit&--x-cmdpal-upscale=true)";
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"KnicksChampionsPage banner unavailable: {ex.Message}");
            return string.Empty;
        }
    }

    // A 1200x180 banner: navy gradient field, Knicks orange/blue accent rails, scattered confetti,
    // and a centered gold trophy (all vector shapes — no text, which the renderer can't draw).
    private static string BuildBannerSvg()
    {
        var sb = new StringBuilder();
        sb.Append("<svg xmlns=\"http://www.w3.org/2000/svg\" width=\"1200\" height=\"180\" viewBox=\"0 0 1200 180\">");
        sb.Append("<defs>");
        sb.Append("<linearGradient id=\"bg\" x1=\"0\" y1=\"0\" x2=\"0\" y2=\"1\"><stop offset=\"0\" stop-color=\"#0B2545\"/><stop offset=\"1\" stop-color=\"#06182E\"/></linearGradient>");
        sb.Append("<radialGradient id=\"glow\" cx=\"0.5\" cy=\"0.5\" r=\"0.5\"><stop offset=\"0\" stop-color=\"#1E4E8C\" stop-opacity=\"0.65\"/><stop offset=\"1\" stop-color=\"#0B2545\" stop-opacity=\"0\"/></radialGradient>");
        sb.Append("</defs>");
        sb.Append("<rect width=\"1200\" height=\"180\" fill=\"url(#bg)\"/>");
        sb.Append("<ellipse cx=\"600\" cy=\"95\" rx=\"300\" ry=\"120\" fill=\"url(#glow)\"/>");
        sb.Append("<rect width=\"1200\" height=\"10\" fill=\"#F58426\"/>");
        sb.Append("<rect y=\"170\" width=\"1200\" height=\"10\" fill=\"#006BB6\"/>");

        // Confetti scattered deterministically across the full width, kept clear of the trophy.
        string[] colors = { "#F58426", "#006BB6", "#FFD23F", "#FFFFFF", "#E0A92E" };
        for (var i = 0; i < 54; i++)
        {
            var x = ((i * 137) + 30) % 1170 + 15;
            var y = ((i * 71) + 25) % 150 + 15;
            if (x > 500 && x < 700)
            {
                x = (x + 380) % 1170 + 15; // nudge out of the centered trophy zone
            }

            var c = colors[i % colors.Length];
            if (i % 3 == 0)
            {
                var r = 4 + (i % 3);
                sb.Append($"<circle cx=\"{x}\" cy=\"{y}\" r=\"{r}\" fill=\"{c}\"/>");
            }
            else
            {
                var s = 7 + (i % 4);
                var rot = (i * 37) % 360;
                sb.Append($"<rect x=\"{x}\" y=\"{y}\" width=\"{s}\" height=\"{s}\" rx=\"2\" fill=\"{c}\" transform=\"rotate({rot} {x + (s / 2)} {y + (s / 2)})\"/>");
            }
        }

        // Centered gold trophy (cup + handles + stem + base).
        sb.Append("<g>");
        sb.Append("<rect x=\"556\" y=\"44\" width=\"88\" height=\"11\" rx=\"5\" fill=\"#FFE08A\"/>");
        sb.Append("<path d=\"M 560 55 H 640 L 628 92 Q 600 112 572 92 Z\" fill=\"#FFD23F\"/>");
        sb.Append("<path d=\"M 560 58 q -28 2 -28 24 q 0 18 26 20\" fill=\"none\" stroke=\"#FFD23F\" stroke-width=\"7\"/>");
        sb.Append("<path d=\"M 640 58 q 28 2 28 24 q 0 18 -26 20\" fill=\"none\" stroke=\"#FFD23F\" stroke-width=\"7\"/>");
        sb.Append("<rect x=\"593\" y=\"106\" width=\"14\" height=\"16\" fill=\"#E0A92E\"/>");
        sb.Append("<rect x=\"574\" y=\"122\" width=\"52\" height=\"10\" rx=\"3\" fill=\"#FFD23F\"/>");
        sb.Append("<rect x=\"564\" y=\"132\" width=\"72\" height=\"12\" rx=\"4\" fill=\"#E0A92E\"/>");
        sb.Append("</g>");

        sb.Append("</svg>");
        return sb.ToString();
    }

    private static string BuildHero()
    {
        return $"# \U0001F3C6 New York Knicks 2026 NBA Champions";
    }

    private static string BuildStory()
    {
        return "> **The drought is over.** After 53 years, New York has won another NBA championship.";
    }

    private static string BuildClincher()
    {
        return
            "## \U0001F3C0 The Clincher\n\n" +
            $"![Knicks]({KnicksLogo}?--x-cmdpal-width={ImageWidth}) ![Spurs]({SpursLogo}?--x-cmdpal-width={ImageWidth})\n\n" +
            "**Knicks 94 — Spurs 90** · Game 5 · June 13, 2026 · San Antonio · series won **4–1**.";
    }

    private static string BuildMvp()
    {
        return
            "## \U0001F3C5 Finals MVP\n\n" +
            $"![Jalen Brunson]({BrunsonHeadshot}?--x-cmdpal-width={ImageWidth})\n\n" +
            "**Jalen Brunson** delivers the championship to NYC with a **45-point** masterpiece. Congratulations to the Finals MVP.";
    }

    private static string BuildClosing()
    {
        return
            "---\n\n" +
            "> \U0001F5D3️ The NBA schedule returns when games are back. For now — **celebrate, New York.** \U0001F9E1\U0001F499";
    }

    public void Dispose()
    {
        _disposed = true;
        _timer?.Dispose();
        _timer = null;
    }
}
