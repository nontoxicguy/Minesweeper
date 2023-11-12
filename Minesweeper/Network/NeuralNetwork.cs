using System.Linq;

using JsonInclude = System.Text.Json.Serialization.JsonIncludeAttribute;

namespace Minesweeper.NeatNetwork;

sealed partial class NeuralNetwork
{
    [JsonInclude]
    public InputNeuron[] Inputs = new InputNeuron[80];

    [JsonInclude]
    public System.Collections.Generic.List<HiddenNeuron> Hidden = new();

    [JsonInclude]
    public OutputNeuron Output = new(); // I only needed one output

    internal int Score = 0;

    int _maxLayer = 0;

    // Initializes every input with an offset from -4 to 4
    public NeuralNetwork()
    {
        for (byte i = 0; i < 40; ++i)
        {
            Inputs[i] = new((sbyte)(i % 9 - 4), (sbyte)(i / 9 - 4));
        }

        for (byte i = 41; i < 81; ++i)
        {
            Inputs[i - 1] = new((sbyte)(i % 9 - 4), (sbyte)(i / 9 - 4));
        }
    }

    // Gives the output of the neural network
    internal byte Process()
    {
        foreach (IGrouping<int, HiddenNeuron> layer in Hidden.GroupBy(h => h.Layer))
        {
            foreach (HiddenNeuron neuron in layer)
            {
                neuron.Value = AI.ActivationFunctions[neuron.FunctionIndex](neuron.Ins.Sum(i => i.Input.Value));
            }
        }

        return (byte)(Output.Ins.Sum(c => c.Input.Value) % 3);
    }
}