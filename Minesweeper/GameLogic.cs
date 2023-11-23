using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minesweeper;

sealed partial class Game : Window
{
    AI? _ai;

    System.Threading.CancellationTokenSource _trainCancel = new();

    internal Tile[,] Tiles { get; private set; } = new Tile[25, 25];

    bool _start = true;

    readonly Microsoft.Win32.OpenFileDialog _aiSelect = new()
    {
        Filter = "JSON (*.json)|*.json",
        InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves"
    };

    readonly System.Timers.Timer _timer = new(1000);

    short _time;

    int _totalMines = 125, _safeSpotsLeft = 500, _minesLeft = 0;

    Game()
    {
        _timer.Elapsed += (_, _) => Dispatcher.Invoke(() =>
        {
            if (++_time == 999) _timer.Stop();
            _timeText.Text = _time.ToString();
        });

        InitializeComponent();
        Face.Source = Images.Happy;
        SetupGrid();
    }
        
    [STAThread]
    static void Main() => new Application().Run(new Game());

    void SetupGrid()
    {
        for (var y = 0; y < GameGrid.Rows; ++y)
            for (var x = 0; x < GameGrid.Columns; ++x)
            {
                int copyX = x, copyY = y;

                Image image = new()
                {
                    Source = Images.Normal
                };

                image.MouseRightButtonDown += (_, _) => // Flag
                {
                    if (Face.Source != Images.Happy) return;

                    if (Tiles[copyX, copyY].Source == Images.Normal)
                    {
                        Tiles[copyX, copyY].Source = Images.Flag;
                        _mines.Text = (--_minesLeft).ToString();
                        Tiles[copyX, copyY].CanTell = false;
                    }
                    else if (Tiles[copyX, copyY].Source == Images.Flag)
                    {
                        Tiles[copyX, copyY].Source = Images.Normal;
                        _mines.Text = (++_minesLeft).ToString();
                        Tiles[copyX, copyY].CanTell = true;
                    }
                };

                image.MouseLeftButtonUp += (_, _) => // Reveal
                {
                    if (Face.Source != Images.Suspense) return;
                    Face.Source = Images.Happy;

                    if (_start)
                    {
                        _timer.Start();
                        SetupMines(copyX, copyY);
                    }
                    else if (Tiles[copyX, copyY].Source != Images.Normal) return;
                    
                    if (Tiles[copyX, copyY].IsBomb)
                    {
                        Face.Source = Images.Dead;
                        _timer.Stop();

                        foreach (var tile in Tiles)
                            if (tile.IsBomb)
                            {
                                if (tile.Source == Images.Normal) tile.Source = Images.Bomb;
                            }
                            else if (tile.Source == Images.Flag) tile.Source = Images.FalseFlag;
                    }
                    else Reveal(copyX, copyY);
                };

                Tiles[x, y].SetupImage(image);
                GameGrid.Children.Add(image);
            }
    }

    void NewGame(object _1, RoutedEventArgs _2)
    {
        Face.Source = Images.Happy;

        _timeText.Text = "0";
        _timer.Stop();
        _time = 0;

        _mines.Text = _totalMines.ToString();

        NewGame();
    }

    internal void NewGame()
    {
        _start = true;

        for (byte x = 0; x < GameGrid.Columns; ++x)
            for (byte y = 0; y < GameGrid.Rows; ++y)
            {
                Tiles[x, y].Source = Images.Normal;
                Tiles[x, y].CanTell = false;
                Tiles[x, y].IsBomb = false;
            }

        _minesLeft = 0;
        _safeSpotsLeft = Tiles.Length - _totalMines;
    }

    void SaveAI(object _1, RoutedEventArgs _2) => _ai?.Save();

    void LoadAI(object _1, RoutedEventArgs _2)
    {
        if (_aiSelect.ShowDialog() == true)
        {
            AI loaded = new(_aiSelect.FileName, out bool validJson);
            if (validJson) _ai = loaded;
        }
    }

    void Options(object _1, RoutedEventArgs _2) => _options.Visibility = _options.Visibility == Visibility.Visible ? Visibility.Hidden : Visibility.Visible;

    void CanEnterText(object _, TextCompositionEventArgs e) => e.Handled = !char.IsDigit(e.Text, e.Text.Length - 1);

    void AISpeedChanged(object speedBox, SelectionChangedEventArgs __)
    {
        var newSpeed = (string)((ComboBoxItem)((ComboBox)speedBox).SelectedItem).Content;

        if (newSpeed == "Deactivated")
        {
            _trainCancel.Cancel();
            _trainCancel = new();
            return;
        }

#pragma warning disable 8509
        (_ai ??= new()).WaitTime = newSpeed switch
#pragma warning restore 8509
        {
            "Human" => 800,
            "Timelapse" => 75,
            "Computer" => 1
        };

        _timer.Stop();
        _timeText.Text = "0";

        Face.Source = Images.Happy;

        _ = _ai.Train(this, _trainCancel.Token);
    }

    void ChangeMineCount(object mineBox, KeyEventArgs e)
    {
        var newCountText = ((TextBox)mineBox).Text;
        if (e.Key != Key.Enter || newCountText == string.Empty) return;

        _totalMines = Math.Clamp(short.Parse(newCountText), 1, Tiles.Length - 9);

        NewGame(null!, null!);
    }

    void ChangeSize(object sizeBox, KeyEventArgs e)
    {
        var newSizeText = ((TextBox)sizeBox).Text;
        if (e.Key != Key.Enter || newSizeText == string.Empty) return;

        var newSize = Math.Clamp(int.Parse(newSizeText), 5, 50);

        Tiles = new Tile[newSize, newSize];

        GameGrid.Children.Clear();
        GameGrid.Rows = GameGrid.Columns = newSize;

        if (Tiles.Length - 9 < _totalMines) _totalMines = Tiles.Length / 5;

        SetupGrid();
        NewGame(null!, null!);
    }

    internal void Reveal(int x, int y)
    {
        Core(x, y);
        if (_safeSpotsLeft == 0)
        {
            Face.Source = Images.Cool;
            _timer.Stop();
        }

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
            }.Where(c => c.X >= 0 && c.Y >= 0 && c.X < GameGrid.Columns && c.Y < GameGrid.Rows);

            Tiles[x, y].Source = Images.Numbers[neighbours.Count(c => Tiles[c.X, c.Y].IsBomb)];

            Action<int, int> neighbourAction = Tiles[x, y].Source == Images.Numbers[0] ? Core : (x, y) => Tiles[x, y].CanTell = true;
            foreach ((int neighbourX, int neighbourY) in neighbours)
                if (Tiles[neighbourX, neighbourY].Source == Images.Normal) neighbourAction(neighbourX, neighbourY);

            --_safeSpotsLeft;
        }
    }

    internal void SetupMines(int x, int y)
    {
        _start = false;

        System.Collections.Generic.List<(byte, byte)> freeBombLocations = [];
        for (byte row = 0; row < GameGrid.Rows; ++row)
            for (byte col = 0; col < GameGrid.Columns; ++col)
                if (Math.Abs(x - col) > 1 || Math.Abs(y - row) > 1) freeBombLocations.Add((col, row));

        Random random = new();
        do
        {
            var bombIndex = random.Next(freeBombLocations.Count);
            Tiles[freeBombLocations[bombIndex].Item1, freeBombLocations[bombIndex].Item2].IsBomb = true;
            freeBombLocations.RemoveAt(bombIndex);
        }
        while (++_minesLeft < _totalMines);
    }

    void Suspense(object _1, MouseButtonEventArgs _2)
    {
        if (Face.Source == Images.Happy) Face.Source = Images.Suspense;
    }
}