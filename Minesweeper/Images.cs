using System.Collections.ObjectModel;
using Bitmap = System.Windows.Media.Imaging.BitmapImage;
using ImageSource = System.Windows.Media.ImageSource;

namespace Minesweeper;

/// <summary>
/// Container class for every image in the game
/// </summary>
static class Images
{
	internal static readonly ReadOnlyCollection<Bitmap> _numbers = new(
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
	_normal = CreateImage("Normal"),
	_flag = CreateImage("Flag"),
	_bomb = CreateImage("Bomb"),
	_falseFlag = CreateImage("FalseFlag"),
	_happy = CreateImage("HappyFace"),
	_suspense = CreateImage("SuspenseFace"),
	_dead = CreateImage("DeadFace"),
	_cool = CreateImage("CoolFace");

	internal static readonly ReadOnlyDictionary<ImageSource, sbyte> _imageToInput = new(new System.Collections.Generic.Dictionary<ImageSource, sbyte>
	{
		{ _flag, -1 },
		{ _normal, 0 },
		{ _numbers[0], 0 },
		{ _numbers[1], 1 },
		{ _numbers[2], 2 },
		{ _numbers[3], 3 },
		{ _numbers[4], 4 },
		{ _numbers[5], 5 },
		{ _numbers[6], 6 },
		{ _numbers[7], 7 },
		{ _numbers[8], 8 }
	});

	static Bitmap CreateImage(string name) => new(new($"Images/{name}.png", System.UriKind.Relative));
}