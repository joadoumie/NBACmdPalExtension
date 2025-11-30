using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace NBAExtension;

internal sealed partial class SampleGalleryListPage : ListPage
{
    public SampleGalleryListPage()
    {
        Icon = new IconInfo("\uE7C5");
        Name = "Sample Gallery List Page";
        GridProperties = new GalleryGridLayout();
    }

    public override IListItem[] GetItems()
    {
        return [
            new ListItem(new NoOpCommand())
            {
                Title = "Sample Title",
                Subtitle = "I don't do anything",
                Icon = new IconInfo("https://a.espncdn.com/i/headshots/nba/players/full/4594268.png"),
            },
           new ListItem(new NoOpCommand())
            {
                Title = "Sample Title",
                Subtitle = "I don't do anything",
                Icon = new IconInfo("https://a.espncdn.com/i/headshots/nba/players/full/4594268.png"),
            }, 
        new ListItem(new NoOpCommand())
            {
                Title = "Sample Title",
                Subtitle = "I don't do anything",
                Icon = new IconInfo("https://a.espncdn.com/i/headshots/nba/players/full/4594268.png"),
        },
        ];
    }
}