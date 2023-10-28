using System.Collections.Generic;

namespace Minesweeper.Network
{
    internal interface IInputNeuron
    {
        public float Value { get; set; }

        public List<Connection> Outs { get; set; }
    }

    internal interface IOutputNeuron
    {
        public List<Connection> Ins { get; set; }
    }
}