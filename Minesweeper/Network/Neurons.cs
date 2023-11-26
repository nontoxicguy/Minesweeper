using System.Text.Json.Serialization;

namespace Minesweeper.AINetwork;

using Connections = System.Collections.Generic.List<Connection>;

sealed class InputNeuron : IInputNeuron
{
	[JsonIgnore]
	public float Value { get; set; }

	public Connections Outs { get; init; } = [];

#region offsets
	internal readonly sbyte _offsetX, _offsetY;

	static byte s_currentOffset;

	public InputNeuron()
	{
		_offsetX = (sbyte)(s_currentOffset % 9 - 4);
		_offsetY = (sbyte)(s_currentOffset / 9 - 4);
		
		if (++s_currentOffset == 40) ++s_currentOffset;
	}
	
	internal static void ResetOffset() => s_currentOffset = 0;
#endregion
}

sealed class HiddenNeuron(int layer, byte functionIndex) : IInputNeuron, IOutputNeuron
{
	[JsonIgnore]
	public float Value { get; set; }

	public Connections Ins { get; init; } = [];

	public Connections Outs { get; init; } = [];

	[JsonInclude]
	public readonly int Layer = layer;

	[JsonInclude]
	public byte FunctionIndex = functionIndex;
}

sealed class OutputNeuron : IOutputNeuron
{
	public Connections Ins { get; init; } = [];
}