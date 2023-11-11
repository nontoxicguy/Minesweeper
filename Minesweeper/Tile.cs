using System.Windows.Controls;
using System.Windows.Media;

namespace Minesweeper;

struct Tile
{
    Image _image;
        
    internal readonly ImageSource Source
    {
        get => _image.Source;
        set => _image.Source = value;
    }

    internal bool
        IsBomb,
        CanTell;

    internal void SetupImage(Image image) => _image ??= image;
}