using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Minesweeper;

static class Images
{
    internal static readonly ImageSource[] Numbers = new ImageSource[9]
    {
        CreateImage("0"),
        CreateImage("1"),
        CreateImage("2"),
        CreateImage("3"),
        CreateImage("4"),
        CreateImage("5"),
        CreateImage("6"),
        CreateImage("7"),
        CreateImage("8")
    };

    public static readonly ImageSource Happy = CreateImage("HappyFace");
    internal static readonly ImageSource
        Normal = CreateImage("Normal"),
        Flag = CreateImage("Flag"),
        Bomb = CreateImage("Bomb"),
        FalseFlag = CreateImage("FalseFlag"),
        Suspense = CreateImage("SuspenseFace"),
        Dead = CreateImage("DeadFace"),
        Cool = CreateImage("CoolFace");

    internal static readonly Dictionary<ImageSource, sbyte> GridMaker = new()
    {
        { Normal, -2 },
        { Flag, -1 },
        { Numbers[0], 0 },
        { Numbers[1], 1 },
        { Numbers[2], 2 },
        { Numbers[3], 3 },
        { Numbers[4], 4 },
        { Numbers[5], 5 },
        { Numbers[6], 6 },
        { Numbers[7], 7 },
        { Numbers[8], 8 }
    };

    static ImageSource CreateImage(string name) => new BitmapImage(new($"Images/{name}.png", UriKind.Relative));
}