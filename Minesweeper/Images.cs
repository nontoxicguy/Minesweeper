using System;
using System.Collections.Generic;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Minesweeper
{
    static internal class Images
    {
        public static readonly ImageSource[] Numbers = new ImageSource[9]
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

        public static readonly ImageSource
            Normal = CreateImage("Normal"),
            Flag = CreateImage("Flag"),
            Bomb = CreateImage("Bomb"),
            FalseFlag = CreateImage("FalseFlag"),

            Happy = CreateImage("HappyFace"),
            Suspense = CreateImage("SuspenseFace"),
            Dead = CreateImage("DeadFace"),
            Cool = CreateImage("CoolFace");

        public static readonly Dictionary<ImageSource, sbyte> GridMaker = new()
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

        private static ImageSource CreateImage(string name) =>
            new BitmapImage(new Uri($"Images/{name}.png", UriKind.Relative));
    }
}