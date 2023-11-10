using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minesweeper
{
    sealed partial class MinesweeperGame : Window
    {
        AI? _ai;

        internal Square[,] Grid { get; private set; } = new Square[25, 25];

        CancellationTokenSource
            _timerCancel = new(),
            _trainCancel = new();

        bool _start = true;

        readonly OpenFileDialog _aiSelect = new()
        {
            Filter = "JSON (*.json)|*.json",
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves"
        };

        int
            _totalMines = 125,
            _safeSpotsLeft = 500,
            _minesLeft = 125;

        MinesweeperGame()
        {
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
                    Grid[x, y].GridImage = new()
                    {
                        Source = Images.Normal
                    };

                    byte copyX = x, copyY = y;

                    Grid[x, y].GridImage.MouseRightButtonDown += (_, _) => Flag(copyX, copyY);

                    Grid[x, y].GridImage.MouseLeftButtonUp += (_, _) =>
                    {
                        if (Face.Source != Images.Suspense) return;

                        if (_start)
                        {
                            _ = Timer();
                        }

                        Face.Source = Images.Happy;

                        Reveal(copyX, copyY);
                    };

                    GameGrid.Children.Add(Grid[x, y].GridImage);
                }
            }
        }

        void SetupMines(byte x, byte y)
        {
            _minesLeft = _totalMines;

            Random random = new();

            int remainingMines = _totalMines;

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

            while (remainingMines-- > 0)
            {
                int randomIndex = random.Next(freeBombLocations.Count);
                Grid[freeBombLocations[randomIndex].Item1, freeBombLocations[randomIndex].Item2].IsBomb = true;
                freeBombLocations.RemoveAt(randomIndex);
            }
        }

        void NewGame(object _1, RoutedEventArgs _2)
        {
            Face.Source = Images.Happy;

            if (!_timerCancel.IsCancellationRequested)
            {
                _timerCancel.Cancel();
            }
            _timerCancel = new();
            Time.Text = "0";

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
                    Grid[x, y].GridImage.Source = Images.Normal;
                    Grid[x, y].CanTell = false;
                    Grid[x, y].IsBomb = false;
                }
            }

            _safeSpotsLeft = Grid.Length - _totalMines;
        }

        void SaveAI(object _1, RoutedEventArgs _2) => _ai?.Save();

        void LoadAI(object _1, RoutedEventArgs _2)
        {
            if (_aiSelect.ShowDialog() == true)
            {
                AI loaded = new(this, _aiSelect.FileName, out bool validJson);
                if (validJson)
                {
                    _ai = loaded;
                }
            }
        }

        void WinGame()
        {
            Face.Source = Images.Cool;

            _timerCancel.Cancel();

            for (byte x = 0; x < GameGrid.Columns; ++x)
            {
                for (byte y = 0; y < GameGrid.Rows; ++y)
                {
                    if (Grid[x, y].GridImage.Source == Images.Normal)
                    {
                        Grid[x, y].GridImage.Source = Images.Flag;
                    }
                }
            }

            Mines.Text = "0";
        }

        void LoseGame()
        {
            Face.Source = Images.Dead;

            _timerCancel.Cancel();

            for (byte x = 0; x < GameGrid.Columns; ++x)
            {
                for (byte y = 0; y < GameGrid.Rows; ++y)
                {
                    if (Grid[x, y].IsBomb)
                    {
                        if (Grid[x, y].GridImage.Source == Images.Normal)
                        {
                            Grid[x, y].GridImage.Source = Images.Bomb;
                        }
                    }
                    else if (Grid[x, y].GridImage.Source == Images.Flag)
                    {
                        Grid[x, y].GridImage.Source = Images.FalseFlag;
                    }
                }
            }
        }

        void Options(object _1, RoutedEventArgs _2) => OptionsMenu.Visibility = OptionsMenu.IsVisible ? Visibility.Hidden : Visibility.Visible;

        void Flag(byte x, byte y)
        {
            if (Face.Source != Images.Happy) return;

            if (Grid[x, y].GridImage.Source == Images.Normal)
            {
                Grid[x, y].GridImage.Source = Images.Flag;
                Mines.Text = (--_minesLeft).ToString();
                Grid[x, y].CanTell = false;
            }
            else if (Grid[x, y].GridImage.Source == Images.Flag)
            {
                Grid[x, y].GridImage.Source = Images.Normal;
                Mines.Text = (++_minesLeft).ToString();
                Grid[x, y].CanTell = true;
            }
        }

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
                    _ai.WaitTime = 50;
                    break;
                case "Computer":
                    _ai.WaitTime = 1;
                    break;
            }

            _timerCancel.Cancel();
            _timerCancel = new();
            Time.Text = "0";

            Face.Source = Images.Happy;

            _ = _ai.Train(_trainCancel.Token);
        }

        void ChangeMineCount(object _, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || SizeBox.Text == string.Empty) return;

            _totalMines = Math.Clamp(short.Parse(MinesBox.Text), 1, Grid.Length - 9);

            NewGame(null!, null!);
        }

        void ChangeSize(object _, KeyEventArgs e)
        {
            if (e.Key != Key.Enter || SizeBox.Text == string.Empty) return;

            byte newSize = Math.Clamp(byte.Parse(SizeBox.Text), (byte)5, (byte)50);

            GameGrid.Children.Clear();
            Grid = new Square[newSize, newSize];

            GameGrid.Rows = newSize;
            GameGrid.Columns = newSize;

            if (Grid.Length - 9 <= _totalMines)
            {
                _totalMines = Grid.Length / 5;
            }

            SetupGrid();
            NewGame(null!, null!);
        }

        internal void Reveal(byte x, byte y)
        {
            if (_start)
            {
                _start = false;

                SetupMines(x, y);
            }

            if (Grid[x, y].GridImage.Source == Images.Normal)
            {
                if (Grid[x, y].IsBomb)
                {
                    LoseGame();
                    return;
                }

                SafeReveal(x, y);

                if (_safeSpotsLeft == 0)
                {
                    WinGame();
                }
            }
        }

        void SafeReveal(int x, int y)
        {
            Grid[x, y].CanTell = false;

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

            int minesNear = neighbours.Count(c => Grid[c.Item1, c.Item2].IsBomb);
            Grid[x, y].GridImage.Source = Images.Numbers[minesNear];

            Action<int, int> neighbourAction = minesNear == 0 ? SafeReveal : (x, y) => Grid[x, y].CanTell = true;
            foreach ((int neighbourX, int neighbourY) in neighbours)
            {
                if (Grid[neighbourX, neighbourY].GridImage.Source == Images.Normal)
                {
                   neighbourAction(neighbourX, neighbourY);
                }
            }

            --_safeSpotsLeft;
        }

        async Task Timer()
        {
            int time = 0;

            while (true)
            {
                Time.Text = time.ToString();
                time = Math.Min(time + 1, 999);

                await Task.Delay(1000, _timerCancel.Token);
            }
        }

        void Suspense(object _1, MouseButtonEventArgs _2)
        {
            if (Face.Source == Images.Happy)
            {
                Face.Source = Images.Suspense;
            }
        }
    }
}