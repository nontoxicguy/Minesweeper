using System.Collections.Generic;

namespace Minesweeper.NeatNetwork
{
    interface IInputNeuron
    {
        public float Value { get; set; }

        public List<Connection> Outs { get; set; }
    }

    interface IOutputNeuron
    {
        public List<Connection> Ins { get; set; }
    }
}