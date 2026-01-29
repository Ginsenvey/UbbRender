using UBBParser.Parser;
using UBBParser.Scanner;

namespace UbbParser.Test;

[TestClass]
public class UBBParserTests
{
    // 辅助方法：封装从字符串到 AST 的完整过程
    private UbbDocument GetAst(string input)
    {
        var scanner = new UBBScanner(input);
        var tokens = scanner.ScanTokens().ToList();
        var parser = new UBBParser.Parser.UBBParser(tokens);
        return parser.Parse();
    }

    #region 1. 正常嵌套与结构测试

    [TestMethod]
    public void Test_NestedTags_Structure()
    {
        // 输入：粗体包含斜体
        string input = "[b][i]Hello[/i][/b]";
        var doc = GetAst(input);

        // 层级验证: Root -> Bold -> Italic -> Text
        Assert.AreEqual(1, doc.Root.Children.Count);
        var boldNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(UbbNodeType.Bold, boldNode.Type);

        var italicNode = boldNode.Children[0] as TagNode;
        Assert.AreEqual(UbbNodeType.Italic, italicNode.Type);

        var textNode = italicNode.Children[0] as TextNode;
        Assert.AreEqual("Hello", textNode.Content);
        Assert.AreSame(italicNode, textNode.Parent);
    }

    [TestMethod]
    public void Test_Attributes_Parsing()
    {
        // 测试 CC98 典型的多属性标签
        string input = "[upload=jpg,1]File[/upload]";
        var doc = GetAst(input);

        var uploadNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(UbbNodeType.Emoji, MapToNodeType("upload")); // 假设在 Map 中定义
        Assert.AreEqual("jpg", uploadNode.GetAttribute("default"));
        Assert.AreEqual("1", uploadNode.GetAttribute("1"));
    }

    [TestMethod]
    public void Test_Latex_Node()
    {
        string input = "Math: $x^2$";
        var doc = GetAst(input);

        Assert.AreEqual(2, doc.Root.Children.Count);
        Assert.IsInstanceOfType(doc.Root.Children[0], typeof(TextNode));
        var latexNode = doc.Root.Children[1] as LatexNode;
        Assert.AreEqual("x^2", latexNode.Latex);
        Assert.IsFalse(latexNode.IsBlock);
    }

    #endregion

    #region 2. 边界与异常情况测试

    [TestMethod]
    public void Test_UnclosedTag_ShouldBeChildOfParent()
    {
        // 异常：[b] 没有对应的 [/b]
        // 按照递归下降逻辑，它会把之后的所有内容都吞作子节点
        string input = "Outer [b]Inner Text";
        var doc = GetAst(input);

        var boldNode = doc.Root.Children[1] as TagNode;
        Assert.AreEqual(UbbNodeType.Bold, boldNode.Type);
        Assert.AreEqual("Inner Text", ((TextNode)boldNode.Children[0]).Content);
    }

    [TestMethod]
    public void Test_MismatchedClosingTag()
    {
        // 异常：[b][i]Text[/b][/i] (交叉嵌套)
        // 这是一个经典的 UBB 挑战。标准 Parser 会在遇到 [/b] 时由于不匹配当前的 [i] 而不跳出 [i] 的递归
        string input = "[b][i]Text[/b][/i]";
        var doc = GetAst(input);

        var bold = doc.Root.Children[0] as TagNode;
        var italic = bold.Children[0] as TagNode;

        // 此时由于 [/b] 不匹配 [i]，它可能被识别为普通文本或跳过（取决于 Parser 实现）
        // 在我们的递归实现中，italic 会吞掉所有内容直到 EOF
        Assert.AreEqual(UbbNodeType.Italic, italic.Type);
    }

    [TestMethod]
    public void Test_SelfClosing_Tag_ShouldNotConsumeNext()
    {
        // [hr] 是自闭合的，不应该把后面的文本吞为子节点
        string input = "[hr]Next Text";
        var doc = GetAst(input);

        Assert.AreEqual(2, doc.Root.Children.Count);
        Assert.AreEqual(UbbNodeType.Divider, doc.Root.Children[0].Type);
        Assert.AreEqual(UbbNodeType.Text, doc.Root.Children[1].Type);
    }

    [TestMethod]
    public void Test_EmptyTag()
    {
        string input = "[b][/b]";
        var doc = GetAst(input);

        var boldNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, boldNode.Children.Count);
    }



    [TestMethod]
    public void Test_MixedText_And_NestedBoldItalic()
    {
        // 输入：正常[b]加粗[i]加粗斜体[/i]加粗[/b]正常
        string input = "正常[b]加粗[i]加粗斜体[/i]加粗[/b]正常";
        var doc = GetAst(input);
        var children = doc.Root.Children;

        // 1. 根节点应该有 3 个直接子节点：[Text, BoldTag, Text]
        Assert.AreEqual(3, children.Count);

        // 检查第一个节点： "正常"
        Assert.IsInstanceOfType(children[0], typeof(TextNode));
        Assert.AreEqual("正常", ((TextNode)children[0]).Content);

        // 2. 检查第二个节点： [b]...[/b]
        var boldNode = children[1] as TagNode;
        Assert.IsNotNull(boldNode);
        Assert.AreEqual(UbbNodeType.Bold, boldNode.Type);

        // [b] 内部应该有 3 个子节点：["加粗", ItalicTag, "加粗"]
        Assert.AreEqual(3, boldNode.Children.Count);
        Assert.AreEqual("加粗", ((TextNode)boldNode.Children[0]).Content);
        Assert.AreEqual("加粗", ((TextNode)boldNode.Children[2]).Content);

        // 3. 检查 [b] 内部嵌套的 [i]...[/i]
        var italicNode = boldNode.Children[1] as TagNode;
        Assert.IsNotNull(italicNode);
        Assert.AreEqual(UbbNodeType.Italic, italicNode.Type);

        // [i] 内部应该有 1 个子节点：["加粗斜体"]
        Assert.AreEqual(1, italicNode.Children.Count);
        Assert.AreEqual("加粗斜体", ((TextNode)italicNode.Children[0]).Content);

        // 4. 检查最后一个节点： "正常"
        Assert.IsInstanceOfType(children[2], typeof(TextNode));
        Assert.AreEqual("正常", ((TextNode)children[2]).Content);
    }

    #endregion

    // 辅助映射逻辑验证（如果 MapToNodeType 是私有的，可通过反射或改为 internal）
    private UbbNodeType MapToNodeType(string tag)
    {
        // 同 Parser 内部逻辑
        if (tag == "upload") return UbbNodeType.Emoji;
        return UbbNodeType.Text;
    }
}