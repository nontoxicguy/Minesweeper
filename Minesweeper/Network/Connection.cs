using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Minesweeper.NeatNetwork
{
    class Connection
    {
        public float Weight;

        internal IInputNeuron Input;
        internal IOutputNeuron Output;

        int? _jsonId;

        public Connection(float weight) => Weight = weight;

        internal Connection(IInputNeuron input, IOutputNeuron output)
        {
            Input = input;
            input.Outs.Add(this);

            Output = output;
            output.Ins.Add(this);
        }

        public void Destroy()
        {
            Input.Outs.Remove(this);
            Output.Ins.Remove(this);
        }

        internal class Converter : JsonConverter<Connection>
        {
            int CurrentJsonId = 0;
            
            internal void FinishSerialization() => CurrentJsonId = 0;

            public override Connection? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) => throw new NotSupportedException("Connection converter only for writing!");

            public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                if (value._jsonId == null)
                {
                    value._jsonId = ++CurrentJsonId;

                    writer.WriteString("$id", value._jsonId.ToString());
                    writer.WriteNumber("Weight", value.Weight);
                }
                else
                {
                    writer.WriteString("$ref", value._jsonId.ToString());

                    value._jsonId = null;
                }

                writer.WriteEndObject();
            }
        }
    }
}