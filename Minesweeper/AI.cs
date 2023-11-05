using Microsoft.Win32;
using Minesweeper.Network;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace Minesweeper
{
    internal class AI
    {
        private readonly NeuralNetwork[] _ais = new NeuralNetwork[100];

        private readonly MinesweeperGame _mainWindow;

        private readonly Random random = new();

        public ushort WaitTime;

        private NeuralNetwork best;

        private readonly SaveFileDialog _saveDialog = new()
        {
            InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves",
            DefaultExt = ".json"
        };

        private readonly JsonSerializerOptions
            _serializeOptions = new()
            {
                Converters = { new Connection.Converter() }
            },
            _deserializeOptions = new()
            {
                ReferenceHandler = ReferenceHandler.Preserve
            };

        internal static readonly Func<float, float>[] ActivationFunctions = new Func<float, float>[4]
        {
            x => x,
            x => Math.Max(0, x),
            x => (float)(1 / (1 + Math.Exp(-x))),
            x => (float)Math.Tanh(x)
        };

        public AI(MinesweeperGame window)
        {
            best = null!;
            _mainWindow = window;

            for (byte i = 0; i < _ais.Length; ++i)
            {
                _ais[i] = new();
                _ais[i].Mutate();
            }
        }

        internal AI(MinesweeperGame window, string toLoadPath, out bool success)
        {
            _mainWindow = window;

            string json = File.ReadAllText(toLoadPath);

            try
            {
                _ais[0] = Load(json);
            }
            catch (JsonException)
            {
                MessageBox.Show("Invalid JSON provided", "Error while loading");
                success = false;
                best = null!;
                return;
            }

            success = true;

            best = _ais[0];

            for (byte i = 1; i < _ais.Length; ++i)
            {
                _ais[i] = Load(json);
                _ais[i].Mutate();
            }
        }

        private NeuralNetwork Load(string json)
        {
            NeuralNetwork loaded = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!;

            foreach (InputNeuron input in loaded.Inputs)
            {
                input.Outs.ForEach(o => o.Input = input);
            }

            foreach (HiddenNeuron hidden in loaded.Hidden)
            {
                hidden.Ins.ForEach(i => i.Output = hidden);
                hidden.Outs.ForEach(o => o.Input = hidden);
            }

            loaded.Output.Ins.ForEach(i => i.Output = loaded.Output);

            return loaded;
        }

        internal void Save()
        {
            if (_saveDialog.ShowDialog() == true)
            {
                string json = JsonSerializer.Serialize(best, _serializeOptions);
                ((Connection.Converter)_serializeOptions.Converters[0]).CurrentJsonId = 0;

                File.WriteAllText(_saveDialog.FileName, json);
            }
        }

        private void Mix(NeuralNetwork killed)
        {
            if (killed.Hidden.Count > best.Hidden.Count)
            {
                int index = random.Next(killed.Hidden.Count);
                killed.Hidden[index].Ins.ForEach(i => i.Input.Outs.Remove(i));
                killed.Hidden[index].Outs.ForEach(o => o.Output.Ins.Remove(o));
                killed.Hidden.RemoveAt(index);
            }
            else if (killed.Hidden.Count < best.Hidden.Count)
            {
                killed.AddHidden(random);
            }

            int hiddenLength = Math.Min(killed.Hidden.Count, best.Hidden.Count);
            for (int i = 0; i < hiddenLength; ++i)
            {
                if (killed.Hidden[i].Outs.Count > best.Hidden[i].Outs.Count)
                {
                    killed.Hidden[i].Outs[random.Next(killed.Hidden[i].Outs.Count)].Destroy();
                }
                else if (killed.Hidden[i].Outs.Count < best.Hidden[i].Outs.Count)
                {
                    killed.AddNeuronOut(random, killed.Hidden[i]);
                }

                int outsLength = Math.Min(killed.Hidden[i].Outs.Count, best.Hidden[i].Outs.Count);
                for (int j = 0; j < outsLength; ++j)
                {
                    killed.Hidden[i].Outs[j].Weight = (killed.Hidden[i].Outs[j].Weight + best.Hidden[i].Outs[j].Weight) / 2;
                }

                if (random.Next(2) == 0)
                {
                    killed.Hidden[i].FunctionIndex = best.Hidden[i].FunctionIndex;
                }
            }

            for (byte i = 0; i < 80; ++i)
            {
                if (killed.Inputs[i].Outs.Count > best.Inputs[i].Outs.Count)
                {
                    killed.Inputs[i].Outs[random.Next(killed.Inputs[i].Outs.Count)].Destroy();
                }
                else if (killed.Inputs[i].Outs.Count < best.Inputs[i].Outs.Count)
                {
                    int outputIndex = random.Next(killed.Hidden.Count + 1);

                    _ = new Connection(killed.Inputs[i], outputIndex == killed.Hidden.Count ? killed.Output : killed.Hidden[outputIndex]);
                }

                int outsLength = Math.Min(killed.Inputs[i].Outs.Count, best.Inputs[i].Outs.Count);
                for (int j = 0; j < outsLength; ++j)
                {
                    killed.Inputs[i].Outs[j].Weight = (killed.Inputs[i].Outs[j].Weight + best.Inputs[i].Outs[j].Weight) / 2;
                }
            }
        }

        public async Task Train(CancellationToken cancelToken)
        {
            while (true)
            {
                byte bestIndex = 0;

                for (byte n = 0; n < 100; ++n)
                {
                    _mainWindow.NewGame();
                    _mainWindow.Reveal((byte)random.Next(_mainWindow.GameGrid.Columns), (byte)random.Next(_mainWindow.GameGrid.Rows));

                    _ais[n].Score = 0;

                    for (short i = 0; i < 1000; i += 10)
                    {
                        bool locked = true;

                        for (byte x = 0; x < _mainWindow.GameGrid.Columns; ++x)
                        {
                            for (byte y = 0; y < _mainWindow.GameGrid.Rows; ++y)
                            {
                                if (!_mainWindow.Grid[x, y].CanTell) continue;

                                foreach (InputNeuron input in _ais[n].Inputs)
                                {
                                    int
                                        inputX = x + input.OffsetX,
                                        inputY = y + input.OffsetY;

                                    if ((inputX != 0 || inputY != 0)
                                        && inputX >= 0
                                        && inputY >= 0
                                        && inputX < _mainWindow.GameGrid.Columns
                                        && inputY < _mainWindow.GameGrid.Rows)
                                    {
                                        input.Value = Images.GridMaker[_mainWindow.Grid[inputX, inputY].Source];
                                    }
                                }

                                switch (_ais[n].Process())
                                {
                                    case 1:
                                        await Task.Delay(WaitTime, cancelToken);
                                        locked = false;
                                        ++i;

                                        if (_mainWindow.Grid[x, y].IsBomb)
                                        {
                                            _ais[n].Score -= 5;
                                            _mainWindow.NewGame();
                                            _mainWindow.Reveal((byte)random.Next(_mainWindow.GameGrid.Columns), (byte)random.Next(_mainWindow.GameGrid.Rows));
                                            continue;
                                        }

                                        ++_ais[n].Score;
                                        _mainWindow.Reveal(x, y);

                                        if (_mainWindow.Face.Source == Images.Cool)
                                        {
                                            _mainWindow.NewGame();
                                            _mainWindow.Reveal((byte)random.Next(_mainWindow.GameGrid.Columns), (byte)random.Next(_mainWindow.GameGrid.Rows));
                                        }

                                        continue;
                                    case 2:
                                        _mainWindow.Grid[x, y].Source = Images.Flag;
                                        _mainWindow.Grid[x, y].CanTell = false;

                                        await Task.Delay(WaitTime, cancelToken);
                                        locked = false;
                                        ++i;

                                        continue;
                                }
                            }
                        }

                        if (locked)
                        {
                            await Task.Delay(WaitTime, cancelToken);
                            break;
                        }
                    }

                    if (_ais[n].Score > _ais[bestIndex].Score)
                    {
                        bestIndex = n;
                    }
                }
                
                best = _ais[bestIndex];

                for (byte i = 0; i < 100; ++i)
                {
                    if (_ais[i] != best)
                    {
                        Mix(_ais[i]);
                        _ais[i].Mutate();
                    }
                }
            }
        }
    }
}