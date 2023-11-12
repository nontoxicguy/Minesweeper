// NOTE: if I switch to a C# version >= 12 then use primary constructors for input and hidden neurons

using System.Text.Json.Serialization;

namespace Minesweeper.NeatNetwork;

using Connections = System.Collections.Generic.List<Connection>;

sealed class InputNeuron : IInputNeuron
{
    [JsonIgnore]
    public float Value { get; set; }

    [JsonInclude]
    public readonly sbyte
        OffsetX = 0,
        OffsetY = 0;

    public Connections Outs { get; init; } = new();

    public InputNeuron(sbyte offsetX, sbyte offsetY)
    {
        OffsetX = offsetX;
        OffsetY = offsetY;
    }
}

sealed class HiddenNeuron : IInputNeuron, IOutputNeuron
{
    [JsonIgnore]
    public float Value { get; set; }

    [JsonInclude]
    public readonly int Layer;

    public byte FunctionIndex;

    public Connections Ins { get; init; } = new();

    public Connections Outs { get; init; } = new();
        
    public HiddenNeuron(int layer, byte functionIndex)
    {
        Layer = layer;
        FunctionIndex = functionIndex;
    }
}

sealed class OutputNeuron : IOutputNeuron
{
    public Connections Ins { get; init; } = new();
}