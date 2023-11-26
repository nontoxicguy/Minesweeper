using Image = System.Windows.Controls.Image;

namespace Minesweeper;

class Tile
{
	// CanTell: Is empty and next to a number
	internal bool IsBomb, CanTell;

	readonly Image _image;
	internal System.Windows.Media.ImageSource Source
	{
		get => _image.Source;
		set => _image.Source = value;
	}

	internal Tile(Image image) => _image = image;
}