using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Text.Json;
using System.Threading.Tasks;

namespace Minesweeper.Network
{
    internal class Connection
    {
        public float Weight;

        internal INeuronInput Input;
        internal INeuronOutput Output;

        internal int? JsonId;

        public Connection()
        {
            Input = null!;
            Output = null!;
        }

        internal Connection(INeuronInput input, INeuronOutput output)
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
            internal int CurrentJsonId = 0;

            public override Connection? Read(
                ref Utf8JsonReader reader,
                Type typeToConvert,
                JsonSerializerOptions options)
                => throw new NotSupportedException("Connection converter only for writing!");

            public override void Write(Utf8JsonWriter writer, Connection value, JsonSerializerOptions options)
            {
                writer.WriteStartObject();

                if (value.JsonId == null)
                {
                    value.JsonId = ++CurrentJsonId;

                    writer.WriteString("$id", value.JsonId.ToString());
                    writer.WriteNumber("Weight", value.Weight);
                }
                else
                {
                    writer.WriteString("$ref", value.JsonId.ToString());

                    value.JsonId = null;
                }

                writer.WriteEndObject();
            }
        }
    }
}