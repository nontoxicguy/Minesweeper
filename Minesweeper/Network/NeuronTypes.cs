using System.Collections.Generic;

namespace Minesweeper.Network
{
    internal interface INeuronInput
    {
        public float Value { get; set; }

        public List<Connection> Outs { get; set; }
    }

    internal interface INeuronOutput
    {
        public List<Connection> Ins { get; set; }
    }
}