using System.Collections.Generic;

namespace Minesweeper.NeatNetwork;

interface IInputNeuron
{
    public float Value { get; set; }

    public List<Connection> Outs { get; init; }
}

interface IOutputNeuron
{
    public List<Connection> Ins { get; init; }
}