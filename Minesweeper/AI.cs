using System;
using System.Text.Json;
using Task = System.Threading.Tasks.Task;
using static System.IO.File;

namespace Minesweeper;

using AINetwork;

/// <summary>
/// Trainer and serializer of the competing neural networks
/// </summary>
sealed class AI
{
	readonly NeuralNetwork[] _ais = new NeuralNetwork[100];

	readonly Random _random = new();

	/// <remarks>
	/// Game crashes if lower or equal to 0
	/// </remarks>
	internal ushort _waitTime;

	/// <summary>
	/// The neural network with the highest score of the last generation
	/// </summary>
	/// <remarks>
	/// If no generation passed, null
	/// </remarks>
	NeuralNetwork _best;

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

	internal AI()
	{
		for (var i = 0; i < 100; ++i)
		{
			_ais[i] = new();
			
			for (var j = 0; j < 80; ++j)
				_ais[i].Inputs[j] = new();
			InputNeuron.ResetOffset();

			_ais[i].Mutate(_random);
		}
	}

	/// <summary>
	/// Creates a new AI with the provided .json file as the best neural network
	/// </summary>
	/// <param name="jsonPath">The path of the .json file</param>
	/// <param name="success">Wheter the program succeeded at loading</param>
	internal AI(string jsonPath, out bool success)
	{
		var json = ReadAllText(jsonPath);

		try
		{
			(_ais[0] = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!).LoadSetup();
		}
		catch
		{
			System.Windows.MessageBox.Show("Invalid JSON provided", "Error while loading");
			success = false;
			return;
		}
		finally
		{
			InputNeuron.ResetOffset();
		}

		success = true;

		// I chose the easy solution to make a deep copy
		for (var i = 1; i < 100; ++i)
		{
			(_ais[i] = JsonSerializer.Deserialize<NeuralNetwork>(json, _deserializeOptions)!).LoadSetup();
			InputNeuron.ResetOffset();
			_ais[i].Mutate(_random);
		}
	}

	internal void Save()
	{
		if (_saveDialog.ShowDialog() == true) // ShowDialog returns a bool? so we need '== true'
		{
			WriteAllText(_saveDialog.FileName, JsonSerializer.Serialize(_best, _serializeOptions));
			((Connection.Converter)_serializeOptions.Converters[0]).ResetId();
		}
	}

	/// <summary>
	/// Overcomplicated method that does everything to train the AI all in one
	/// </summary>
	/// <param name="game">The calling minesweeper game</param>
	/// <param name="cancelToken">The token to cancel the task</param>
	internal async Task Train(Game game, System.Threading.CancellationToken cancelToken)
	{
		void NewGame()
		{
			game.NewGame(null!, null!);
			int x = _random.Next(game._grid.Columns), y = _random.Next(game._grid.Rows);
			game.SetupMines(x, y);
			game.Reveal(x, y);
		}

		int bestScore = 0;

		do
		{
			foreach (var ai in _ais)
			{
				NewGame();
				int aiScore = 0;

				for (short i = 0; i < 1000; i += 10)
					for (var x = 0; x < game._grid.Columns; ++x)
						for (var y = 0; y < game._grid.Rows; ++y)
						{
							if (!game.Tiles[x, y].CanTell) continue;

							// AI can do something on this square so we setup the inputs in the grid
							foreach (var input in ai.Inputs)
							{
								int inputX = x + input._offsetX, inputY = y + input._offsetY;
								input.Value = inputX >= 0 && inputY >= 0 && inputX < game._grid.Columns && inputY < game._grid.Rows ? Images._imageToInput[game.Tiles[inputX, inputY].Source] : 0;
							}

							switch (ai.Process())
							{
								case 1: // Reveal
									++i;

									if (game.Tiles[x, y].IsBomb) // AI never reveals any bomb because we stop it
									{
										aiScore -= 5;
										NewGame();
										continue;
									}

									++aiScore;
									game.Reveal(x, y);

									if (game._face.Source == Images._cool) NewGame();

									await Task.Delay(_waitTime, cancelToken);
									continue;
								case 2: // Flag
									++i;

									game.Tiles[x, y].Source = Images._flag;
									game.Tiles[x, y].CanTell = false;

									await Task.Delay(_waitTime, cancelToken);
									continue;
							}
						}

				if (aiScore > bestScore)
				{
					bestScore = aiScore;
					_best = ai;
				}
			}

			// we mix _best with every other neural network
			foreach (var ai in _ais)
			{
				if (ai == _best) continue;

				if (ai.Hidden.Count > _best.Hidden.Count)
				{
					var index = _random.Next(ai.Hidden.Count);

					foreach (var input in ai.Hidden[index].Ins)
						input._input.Outs.Remove(input);

					foreach (var output in ai.Hidden[index].Outs)
						output._output.Ins.Remove(output);

					ai.Hidden.RemoveAt(index);
				}
				else if (ai.Hidden.Count < _best.Hidden.Count) ai.AddHidden(_random);

				var hiddenLength = Math.Min(ai.Hidden.Count, _best.Hidden.Count);
				for (var i = 0; i < hiddenLength; ++i)
				{
					if (ai.Hidden[i].Outs.Count > _best.Hidden[i].Outs.Count) ai.Hidden[i].Outs[_random.Next(ai.Hidden[i].Outs.Count)].Destroy();
					else if (ai.Hidden[i].Outs.Count < _best.Hidden[i].Outs.Count) ai.AddNeuronOut(_random, ai.Hidden[i]);

					var outsLength = Math.Min(ai.Hidden[i].Outs.Count, _best.Hidden[i].Outs.Count);
					for (var j = 0; j < outsLength; ++j)
						ai.Hidden[i].Outs[j]._weight = (ai.Hidden[i].Outs[j]._weight + _best.Hidden[i].Outs[j]._weight) / 2;

					if (ai.Hidden[i].FunctionIndex != _best.Hidden[i].FunctionIndex && _random.Next(2) == 0) ai.Hidden[i].FunctionIndex = _best.Hidden[i].FunctionIndex;
				}

				for (var i = 0; i < 80; ++i)
				{
					if (ai.Inputs[i].Outs.Count > _best.Inputs[i].Outs.Count) ai.Inputs[i].Outs[_random.Next(ai.Inputs[i].Outs.Count)].Destroy();
					else if (ai.Inputs[i].Outs.Count < _best.Inputs[i].Outs.Count)
					{
						var outputIndex = _random.Next(ai.Hidden.Count + 1);
						_ = new Connection(ai.Inputs[i], outputIndex == ai.Hidden.Count ? ai.Output : ai.Hidden[outputIndex]);
					}

					var outsLength = Math.Min(ai.Inputs[i].Outs.Count, _best.Inputs[i].Outs.Count);
					for (var j = 0; j < outsLength; ++j)
						ai.Inputs[i].Outs[j]._weight = (ai.Inputs[i].Outs[j]._weight + _best.Inputs[i].Outs[j]._weight) / 2;
				}

				ai.Mutate(_random);
			}
		}
		while (!cancelToken.IsCancellationRequested);
	}
}