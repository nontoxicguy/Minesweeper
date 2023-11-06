using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Minesweeper.Network
{
    internal class InputNeuron : IInputNeuron
    {
        [JsonIgnore]
        public float Value { get; set; }

        internal readonly sbyte
            OffsetX = 0,
            OffsetY = 0;

        [JsonInclude]
        public List<Connection> Outs { get; set; } = new();

        public InputNeuron() { }

        internal InputNeuron(sbyte x, sbyte y)
        {
            OffsetX = x;
            OffsetY = y;
        }
    }

    internal class HiddenNeuron : IInputNeuron, IOutputNeuron
    {
        [JsonIgnore]
        public float Value { get; set; }

        [JsonInclude]
        public int Layer;

        [JsonInclude]
        public byte FunctionIndex;

        [JsonInclude]
        public List<Connection> Ins { get; set; } = new();

        [JsonInclude]
        public List<Connection> Outs { get; set; } = new();
    }

    internal class OutputNeuron : IOutputNeuron
    {
        [JsonInclude]
        public List<Connection> Ins { get; set; } = new();
    }
}