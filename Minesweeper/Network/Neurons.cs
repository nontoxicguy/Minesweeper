using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Minesweeper.NeatNetwork
{
    class InputNeuron : IInputNeuron
    {
        [JsonIgnore]
        public float Value { get; set; }

        [JsonInclude]
        public readonly sbyte
            OffsetX = 0,
            OffsetY = 0;

        public List<Connection> Outs { get; set; } = new();

        public InputNeuron(sbyte offsetX, sbyte offsetY)
        {
            OffsetX = offsetX;
            OffsetY = offsetY;
        }
    }

    class HiddenNeuron : IInputNeuron, IOutputNeuron
    {
        [JsonIgnore]
        public float Value { get; set; }

        [JsonInclude]
        public readonly int Layer;

        public byte FunctionIndex;

        public List<Connection> Ins { get; set; } = new();

        public List<Connection> Outs { get; set; } = new();
        
        public HiddenNeuron(int layer, byte functionIndex)
        {
            Layer = layer;
            FunctionIndex = functionIndex;
        }
    }

    class OutputNeuron : IOutputNeuron
    {
        public List<Connection> Ins { get; set; } = new();
    }
}