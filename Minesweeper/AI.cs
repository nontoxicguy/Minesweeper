using Microsoft.Win32;
using Minesweeper.Network;
using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;

namespace Minesweeper
{
    internal class AI
    {
        private readonly NeuralNetwork[] _ais = new NeuralNetwork[100];

        private readonly MainWindow _mainWindow;

        private readonly Random random = new();

        public short WaitTime;

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

        public AI(MainWindow window)
        {
            best = null!;
            _mainWindow = window;

            for (byte i = 0; i < _ais.Length; ++i)
            {
                _ais[i] = new();
                _ais[i].Mutate();
            }
        }

        internal AI(MainWindow window, string toLoadPath)
        {
            _mainWindow = window;

            string json = File.ReadAllText(toLoadPath);
            best = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!;

            foreach (InputNeuron input in best.Inputs)
            {
                input.Outs.ForEach(o => o.Input = input);
            }

            foreach (HiddenNeuron hidden in best.Hidden)
            {
                hidden.Ins.ForEach(i => i.Output = hidden);
                hidden.Outs.ForEach(o => o.Input = hidden);
            }

            best.Output.Ins.ForEach(i => i.Output = best.Output);

            _ais[0] = best;

            for (byte i = 1; i < _ais.Length; ++i)
            {
                _ais[i] = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!;

                foreach (InputNeuron input in _ais[i].Inputs)
                {
                    input.Outs.ForEach(o => o.Input = input);
                }

                foreach (HiddenNeuron hidden in _ais[i].Hidden)
                {
                    hidden.Ins.ForEach(i => i.Output = hidden);
                    hidden.Outs.ForEach(o => o.Input = hidden);
                }

                _ais[i].Output.Ins.ForEach(i => i.Output = best.Output);

                _ais[i].Mutate();
            }
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
            int hiddenLength = 0;

            if (killed.Hidden.Count > best.Hidden.Count)
            {
                int index = random.Next(killed.Hidden.Count);
                killed.Hidden[index].Ins.ForEach(i => i.Input.Outs.Remove(i));
                killed.Hidden[index].Outs.ForEach(o => o.Output.Ins.Remove(o));
                killed.Hidden.RemoveAt(index);

                hiddenLength = best.Hidden.Count;
            }
            else if (killed.Hidden.Count < best.Hidden.Count)
            {
                killed.AddHidden(random);
                hiddenLength = killed.Hidden.Count;
            }

            for (int i = 0; i < hiddenLength; ++i)
            {
                int outsLength = 0;

                if (killed.Hidden[i].Outs.Count > best.Hidden[i].Outs.Count)
                {
                    killed.Hidden[i].Outs[random.Next(killed.Hidden[i].Outs.Count)].Destroy();
                    outsLength = best.Hidden[i].Outs.Count;
                }
                else if (killed.Hidden[i].Outs.Count < best.Hidden[i].Outs.Count)
                {
                    killed.AddNeuronOut(random, killed.Hidden[i]);
                    outsLength = killed.Hidden[i].Outs.Count;
                }

                for (int j = 0; j < outsLength; ++j)
                {
                    float weightSum = killed.Hidden[i].Outs[j].Weight + best.Hidden[i].Outs[j].Weight;
                    killed.Hidden[i].Outs[j].Weight = weightSum / 2;
                }

                if (random.Next(2) == 0)
                {
                    killed.Hidden[i].FunctionIndex = best.Hidden[i].FunctionIndex;
                }
            }

            for (byte i = 0; i < 80; ++i)
            {
                int outsLength = 0;

                if (killed.Inputs[i].Outs.Count > best.Inputs[i].Outs.Count)
                {
                    killed.Inputs[i].Outs[random.Next(killed.Inputs[i].Outs.Count)].Destroy();
                    outsLength = best.Inputs[i].Outs.Count;
                }
                else if (killed.Inputs[i].Outs.Count < best.Inputs[i].Outs.Count)
                {
                    int outputIndex = random.Next(killed.Hidden.Count + 1);
                    INeuronOutput output = outputIndex == killed.Hidden.Count ?
                        killed.Output : killed.Hidden[outputIndex];

                    _ = new Connection(killed.Inputs[i], output);

                    outsLength = killed.Inputs[i].Outs.Count;
                }

                for (int j = 0; j < outsLength; ++j)
                {
                    float weightSum = killed.Inputs[i].Outs[j].Weight + best.Inputs[i].Outs[j].Weight;
                    killed.Inputs[i].Outs[j].Weight = weightSum / 2;
                }
            }
        }

        public async Task Train(CancellationToken cancelToken)
        {
            while (!cancelToken.IsCancellationRequested)
            {
                byte bestIndex = 0;

                for (byte n = 0; n < 100; ++n)
                {
                    _mainWindow.NewGame();
                    byte
                        startX = (byte)random.Next(_mainWindow.GameGrid.Columns),
                        startY = (byte)random.Next(_mainWindow.GameGrid.Rows);
                    _mainWindow.Reveal(startX, startY);

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
                                        Square square = _mainWindow.Grid[inputX, inputY];
                                        input.Value = Images.GridMaker[square.Source];
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
                                            startX = (byte)random.Next(_mainWindow.GameGrid.Columns);
                                            startY = (byte)random.Next(_mainWindow.GameGrid.Rows);
                                            _mainWindow.Reveal(startX, startY);
                                            ++i;
                                            continue;
                                        }

                                        ++_ais[n].Score;
                                        _mainWindow.Reveal(x, y);

                                        if (_mainWindow.SafeSpotsLeft == 0)
                                        {
                                            _ais[n].Score += 9;
                                            _mainWindow.NewGame();
                                            startX = (byte)random.Next(_mainWindow.GameGrid.Columns);
                                            startY = (byte)random.Next(_mainWindow.GameGrid.Rows);
                                            _mainWindow.Reveal(startX, startY);
                                            ++i;
                                        }

                                        continue;
                                    case 2:
                                        _mainWindow.Flag(x, y);

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

                for (byte i = 0; i < bestIndex; ++i)
                {
                    Mix(_ais[i]);
                    _ais[i].Mutate();
                }

                for (byte i = (byte)(bestIndex + 1); i < 100; ++i)
                {
                    Mix(_ais[i]);
                    _ais[i].Mutate();
                }
            }
        }
    }
}