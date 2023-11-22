namespace Minesweeper.AINetwork;

using Connections = System.Collections.Generic.List<Connection>;

// In the case I need it: I have a commented INeuron interface
// If I need it but I still need only one output, make it an attribute

// interface INeuron
// {
//     float Value { get; set; }
// }

interface IInputNeuron // : INeuron
{
    float Value { get; set; }

    Connections Outs { get; init; }
}

interface IOutputNeuron // : INeuron
{
    Connections Ins { get; init; }
}