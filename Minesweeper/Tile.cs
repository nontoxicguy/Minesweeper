using System.Windows.Controls;
using System.Windows.Media;

namespace Minesweeper;

struct Tile
{
    internal bool
        IsBomb,
        CanTell; // Is the tile empty and next to a number

    Image _image;
    internal readonly ImageSource Source
    {
        get => _image.Source;
        set => _image.Source = value;
    }

    internal void SetupImage(Image image) => _image ??= image;
}