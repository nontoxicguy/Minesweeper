using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Minesweeper.Network
{
    internal partial class NeuralNetwork
    {
        [JsonInclude]
        public InputNeuron[] Inputs = new InputNeuron[80];

        [JsonInclude]
        public List<HiddenNeuron> Hidden = new();

        [JsonInclude]
        public OutputNeuron Output = new();

        internal int Score = 0;

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

        internal byte Process()
        {
            if (Output.Ins.Count == 0)
            {
                return 0;
            }

            foreach (IGrouping<int, HiddenNeuron> layer in Hidden.GroupBy(h => h.Layer))
            {
                foreach (HiddenNeuron neuron in layer)
                {
                    neuron.Value = AI.ActivationFunctions[neuron.FunctionIndex](neuron.Ins.Sum(i => i.Input.Value));
                }
            }

            return (byte)(Output.Ins.Sum(c => c.Input.Value)! % 3);
        }
    }
}