using System;
using System.Collections.Generic;
using System.Linq;

namespace Minesweeper.Network
{
    internal partial class NeuralNetwork
    {
        internal void Mutate()
        {
            Random random = new();

            switch (random.Next(38))
            {
                case int i when i < 2:
                    AddHidden(random);
                    return;
                case int i when i < 8:
                    AddConnection(random);
                    return;
                case int i when i < 28:
                    IEnumerable<Connection> connections = Inputs
                        .Cast<IInputNeuron>()
                        .Concat(Hidden)
                        .SelectMany(i => i.Outs);

                    if (!connections.Any())
                    {
                        AddConnection(random);
                        return;
                    }

                    connections.ElementAt(random.Next(connections.Count())).Weight += random.NextSingle() * 2 - 1;

                    return;
                default:
                    if (Hidden.Count == 0)
                    {
                        AddHidden(random);
                        return;
                    }

                    byte index = (byte)random.Next(AI.ActivationFunctions.Length);
                    Hidden[random.Next(Hidden.Count)].FunctionIndex = index;

                    return;
            }
        }

        internal void AddHidden(Random random)
        {
            HiddenNeuron added = new()
            {
                Layer = Hidden.Any() ? random.Next(Hidden.Max(h => h.Layer) + 2) : 0,
                FunctionIndex = (byte)random.Next(AI.ActivationFunctions.Length)
            };
            Hidden.Add(added);

            IEnumerable<Connection> connections = Inputs
                .Cast<IInputNeuron>()
                .Concat(Hidden)
                .SelectMany(n => n.Outs);
            if (connections.Any())
            {
                Connection cutted = connections.ElementAt(random.Next(connections.Count()));
                IOutputNeuron output = cutted.Output;

                output.Ins.Remove(cutted);
                cutted.Output = added;
                added.Ins.Add(cutted);

                _ = new Connection(added, output);
            }
        }

        private void AddConnection(Random random)
        {
            IEnumerable<IInputNeuron> inputs = Inputs
                .Cast<IInputNeuron>()
                .Concat(Hidden);

            if (!inputs.Any())
            {
                AddHidden(random);
                return;
            }

            IInputNeuron input = inputs.ElementAt(random.Next(inputs.Count()));

            if (input is HiddenNeuron hidden)
            {
                AddNeuronOut(random, hidden);
                return;
            }
            else
            {
                int index = random.Next(Hidden.Count + 1);
                IOutputNeuron output = index == Hidden.Count ? Output : Hidden[index];
                _ = new Connection(input, output);
            }
        }

        internal void AddNeuronOut(Random random, HiddenNeuron neuron)
        {
            IEnumerable<HiddenNeuron> hidden = Hidden.Where(h => h.Layer > neuron.Layer);
            int outputIndex = random.Next(hidden.Count() + 1);
            IOutputNeuron output = outputIndex == hidden.Count() ?
                Output : hidden.ElementAt(outputIndex);

            _ = new Connection(neuron, output);
        }
    }
}