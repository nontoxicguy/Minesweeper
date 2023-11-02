using System.Windows.Controls;
using System.Windows.Media;

namespace Minesweeper
{
    internal struct Square
    {
        internal Image GridImage;

        internal readonly ImageSource Source
        {
            get => GridImage.Source;
            set => GridImage.Source = value;
        }

        internal bool
            IsBomb,
            CanTell;
    }
}