using System;
using System.Linq;
using JsonInclude = System.Text.Json.Serialization.JsonIncludeAttribute;

namespace Minesweeper.AINetwork;

internal sealed partial class NeuralNetwork
{
	[JsonInclude]
	public InputNeuron[] Inputs = new InputNeuron[80];

	[JsonInclude]
	public System.Collections.Generic.List<HiddenNeuron> Hidden = [];

	[JsonInclude]
	public OutputNeuron Output = new();

	int _maxLayer;

	static readonly System.Collections.ObjectModel.ReadOnlyCollection<Func<float, float>> s_activationFunctions = new(
	[
		x => x,
		x => Math.Max(0, x),
		x => 1 / (1 + MathF.Exp(-x)),
		MathF.Tanh
	]);

	internal void LoadSetup()
	{
		// We do not store Input and Output fields in JSON so we assign them here
		foreach (var input in Inputs)
			foreach (var connection in input.Outs)
				connection._input = input;

		foreach (var hidden in Hidden)
		{
			foreach (var connection in hidden.Ins)
				connection._output = hidden;

			foreach (var connection in hidden.Outs)
				connection._input = hidden;
		}

		foreach (var connection in Output.Ins)
			connection._output = Output;
	}

	internal byte Process()
	{
		foreach (var layer in Hidden.GroupBy(h => h.Layer))
			foreach (var neuron in layer)
				neuron.Value = s_activationFunctions[neuron.FunctionIndex](neuron.Ins.Sum(i => i._input.Value * i._weight));

		return (byte)(Output.Ins.Sum(c => c._input.Value * c._weight) % 3);
	}
}