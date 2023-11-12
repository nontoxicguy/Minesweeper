namespace Minesweeper.NeatNetwork;

using Connections = System.Collections.Generic.List<Connection>;

interface IInputNeuron
{
    public float Value { get; set; }

    public Connections Outs { get; init; }
}

interface IOutputNeuron
{
    public Connections Ins { get; init; }
}