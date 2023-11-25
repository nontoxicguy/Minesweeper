using System.Text.Json;

namespace Minesweeper.AINetwork;

sealed class Connection(float weight)
{
	internal float _weight = weight;

	internal IInputNeuron _input;
	internal IOutputNeuron _output;

#nullable enable
	string? _jsonId;
#nullable disable

	internal Connection(IInputNeuron input, IOutputNeuron output) : this(1)
	{
		(_input = input).Outs.Add(this);
		(_output = output).Ins.Add(this);
	}

	public void Destroy()
	{
		_input.Outs.Remove(this);
		_output.Ins.Remove(this);
	}

	internal sealed class Converter : System.Text.Json.Serialization.JsonConverter<Connection>
	{
		int _currentId;
		
		internal void ResetId() => _currentId = 0;

		/// <summary>
		/// Dummy method
		/// </summary>
		/// <returns>null</returns>
#nullable enable
		public override Connection? Read(ref Utf8JsonReader reader, System.Type typeToConvert, JsonSerializerOptions options) => null;
#nullable disable

		public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
		{
			writer.WriteStartObject();

			if (value._jsonId == null)
			{
				writer.WriteString("$id", value._jsonId = (++_currentId).ToString());
				writer.WriteNumber("Weight", value._weight);
			}
			else
			{
				writer.WriteString("$ref", value._jsonId);
				value._jsonId = null;
			}

			writer.WriteEndObject();
		}
	}
}