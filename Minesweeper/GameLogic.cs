using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace Minesweeper;

sealed partial class MinesweeperGame : Window, INotifyPropertyChanged
{
    AI? _ai;

    ImageSource _face = Images.Happy;
    public ImageSource Face
    {
        get => _face;
        set
        {
            _face = value;
            PropertyChanged?.Invoke(this, new(nameof(Face)));
        }
    }

    internal Tile[,] Tiles { get; private set; } = new Tile[25, 25];
        
    readonly System.Timers.Timer _timer = new(1000);

    CancellationTokenSource _trainCancel = new();

    bool _start = true;

    readonly OpenFileDialog _aiSelect = new()
    {
        Filter = "JSON (*.json)|*.json",
        InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves"
    };

    short
        _time = 0,
        _totalMines = 125,
        _safeSpotsLeft = 500,
        _minesLeft = 125;

    public event PropertyChangedEventHandler? PropertyChanged;

    MinesweeperGame()
    {
        DataContext = this; // needed for face binding

        _timer.Elapsed += (_, _) => Dispatcher.Invoke(() =>
        {
            if (++_time == 999)
            {
                _timer.Stop();
            }

            Time.Text = _time.ToString();
        });

        InitializeComponent();
        SetupGrid();
    }
        
    [STAThread]
    static void Main() => new Application().Run(new MinesweeperGame());

    void SetupGrid()
    {
        for (byte y = 0; y < GameGrid.Rows; ++y)
        {
            for (byte x = 0; x < GameGrid.Columns; ++x)
            {
                byte copyX = x, copyY = y;

                Image image = new()
                {
                    Source = Images.Normal
                };

                // human's flag
                image.MouseRightButtonDown += (_, _) => 
                {
                    if (Face != Images.Happy) return;

                    if (Tiles[copyX, copyY].Source == Images.Normal)
                    {
                        Tiles[copyX, copyY].Source = Images.Flag;
                        Mines.Text = (--_minesLeft).ToString();
                        Tiles[copyX, copyY].CanTell = false;
                    }
                    else if (Tiles[copyX, copyY].Source == Images.Flag)
                    {
                        Tiles[copyX, copyY].Source = Images.Normal;
                        Mines.Text = (++_minesLeft).ToString();
                        Tiles[copyX, copyY].CanTell = true;
                    }
                };

                // human's reveal
                image.MouseLeftButtonUp += (_, _) =>
                {
                    if (Face != Images.Suspense) return;
                    Face = Images.Happy;

                    if (_start)
                    {
                        _timer.Start();
                    }

                    Reveal(copyX, copyY);
                };

                Tiles[x, y].SetupImage(image);
                GameGrid.Children.Add(image);
            }
        }
    }

    // human's NewGame
    void NewGame(object _1, RoutedEventArgs _2)
    {
        Face = Images.Happy;

        Time.Text = "0";
        _timer.Stop();
        _time = 0;

        Mines.Text = _totalMines.ToString();

        NewGame();
    }

    internal void NewGame()
    {
        _start = true;

        for (byte x = 0; x < GameGrid.Columns; ++x)
        {
            for (byte y = 0; y < GameGrid.Rows; ++y)
            {
                Tiles[x, y].Source = Images.Normal;
                Tiles[x, y].CanTell = false;
                Tiles[x, y].IsBomb = false;
            }
        }

        _safeSpotsLeft = (short)(Tiles.Length - _totalMines);
    }

    void SaveAI(object _1, RoutedEventArgs _2) => _ai?.Save();

    void LoadAI(object _1, RoutedEventArgs _2)
    {
        if (_aiSelect.ShowDialog() == true) // ShowDialog returns a nullable bool so == true is needed
        {
            AI loaded = new(this, _aiSelect.FileName, out bool validJson);
            if (validJson)
            {
                _ai = loaded;
            }
        }
    }

    void Options(object _1, RoutedEventArgs _2) => OptionsMenu.Visibility = OptionsMenu.IsVisible ? Visibility.Hidden : Visibility.Visible;

    void CanEnterText(object _, TextCompositionEventArgs e) => e.Handled = !char.IsDigit(e.Text, e.Text.Length - 1);

    void AISpeedChanged(object _1, SelectionChangedEventArgs _2)
    {
        _ai ??= new(this);

        switch (((ComboBoxItem)AISpeedBox.SelectedItem).Content)
        {
            case "Deactivated":
                _trainCancel.Cancel();
                _trainCancel = new();
                return;
            case "Human":
                _ai.WaitTime = 1000;
                break;
            case "Timelapse":
                _ai.WaitTime = 75;
                break;
            case "Computer":
                _ai.WaitTime = 1;
                break;
        }

        _timer.Stop();
        Time.Text = "0";

        Face = Images.Happy;

        _ = _ai.Train(_trainCancel.Token);
    }

    void ChangeMineCount(object _, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || MinesBox.Text == string.Empty) return;

        _totalMines = (short)Math.Clamp(short.Parse(MinesBox.Text), 1, Tiles.Length - 9);

        NewGame(null!, null!);
    }

    void ChangeSize(object _, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || SizeBox.Text == string.Empty) return;

        byte newSize = Math.Clamp(byte.Parse(SizeBox.Text), (byte)5, (byte)50);

        Tiles = new Tile[newSize, newSize];

        GameGrid.Children.Clear();
        GameGrid.Rows = newSize;
        GameGrid.Columns = newSize;

        // if the new grid is not big enough to contain all the mines we put a reasonable number of mines
        if (Tiles.Length - 9 < _totalMines)
        {
            _totalMines = (short)(Tiles.Length / 5);
        }

        SetupGrid();
        NewGame(null!, null!);
    }

    internal void Reveal(byte x, byte y)
    {
        // if the game must start next reveal (now) setup the mines
        if (_start)
        {
            _start = false;

            _minesLeft = _totalMines;

            List<(byte, byte)> freeBombLocations = new();
            for (byte row = 0; row < GameGrid.Rows; ++row)
            {
                for (byte col = 0; col < GameGrid.Columns; ++col)
                {
                    if (Math.Abs(x - col) > 1 || Math.Abs(y - row) > 1)
                    {
                        freeBombLocations.Add((col, row));
                    }
                }
            }

            int remainingMines = _totalMines;
            Random random = new();
            while (remainingMines-- > 0)
            {
                int randomIndex = random.Next(freeBombLocations.Count);
                Tiles[freeBombLocations[randomIndex].Item1, freeBombLocations[randomIndex].Item2].IsBomb = true;
                freeBombLocations.RemoveAt(randomIndex);
            }
        }

        if (Tiles[x, y].Source != Images.Normal) return; // already revealed or flagged
        
        // revealed a bomb, now die and show the bombs and false flags
        if (Tiles[x, y].IsBomb)
        {
            Face = Images.Dead;
            _timer.Stop();

            foreach (Tile tile in Tiles)
            {
                if (tile.IsBomb)
                {
                    if (tile.Source == Images.Normal)
                    {
                        tile.Source = Images.Bomb;
                    }
                }
                else if (tile.Source == Images.Flag)
                {
                    tile.Source = Images.FalseFlag;
                }
            }

            return;
        }

        // not a bomb reveal and see if we won
        SafeReveal(x, y);
        if (_safeSpotsLeft == 0)
        {
            Face = Images.Cool;
            _timer.Stop();
        }
    }

    /// <summary>
    /// Reveal but no bomb at given coordinates. no check is done
    /// </summary>
    /// <seealso cref="Reveal"/>
    void SafeReveal(int x, int y)
    {
        Tiles[x, y].CanTell = false;

        IEnumerable<(int, int)> neighbours = new (int, int)[8]
        {
            (x - 1, y - 1),
            (x, y - 1),
            (x + 1, y - 1),
            (x - 1, y),
            (x + 1, y),
            (x - 1, y + 1),
            (x, y + 1),
            (x + 1, y + 1)
        }.Where(c => c.Item1 >= 0 && c.Item2 >= 0 && c.Item1 < GameGrid.Columns && c.Item2 < GameGrid.Rows);

        int minesNear = neighbours.Count(c => Tiles[c.Item1, c.Item2].IsBomb);
        Tiles[x, y].Source = Images.Numbers[minesNear];

        Action<int, int> neighbourAction = minesNear == 0 ? SafeReveal : (x, y) => Tiles[x, y].CanTell = true;
        foreach ((int neighbourX, int neighbourY) in neighbours)
        {
            if (Tiles[neighbourX, neighbourY].Source == Images.Normal)
            {
               neighbourAction(neighbourX, neighbourY);
            }
        }

        --_safeSpotsLeft;
    }

    /// <see cref="Images.Suspense"/>
    void Suspense(object _1, MouseButtonEventArgs _2)
    {
        if (Face == Images.Happy)
        {
            Face = Images.Suspense;
        }
    }
}