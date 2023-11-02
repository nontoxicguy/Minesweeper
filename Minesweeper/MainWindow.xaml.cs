using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minesweeper
{
    public partial class MainWindow : Window
    {
        private AI? _ai;

        internal Square[,] Grid = new Square[25, 25];

        private CancellationTokenSource
            _timerCancel = new(),
            _trainCancel = new();

        private bool
            _playing = false,
            _start = true;

        private readonly OpenFileDialog _aiSelect = new()
        {
            Filter = "JSON (*.json)|*.json",
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves"
        };

        private readonly Regex _isNumber = new("[^0-9]+");

        internal int SafeSpotsLeft = 500;

        private int
            _totalMines = 125,
            _minesLeft = 125;

        public MainWindow()
        {
            InitializeComponent();
            SetupGrid();
        }

        private void SetupGrid()
        {
            for (byte y = 0; y < GameGrid.Rows; ++y)
            {
                for (byte x = 0; x < GameGrid.Columns; ++x)
                {
                    Image image = new()
                    {
                        Source = Images.Normal
                    };

                    byte copyX = x, copyY = y;

                    image.MouseRightButtonDown += (s, e) => Flag(copyX, copyY);
                    image.MouseLeftButtonUp += (s, e) => Reveal(copyX, copyY);

                    GameGrid.Children.Add(image);
                    Grid[x, y].GridImage = image;
                }
            }
        }

        private void SetupMines(byte x, byte y)
        {
            _minesLeft = _totalMines;

            Random random = new();

            int remainingMines = _totalMines;

            List<(byte, byte)> potentialBombLocations = new();

            for (byte row = 0; row < GameGrid.Rows; row++)
            {
                for (byte col = 0; col < GameGrid.Columns; col++)
                {
                    Grid[col, row].IsBomb = false;

                    if (Math.Abs(x - col) > 1 || Math.Abs(y - row) > 1)
                    {
                        potentialBombLocations.Add((col, row));
                    }
                }
            }

            while (remainingMines-- > 0)
            {
                int randomIndex = random.Next(potentialBombLocations.Count);
                (byte col, byte row) = potentialBombLocations[randomIndex];

                Grid[col, row].IsBomb = true;
                potentialBombLocations.RemoveAt(randomIndex);
            }
        }

        private void NewGame(object sender, RoutedEventArgs e)
            => NewGame();

        private void SaveAI(object sender, RoutedEventArgs e)
            => _ai?.Save();

        private void LoadAI(object sender, RoutedEventArgs e)
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

        internal void NewGame()
        {
            Face.Source = Images.Happy;

            _start = true;

            _timerCancel.Cancel();
            _timerCancel = new();

            for (byte x = 0; x < GameGrid.Columns; ++x)
            {
                for (byte y = 0; y < GameGrid.Rows; ++y)
                {
                    Grid[x, y].Source = Images.Normal;
                    Grid[x, y].CanTell = false;
                }
            }

            SafeSpotsLeft = Grid.Length - _totalMines;

            Time.Text = "0";
            Mines.Text = _totalMines.ToString();
        }

        private void WinGame()
        {
            Face.Source = Images.Cool;

            _playing = false;
            _timerCancel.Cancel();

            for (byte x = 0; x < GameGrid.Columns; ++x)
            {
                for (byte y = 0; y < GameGrid.Rows; ++y)
                {
                    if (Grid[x, y].Source == Images.Normal)
                    {
                        Grid[x, y].Source = Images.Flag;
                    }

                    Grid[x, y].CanTell = false;
                }
            }

            Mines.Text = "0";
        }

        private void LoseGame()
        {
            Face.Source = Images.Dead;

            _playing = false;
            _timerCancel.Cancel();

            for (byte x = 0; x < GameGrid.Columns; ++x)
            {
                for (byte y = 0; y < GameGrid.Rows; ++y)
                {
                    if (Grid[x, y].IsBomb)
                    {
                        if (Grid[x, y].Source == Images.Normal)
                        {
                            Grid[x, y].Source = Images.Bomb;
                        }
                    }
                    else if (Grid[x, y].Source == Images.Flag)
                    {
                        Grid[x, y].Source = Images.FalseFlag;
                    }

                    Grid[x, y].CanTell = false;
                }
            }
        }

        private void Options(object sender, RoutedEventArgs e) =>
            OptionsMenu.Visibility = OptionsMenu.IsVisible ? Visibility.Hidden : Visibility.Visible;

        internal void Flag(byte x, byte y)
        {
            if (!_playing) return;

            if (Grid[x, y].Source == Images.Normal)
            {
                Grid[x, y].Source = Images.Flag;
                Mines.Text = (--_minesLeft).ToString();
                Grid[x, y].CanTell = false;
            }
            else if (Grid[x, y].Source == Images.Flag)
            {
                Grid[x, y].Source = Images.Normal;
                Mines.Text = (++_minesLeft).ToString();
                Grid[x, y].CanTell = true;
            }
        }

        private void CanEnterText(object sender, TextCompositionEventArgs e)
            => e.Handled = _isNumber.IsMatch(e.Text);

        private void AISpeedChanged(object sender, SelectionChangedEventArgs e)
        {
            _ai ??= new(this);

            ComboBoxItem newSpeed = (ComboBoxItem)AISpeedBox.SelectedItem;
            switch (newSpeed.Content.ToString())
            {
                case "Deactivated":
                    _trainCancel.Cancel();
                    return;
                case "Human":
                    _ai.WaitTime = 1000;
                    break;
                case "Timelapse":
                    _ai.WaitTime = 100;
                    break;
                case "Computer":
                    _ai.WaitTime = 1;
                    break;
            }

            if (_trainCancel.IsCancellationRequested)
            {
                _trainCancel = new();
            }
            else
            {
                _ = _ai.Train(_trainCancel.Token);
            }
        }

        private void MineCountChanged(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            short newCount = short.Parse(MinesBox.Text);

            if (newCount > 0 && newCount <= GameGrid.Rows * GameGrid.Columns - 9)
            {
                _totalMines = newCount;

                _playing = false;

                NewGame();
            }
            else
            {
                MinesBox.Text = _totalMines.ToString();
            }
        }

        private void ChangeSize(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;

            byte newSize = Math.Clamp(byte.Parse(SizeBox.Text), (byte)5, (byte)50);

            GameGrid.Children.Clear();
            Grid = new Square[newSize, newSize];

            GameGrid.Rows = newSize;
            GameGrid.Columns = newSize;

            _playing = false;

            SizeBox.Text = newSize.ToString();

            if (newSize * newSize - 9 <= _totalMines)
            {
                _totalMines = newSize * newSize / 5;
                MinesBox.Text = _totalMines.ToString();
            }

            SetupGrid();
            NewGame();
        }

        internal void Reveal(byte x, byte y)
        {
            if (_start)
            {
                _start = false;
                _playing = true;

                SetupMines(x, y);
                _ = Timer();
            }
            else if (!_playing)
            {
                return;
            }

            Face.Source = Images.Happy;

            if (Grid[x, y].Source == Images.Normal)
            {
                if (Grid[x, y].IsBomb)
                {
                    LoseGame();
                    return;
                }

                SafeReveal(x, y);

                if (SafeSpotsLeft == 0)
                {
                    WinGame();
                }
            }
        }

        private void SafeReveal(int x, int y)
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

            int MinesNear = neighbours.Count(c => Grid[c.Item1, c.Item2].IsBomb);
            Grid[x, y].Source = Images.Numbers[MinesNear];

            neighbours = neighbours.Where(n => Grid[n.Item1, n.Item2].Source == Images.Normal);

            if (MinesNear == 0)
            {
                foreach ((int x2, int y2) in neighbours)
                {
                    if (Grid[x2, y2].Source == Images.Normal)
                    {
                        SafeReveal(x2, y2);
                    }
                }
            }
            else
            {
                foreach ((int x2, int y2) in neighbours)
                {
                    Grid[x2, y2].CanTell = true;
                }
            }

            --SafeSpotsLeft;
        }

        private async Task Timer()
        {
            int time = 0;

            while (true)
            {
                Time.Text = time.ToString();
                time = Math.Min(time + 1, 999);

                await Task.Delay(1000, _timerCancel.Token);
            }
        }

        private void Suspense(object sender, MouseButtonEventArgs e)
        {
            if (_playing)
            {
                Face.Source = Images.Suspense;
            }
        }
    }
}