using System.Windows.Controls;

namespace Minesweeper
{
    internal struct Square
    {
        internal Image GridImage;

        internal bool
            IsBomb,
            CanTell;
    }
}