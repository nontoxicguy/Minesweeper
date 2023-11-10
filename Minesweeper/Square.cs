using System.Windows.Controls;

namespace Minesweeper
{
    struct Square
    {
        internal Image GridImage;

        internal bool
            IsBomb,
            CanTell;
    }
}