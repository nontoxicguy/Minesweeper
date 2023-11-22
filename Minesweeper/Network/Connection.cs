using System.Text.Json;

namespace Minesweeper.AINetwork;

#pragma warning disable 8618
sealed class Connection(float weight)
#pragma warning restore 8618
{
    public float Weight = weight;

    internal IInputNeuron Input;
    internal IOutputNeuron Output;

    string? _jsonId;

    internal Connection(IInputNeuron input, IOutputNeuron output) : this(1)
    {
        (Input = input).Outs.Add(this);
        (Output = output).Ins.Add(this);
    }

    public void Destroy()
    {
        Input.Outs.Remove(this);
        Output.Ins.Remove(this);
    }

    internal sealed class Converter : System.Text.Json.Serialization.JsonConverter<Connection>
    {
        int _currentId;
        
        internal void ResetId() => _currentId = 0;

        /// <summary>
        /// Dummy method
        /// </summary>
        /// <returns>null</returns>
        public override Connection? Read(ref Utf8JsonReader _1, System.Type _2, JsonSerializerOptions _3) => null;

        /// <summary>
        /// Serializes value on writer keeping references
        /// </summary>
        /// <param name="writer">The writer used to serialize</param>
        /// <param name="value">The connection written on writer</param>
        public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions _)
        {
            writer.WriteStartObject();

            if (value._jsonId == null)
            {
                writer.WriteString("$id", value._jsonId = (++_currentId).ToString());
                writer.WriteNumber("Weight", value.Weight);
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