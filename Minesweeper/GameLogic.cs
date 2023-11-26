using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minesweeper;

sealed partial class Game : Window
{
#region fields and properties
	AI _ai;

	readonly Func<int, int> _randomBombLocationIndex = new Random().Next;

	System.Threading.CancellationTokenSource _trainCancel = new();

	internal Tile[,] Tiles { get; private set; } = new Tile[25, 25];

	readonly Microsoft.Win32.OpenFileDialog _aiSelect = new()
	{
		Filter = "JSON (*.json)|*.json",
		InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves"
	};

	readonly System.Timers.Timer _timer = new(1000);

	short _time;

	int _totalMines = 125, _safeSpotsLeft = 500, _minesLeft;
#endregion

#region entry point and game initialization
	[STAThread]
	static void Main() => new Application().Run(new Game());

	Game()
	{
		_timer.Elapsed += (_, _) => Dispatcher.Invoke(() =>
		{
			if (++_time == 999) _timer.Stop();
			_timeText.Text = _time.ToString();
		});

		InitializeComponent();
		_face.Source = Images._happy;
		SetupGrid();
	}

	void SetupGrid()
	{
		for (var y = 0; y < _grid.Rows; ++y)
			for (var x = 0; x < _grid.Columns; ++x)
			{
				int copyX = x, copyY = y;

				Image image = new()
				{
					Source = Images._normal
				};

				image.MouseLeftButtonUp += (_, _) => PlayerReveal(copyX, copyY);
				image.MouseRightButtonDown += (_, _) => Flag(copyX, copyY);

				Tiles[x, y] = new(image);
				_grid.Children.Add(image);
			}
	}

	internal void SetupMines(int x, int y)
	{
		System.Collections.Generic.List<(byte, byte)> freeBombLocations = [];
		for (byte row = 0; row < _grid.Rows; ++row)
			for (byte col = 0; col < _grid.Columns; ++col)
				if (Math.Abs(x - col) > 1 || Math.Abs(y - row) > 1) freeBombLocations.Add((col, row));

		do
		{
			var bombIndex = _randomBombLocationIndex(freeBombLocations.Count);
			Tiles[freeBombLocations[bombIndex].Item1, freeBombLocations[bombIndex].Item2].IsBomb = true;
			freeBombLocations.RemoveAt(bombIndex);
		}
		while (++_minesLeft < _totalMines);
	}

	internal void NewGame(object sender, RoutedEventArgs e)
	{
		_face.Source = Images._happy;

		_timeText.Text = "0";
		_timer.Stop();
		_time = 0;

		_mines.Text = _totalMines.ToString();

		for (var tileX = 0; tileX < _grid.Columns; ++tileX)
			for (var tileY = 0; tileY < _grid.Rows; ++tileY)
			{
				Tiles[tileX, tileY].Source = Images._normal;
				Tiles[tileX, tileY].CanTell = false;
				Tiles[tileX, tileY].IsBomb = false;
			}

		_minesLeft = 0;
		_safeSpotsLeft = Tiles.Length - _totalMines;
	}
#endregion

#region reveal and flag
	void Suspense(object sender, MouseButtonEventArgs e)
	{
		if (_face.Source == Images._happy) _face.Source = Images._suspense;
	}

	void PlayerReveal(int x, int y)
	{
		if (_face.Source != Images._suspense) return;
		_face.Source = Images._happy;

		if (!_timer.Enabled)
		{
			_timer.Start();
			SetupMines(x, y);
		}
		else if (Tiles[x, y].Source != Images._normal) return;
		
		if (Tiles[x, y].IsBomb)
		{
			_face.Source = Images._dead;
			_timer.Stop();

			foreach (var tile in Tiles)
				if (tile.IsBomb)
				{
					if (tile.Source == Images._normal) tile.Source = Images._bomb;
				}
				else if (tile.Source == Images._flag) tile.Source = Images._falseFlag;
		}
		else Reveal(x, y);
	}

	internal void Reveal(int x, int y)
	{
		void Core(int x, int y)
		{
			Tiles[x, y].CanTell = false;

			var neighbours = new (int X, int Y)[8]
			{
				(x - 1, y - 1),
				(x, y - 1),
				(x + 1, y - 1),
				(x - 1, y),
				(x + 1, y),
				(x - 1, y + 1),
				(x, y + 1),
				(x + 1, y + 1)
			}.Where(c => c.X >= 0 && c.Y >= 0 && c.X < _grid.Columns && c.Y < _grid.Rows);

			Tiles[x, y].Source = Images._numbers[neighbours.Count(c => Tiles[c.X, c.Y].IsBomb)];

			Action<int, int> neighbourAction = Tiles[x, y].Source == Images._numbers[0] ? Core : (x, y) => Tiles[x, y].CanTell = true;
			foreach ((int neighbourX, int neighbourY) in neighbours)
				if (Tiles[neighbourX, neighbourY].Source == Images._normal) neighbourAction(neighbourX, neighbourY);

			--_safeSpotsLeft;
		}

		Core(x, y);
		if (_safeSpotsLeft == 0)
		{
			_face.Source = Images._cool;
			_timer.Stop();
		}
	}

	void Flag(int x, int y)
	{
		if (_face.Source != Images._happy) return;

		if (Tiles[x, y].Source == Images._normal)
		{
			Tiles[x, y].Source = Images._flag;
			_mines.Text = (--_minesLeft).ToString();
			Tiles[x, y].CanTell = false;
		}
		else if (Tiles[x, y].Source == Images._flag)
		{
			Tiles[x, y].Source = Images._normal;
			_mines.Text = (++_minesLeft).ToString();
			Tiles[x, y].CanTell = true;
		}
	}
#endregion

#region options menu
	void ShowOptions(object sender, RoutedEventArgs e) => _options.Visibility = _options.Visibility == 0 ? Visibility.Hidden : Visibility.Visible;

	void KeepDigits(object sender, TextCompositionEventArgs e)
	{
		var inputChar = e.Text[^1];
		e.Handled = inputChar < 48 || inputChar > 57;
	}

	void ChangeSize(object sender, KeyEventArgs e)
	{
		var newSizeText = ((TextBox)sender).Text;
		if (e.Key != Key.Enter || newSizeText == string.Empty) return;

		var newSize = Math.Clamp(int.Parse(newSizeText), 5, 50);

		Tiles = new Tile[newSize, newSize];

		_grid.Children.Clear();
		_grid.Rows = _grid.Columns = newSize;

		if (Tiles.Length - 9 < _totalMines) _totalMines = Tiles.Length / 5;

		SetupGrid();
		NewGame(null, null);
	}

	void ChangeMineCount(object sender, KeyEventArgs e)
	{
		var newCountText = ((TextBox)sender).Text;
		if (e.Key != Key.Enter || newCountText == string.Empty) return;

		_totalMines = Math.Clamp(int.Parse(newCountText), 1, Tiles.Length - 9);

		NewGame(null, null);
	}

	void ChangeAISpeed(object sender, SelectionChangedEventArgs e)
	{
		var newSpeed = (string)((ComboBoxItem)((ComboBox)sender).SelectedItem).Content;

		if (newSpeed == "Deactivated")
		{
			_trainCancel.Cancel();
			_trainCancel = new();
			return;
		}

#pragma warning disable 8509 // We handle every speed in the ComboBox
		(_ai ??= new())._waitTime = newSpeed switch
#pragma warning restore 8509
		{
			"Human" => 800,
			"Timelapse" => 75,
			"Computer" => 1
		};

		_timer.Stop();
		_timeText.Text = "0";

		_face.Source = Images._happy;

		_ = _ai.Train(this, _trainCancel.Token);
	}

	void SaveAI(object sender, RoutedEventArgs e) => _ai?.Save();

	void LoadAI(object sender, RoutedEventArgs e)
	{
		if (_aiSelect.ShowDialog() == true)
		{
			AI loaded = new(_aiSelect.FileName, out bool validJson);
			if (validJson) _ai = loaded;
		}
	}
#endregion
}