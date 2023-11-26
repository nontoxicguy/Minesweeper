using System.Linq;
using Random = System.Random;

namespace Minesweeper.AINetwork;

partial class NeuralNetwork
{
	internal void Mutate(Random random)
	{
		switch (random.Next(28))
		{
			case int i when i < 2:
				AddHidden(random);
				return;
			case int i when i < 8:
				AddConnection(random);
				return;
			case int i when i < 20: // ModifyWeight
				var connections = Inputs.Cast<IInputNeuron>().Concat(Hidden).SelectMany(i => i.Outs);

				if (!connections.Any())
				{
					AddConnection(random);
					return;
				}

				connections.ElementAt(random.Next(connections.Count())).Weight += random.NextSingle() * 2 - 1;
				return;
			default: // ModifyActivationFunction
				if (Hidden.Count == 0)
				{
					AddHidden(random);
					return;
				}

				Hidden[random.Next(Hidden.Count)].FunctionIndex = (byte)random.Next(4);
				return;
		}
	}

	internal void AddHidden(Random random)
	{
		var layer = random.Next(_maxLayer + 2);
		if (layer > _maxLayer) _maxLayer = layer;

		Hidden.Add(new(layer, (byte)random.Next(4)));
	}

	void AddConnection(Random random)
	{
		var inputIndex = random.Next(80 + Hidden.Count);

		if (inputIndex < 80)
		{
			var outputIndex = random.Next(Hidden.Count + 1);
			_ = new Connection(Inputs[inputIndex], outputIndex == Hidden.Count ? Output : Hidden[outputIndex]);
		}
		else AddNeuronOut(random, Hidden[inputIndex - 80]);
	}

	internal void AddNeuronOut(Random random, HiddenNeuron neuron)
	{
		var hidden = Hidden.Where(h => h.Layer > neuron.Layer);
		var outputIndex = random.Next(hidden.Count() + 1);
		_ = new Connection(neuron, outputIndex == hidden.Count() ? Output : hidden.ElementAt(outputIndex));
	}
}