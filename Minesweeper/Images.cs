using System.Collections.ObjectModel;
using Bitmap = System.Windows.Media.Imaging.BitmapImage;
using ImageSource = System.Windows.Media.ImageSource;

namespace Minesweeper;

static class Images
{
    internal static readonly ReadOnlyCollection<Bitmap> Numbers = new(
    [
        CreateImage("0"),
        CreateImage("1"),
        CreateImage("2"),
        CreateImage("3"),
        CreateImage("4"),
        CreateImage("5"),
        CreateImage("6"),
        CreateImage("7"),
        CreateImage("8")
    ]);

    internal static readonly Bitmap
    Normal = CreateImage("Normal"),
    Flag = CreateImage("Flag"),
    Bomb = CreateImage("Bomb"),
    FalseFlag = CreateImage("FalseFlag"),
    Happy = CreateImage("HappyFace"),
    Suspense = CreateImage("SuspenseFace"),
    Dead = CreateImage("DeadFace"),
    Cool = CreateImage("CoolFace");

    internal static readonly ReadOnlyDictionary<ImageSource, sbyte> TileToInput = new(new System.Collections.Generic.Dictionary<ImageSource, sbyte>
    {
        { Flag, -1 },
        { Normal, 0 },
        { Numbers[0], 0 },
        { Numbers[1], 1 },
        { Numbers[2], 2 },
        { Numbers[3], 3 },
        { Numbers[4], 4 },
        { Numbers[5], 5 },
        { Numbers[6], 6 },
        { Numbers[7], 7 },
        { Numbers[8], 8 }
    });

    static Bitmap CreateImage(string name) => new(new($"Images/{name}.png", System.UriKind.Relative));
}