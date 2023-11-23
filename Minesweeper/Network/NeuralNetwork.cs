using System.Linq;
using JsonInclude = System.Text.Json.Serialization.JsonIncludeAttribute;

namespace Minesweeper.AINetwork;

sealed partial class NeuralNetwork
{
    [JsonInclude]
    public InputNeuron[] Inputs = new InputNeuron[80];

    [JsonInclude]
    public System.Collections.Generic.List<HiddenNeuron> Hidden = [];

    [JsonInclude]
    public OutputNeuron Output = new();

    internal int Score;

    int _maxLayer;

    internal void LoadSetup()
    {
        // We do not store Input and Output fields in JSON so we assign them here
        foreach (var input in Inputs)
            foreach (var connection in input.Outs)
                connection.Input = input;

        foreach (var hidden in Hidden)
        {
            foreach (var connection in hidden.Ins)
                connection.Output = hidden;

            foreach (var connection in hidden.Outs)
                connection.Input = hidden;
        }

        foreach (var connection in Output.Ins)
            connection.Output = Output;
    }

    internal byte Process()
    {
        foreach (var layer in Hidden.GroupBy(h => h.Layer))
            foreach (var neuron in layer)
                neuron.Value = AI.ActivationFunctions[neuron.FunctionIndex](neuron.Ins.Sum(i => i.Input.Value * i.Weight));

        return (byte)(Output.Ins.Sum(c => c.Input.Value * c.Weight) % 3);
    }
}