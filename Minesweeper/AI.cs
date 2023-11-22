using System;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;
using static System.IO.File;

namespace Minesweeper;

using AINetwork;

// not a good name? Minesweeper.AI is great
sealed class AI
{
    readonly NeuralNetwork[] _ais = new NeuralNetwork[100];

    readonly Random random = new();

    internal ushort WaitTime;

    NeuralNetwork best;

    readonly Microsoft.Win32.SaveFileDialog _saveDialog = new()
    {
        DefaultExt = ".json",
        InitialDirectory = AppDomain.CurrentDomain.BaseDirectory + "AISaves"
    };

    readonly JsonSerializerOptions
    _serializeOptions = new()
    {
        Converters =
        {
            new Connection.Converter()
        }
    },
    _deserializeOptions = new()
    {
        IncludeFields = true,
        ReferenceHandler = System.Text.Json.Serialization.ReferenceHandler.Preserve
    };

    internal static readonly System.Collections.ObjectModel.ReadOnlyCollection<Func<float, float>> ActivationFunctions = new(
    [
        x => x,
        x => Math.Max(0, x),
        x => 1 / (1 + MathF.Exp(-x)),
        MathF.Tanh
    ]);

#pragma warning disable 8618
    internal AI()
#pragma warning restore 8618
    {
        for (var i = 0; i < _ais.Length; ++i)
        {
            _ais[i] = new();
            
            for (var j = 0; j < 80; ++j)
                _ais[i].Inputs[j] = new();

            _ais[i].Mutate(random);
        }
    }

#pragma warning disable 8618
    internal AI(string toLoadPath, out bool success)
#pragma warning restore 8618
    {
        var json = ReadAllText(toLoadPath);

        try
        {
            (_ais[0] = best = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!).LoadSetup();
        }
        catch
        {
            System.Windows.MessageBox.Show("Invalid JSON provided", "Error while loading");
            success = false;
            return;
        }

        success = true;

        // I chose the easy solution to make a deep copy
        for (var i = 1; i < _ais.Length; ++i)
        {
            (_ais[i] = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!).LoadSetup();
            _ais[i].Mutate(random);
        }
    }

    internal void Save()
    {
        if (_saveDialog.ShowDialog() == true)
        {
            WriteAllText(_saveDialog.FileName, JsonSerializer.Serialize(best, _serializeOptions));
            ((Connection.Converter)_serializeOptions.Converters[0]).ResetId();
        }
    }

    internal async Task Train(Game game, System.Threading.CancellationToken cancelToken)
    {
        do
        {
            var bestIndex = 0;

            for (var n = 0; n < 100; ++n)
            {
                NewGame();
                _ais[n].Score = 0;

                for (short i = 0; i < 1000; i += 10)
                    for (var x = 0; x < game.GameGrid.Columns; ++x)
                        for (var y = 0; y < game.GameGrid.Rows; ++y)
                        {
                            if (!game.Tiles[x, y].CanTell) continue;

                            // AI can do something on this square so we setup the inputs in the grid
                            foreach (var input in _ais[n].Inputs)
                            {
                                int inputX = x + input.OffsetX, inputY = y + input.OffsetY;
                                input.Value = inputX >= 0 && inputY >= 0 && inputX < game.GameGrid.Columns && inputY < game.GameGrid.Rows ? Images.TileToInput[game.Tiles[inputX, inputY].Source] : 0;
                            }

                            switch (_ais[n].Process())
                            {
                                case 1: // Reveal
                                    ++i;

                                    if (game.Tiles[x, y].IsBomb) // AI never reveals any bomb because we stop it
                                    {
                                        _ais[n].Score -= 5;
                                        NewGame();
                                        continue;
                                    }

                                    ++_ais[n].Score;
                                    game.Reveal(x, y);

                                    if (game.Face.Source == Images.Cool) NewGame();

                                    await Task.Delay(WaitTime, cancelToken);
                                    continue;
                                case 2: // Flag
                                    ++i;

                                    game.Tiles[x, y].Source = Images.Flag;
                                    game.Tiles[x, y].CanTell = false;

                                    await Task.Delay(WaitTime, cancelToken);
                                    continue;
                            }
                        }
                
                if (_ais[n].Score > _ais[bestIndex].Score) bestIndex = n;

                void NewGame()
                {
                    game.NewGame();
                    int x = random.Next(game.GameGrid.Columns), y = random.Next(game.GameGrid.Rows);
                    game.SetupMines(x, y);
                    game.Reveal(x, y);
                }
            }
                
            best = _ais[bestIndex];

            for (var i = 0; i < bestIndex; ++i) MakeChild(_ais[i]);
            for (var i = bestIndex + 1; i < 100; ++i) MakeChild(_ais[i]);

            // complicated and I don't know how to explain it
            void MakeChild(NeuralNetwork killed)
            {
                if (killed.Hidden.Count > best.Hidden.Count)
                {
                    var index = random.Next(killed.Hidden.Count);

                    foreach (var input in killed.Hidden[index].Ins)
                        input.Input.Outs.Remove(input);

                    foreach (var output in killed.Hidden[index].Outs)
                        output.Output.Ins.Remove(output);

                    killed.Hidden.RemoveAt(index);
                }
                else if (killed.Hidden.Count < best.Hidden.Count) killed.AddHidden(random);

                var hiddenLength = Math.Min(killed.Hidden.Count, best.Hidden.Count);
                for (var i = 0; i < hiddenLength; ++i)
                {
                    if (killed.Hidden[i].Outs.Count > best.Hidden[i].Outs.Count) killed.Hidden[i].Outs[random.Next(killed.Hidden[i].Outs.Count)].Destroy();
                    else if (killed.Hidden[i].Outs.Count < best.Hidden[i].Outs.Count) killed.AddNeuronOut(random, killed.Hidden[i]);

                    var outsLength = Math.Min(killed.Hidden[i].Outs.Count, best.Hidden[i].Outs.Count);
                    for (var j = 0; j < outsLength; ++j)
                        killed.Hidden[i].Outs[j].Weight = (killed.Hidden[i].Outs[j].Weight + best.Hidden[i].Outs[j].Weight) / 2;

                    if (killed.Hidden[i].FunctionIndex != best.Hidden[i].FunctionIndex && random.Next(2) == 0) killed.Hidden[i].FunctionIndex = best.Hidden[i].FunctionIndex;
                }

                for (var i = 0; i < 80; ++i)
                {
                    if (killed.Inputs[i].Outs.Count > best.Inputs[i].Outs.Count) killed.Inputs[i].Outs[random.Next(killed.Inputs[i].Outs.Count)].Destroy();
                    else if (killed.Inputs[i].Outs.Count < best.Inputs[i].Outs.Count)
                    {
                        var outputIndex = random.Next(killed.Hidden.Count + 1);
                        _ = new Connection(killed.Inputs[i], outputIndex == killed.Hidden.Count ? killed.Output : killed.Hidden[outputIndex]);
                    }

                    var outsLength = Math.Min(killed.Inputs[i].Outs.Count, best.Inputs[i].Outs.Count);
                    for (var j = 0; j < outsLength; ++j)
                        killed.Inputs[i].Outs[j].Weight = (killed.Inputs[i].Outs[j].Weight + best.Inputs[i].Outs[j].Weight) / 2;
                }

                killed.Mutate(random);
            }
        }
        while (!cancelToken.IsCancellationRequested);
    }
}