using Minesweeper.NeatNetwork;
using System;
using System.Text.Json;

using static System.IO.File;

using Task = System.Threading.Tasks.Task;

namespace Minesweeper;

sealed class AI
{
    readonly NeuralNetwork[] _ais = new NeuralNetwork[100];

    readonly MinesweeperGame _game;

    readonly Random random = new();

    public ushort WaitTime; // crash without this

    NeuralNetwork best;

    readonly Microsoft.Win32.SaveFileDialog _saveDialog = new()
    {
        InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves",
        DefaultExt = ".json"
    };

    readonly JsonSerializerOptions
        _serializeOptions = new()
        {
            Converters = { new Connection.Converter() }
        },
        _deserializeOptions = new()
        {
            IncludeFields = true,
            ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
        };

    internal static readonly Func<float, float>[] ActivationFunctions = new Func<float, float>[4]
    {
        x => x,
        x => Math.Max(0, x),
        x => (float)(1 / (1 + Math.Exp(-x))),
        x => (float)Math.Tanh(x)
    };

    public AI(MinesweeperGame game)
    {
        _game = game;

        for (byte i = 0; i < _ais.Length; ++i)
        {
            _ais[i] = new();
            _ais[i].Mutate();
        }
    }

    // create new AI with given JSON file path
    internal AI(MinesweeperGame game, string toLoadPath, out bool success)
    {
        _game = game;

        string json = ReadAllText(toLoadPath);

        try
        {
            _ais[0] = Load(json);
        }
        catch (Exception e) when (e is JsonException or NullReferenceException)
        {
            System.Windows.MessageBox.Show("Invalid JSON provided", "Error while loading");
                
            success = false;
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

    NeuralNetwork Load(in string json)
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
            ((Connection.Converter)_serializeOptions.Converters[0]).FinishSerialization();

            WriteAllText(_saveDialog.FileName, json);
        }
    }

    void Mix(NeuralNetwork killed)
    {
        // balance number of hidden neurons
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
            // balance number of connections
            if (killed.Hidden[i].Outs.Count > best.Hidden[i].Outs.Count)
            {
                killed.Hidden[i].Outs[random.Next(killed.Hidden[i].Outs.Count)].Destroy();
            }
            else if (killed.Hidden[i].Outs.Count < best.Hidden[i].Outs.Count)
            {
                killed.AddNeuronOut(random, killed.Hidden[i]);
            }

            // balance weights
            int outsLength = Math.Min(killed.Hidden[i].Outs.Count, best.Hidden[i].Outs.Count);
            for (int j = 0; j < outsLength; ++j)
            {
                killed.Hidden[i].Outs[j].Weight = (killed.Hidden[i].Outs[j].Weight + best.Hidden[i].Outs[j].Weight) / 2;
            }
            
            // balance functions
            if (killed.Hidden[i].FunctionIndex != best.Hidden[i].FunctionIndex && random.Next(2) == 0)
            {
                killed.Hidden[i].FunctionIndex = best.Hidden[i].FunctionIndex;
            }
        }

        // same thing but for inputs (without functions)
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

    internal async Task Train(System.Threading.CancellationToken cancelToken)
    {
        while (true) // task may be cancelled
        {
            byte bestIndex = 0;

            for (byte n = 0; n < 100; ++n)
            {
                _game.NewGame();
                _game.Reveal((byte)random.Next(_game.GameGrid.Columns), (byte)random.Next(_game.GameGrid.Rows));

                _ais[n].Score = 0;

                for (short i = 0; i < 1000; i += 10)
                {
                    bool locked = true;

                    for (byte x = 0; x < _game.GameGrid.Columns; ++x)
                    {
                        for (byte y = 0; y < _game.GameGrid.Rows; ++y)
                        {
                            if (!_game.Tiles[x, y].CanTell) continue; // can't tell so skip

                            // prepare the inputs
                            foreach (InputNeuron input in _ais[n].Inputs)
                            {
                                int
                                    inputX = x + input.OffsetX,
                                    inputY = y + input.OffsetY;

                                if ((inputX != 0 || inputY != 0)
                                    && inputX >= 0
                                    && inputY >= 0
                                    && inputX < _game.GameGrid.Columns
                                    && inputY < _game.GameGrid.Rows)
                                {
                                    input.Value = Images.GridMaker[_game.Tiles[inputX, inputY].Source];
                                }
                            }

                            switch (_ais[n].Process())
                            {
                                // AI chose to reveal
                                case 1:
                                    await Task.Delay(WaitTime, cancelToken);
                                    locked = false;
                                    ++i;

                                    // AI revealed a bomb
                                    if (_game.Tiles[x, y].IsBomb)
                                    {
                                        _ais[n].Score -= 5;
                                        _game.NewGame();
                                        _game.Reveal((byte)random.Next(_game.GameGrid.Columns), (byte)random.Next(_game.GameGrid.Rows));
                                        continue;
                                    }

                                    ++_ais[n].Score;
                                    _game.Reveal(x, y);

                                    // AI won maybe by luck
                                    if (_game.Face == Images.Cool)
                                    {
                                        _game.NewGame();
                                        _game.Reveal((byte)random.Next(_game.GameGrid.Columns), (byte)random.Next(_game.GameGrid.Rows));
                                    }

                                    continue;
                                // AI chose to flag
                                case 2:
                                    await Task.Delay(WaitTime, cancelToken);
                                    locked = false;
                                    ++i;

                                    _game.Tiles[x, y].Source = Images.Flag;
                                    _game.Tiles[x, y].CanTell = false;

                                    continue;
                            }
                        }
                    }

                    // AI did 0 moves looking at every tile one by one we say it is stupid and we stop its turn
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