using System.Text.Json.Serialization;

namespace Minesweeper.AINetwork;

using Connections = System.Collections.Generic.List<Connection>;

sealed class InputNeuron : IInputNeuron
{
    [JsonIgnore]
    public float Value { get; set; }

    internal sbyte OffsetX { get; private set; }
    internal sbyte OffsetY { get; private set; }

    public Connections Outs { get; init; } = [];

    // Little system that sets offsets automatically
    static byte _currentOffset;

    public InputNeuron()
    {
        OffsetX = (sbyte)(_currentOffset % 9 - 4);
        OffsetY = (sbyte)(_currentOffset / 9 - 4);
        
        if (++_currentOffset == 40) ++_currentOffset;
    }
    
    internal static void ResetOffset() => _currentOffset = 0;
}

sealed class HiddenNeuron(int layer, byte functionIndex) : IInputNeuron, IOutputNeuron
{
    [JsonIgnore]
    public float Value { get; set; }

    [JsonInclude]
    public readonly int Layer = layer;

    [JsonInclude]
    public byte FunctionIndex = functionIndex;

    public Connections Ins { get; init; } = [];

    public Connections Outs { get; init; } = [];
}

sealed class OutputNeuron : IOutputNeuron
{
    public Connections Ins { get; init; } = [];
}
