namespace UBBParser.Parser;

public class LatexNode(string latex, bool isBlock) : UbbNode
{
    public string Latex { get; } = latex;
    public bool IsBlock { get; } = isBlock;
    public override UbbNodeType Type => UbbNodeType.Latex;
}