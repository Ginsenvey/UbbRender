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
        Assert.AreEqual(UbbNodeType.Upload, uploadNode.Type); // 假设在 Map 中定义
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
    public void Test_Emoji_AC()
    {
        string input = "[ac01]";
        var doc = GetAst(input);

        var emojiNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, emojiNode.Children.Count);
        Assert.AreEqual(UbbNodeType.Emoji, emojiNode.Type);
        Assert.AreEqual(emojiNode.Attributes.Count,1);
        Assert.IsTrue(emojiNode.Attributes.ContainsKey("code"));
        Assert.AreEqual(emojiNode.Attributes["code"], "ac01");
    }

    [TestMethod]
    public void Test_Emoji_TB()
    {
        string input = "[tb01]";
        var doc = GetAst(input);

        var emojiNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, emojiNode.Children.Count);
        Assert.AreEqual(UbbNodeType.Emoji, emojiNode.Type);
        Assert.AreEqual(emojiNode.Attributes.Count, 1);
        Assert.IsTrue(emojiNode.Attributes.ContainsKey("code"));
        Assert.AreEqual(emojiNode.Attributes["code"], "tb01");
    }

    [TestMethod]
    public void Test_Emoji_MS()
    {
        string input = "[ms01]";
        var doc = GetAst(input);

        var emojiNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, emojiNode.Children.Count);
        Assert.AreEqual(UbbNodeType.Emoji, emojiNode.Type);
        Assert.AreEqual(emojiNode.Attributes.Count, 1);
        Assert.IsTrue(emojiNode.Attributes.ContainsKey("code"));
        Assert.AreEqual(emojiNode.Attributes["code"], "ms01");
    }

    [TestMethod]
    public void Test_Emoji_EM()
    {
        string input = "[em11]";
        var doc = GetAst(input);

        var emojiNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, emojiNode.Children.Count);
        Assert.AreEqual(UbbNodeType.Emoji, emojiNode.Type);
        Assert.AreEqual(emojiNode.Attributes.Count, 1);
        Assert.IsTrue(emojiNode.Attributes.ContainsKey("code"));
        Assert.AreEqual(emojiNode.Attributes["code"], "em11");
    }

    [TestMethod]
    public void Test_Emoji_CC98()
    {
        string input = "[cc9801]";
        var doc = GetAst(input);

        var emojiNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, emojiNode.Children.Count);
        Assert.AreEqual(UbbNodeType.Emoji, emojiNode.Type);
        Assert.AreEqual(emojiNode.Attributes.Count, 1);
        Assert.IsTrue(emojiNode.Attributes.ContainsKey("code"));
        Assert.AreEqual(emojiNode.Attributes["code"], "cc9801");
    }

    [DataTestMethod]
    [DataRow("a:001")]
    [DataRow("c:001")]
    [DataRow("f:001")]
    public void Test_Emoji_MahjongTagHandler(string emojiCode)
    {
        string input = $"[{emojiCode}]";
        var doc = GetAst(input);

        var emojiNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(0, emojiNode.Children.Count);
        Assert.AreEqual(UbbNodeType.Emoji, emojiNode.Type);
        Assert.AreEqual(emojiNode.Attributes.Count, 1);
        Assert.IsTrue(emojiNode.Attributes.ContainsKey("code"));
        Assert.AreEqual(emojiNode.Attributes["code"], emojiCode);
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
    [TestMethod]
    public void Test_UnknownTag_ShouldFallbackToText()
    {
        // 输入包含一个不存在的标签 [ghost]
        string input = "Hello [ghost]World[/ghost]";
        var doc = GetAst(input);

        // 预期结构：Root -> TextNode("Hello "), TextNode("[ghost"), TextNode("World"), TextNode("[/ghost")
        // 注意：由于我们将未知标签识别为 Text，它们会被切分成多个 TextNode 或保持独立
        var textNodes = doc.Root.Children.OfType<TextNode>().ToList();

        Assert.IsTrue(textNodes.Any(n => n.Content.Contains("[ghost")));
        Assert.IsFalse(doc.Root.Children.Any(n => n is TagNode));
    }

    [TestMethod]
    public void Test_UnclosedTag_AtEndOfDocument()
    {
        // 输入标签没有闭合括号 ']'
        string input = "This is [b bold text";
        var doc = GetAst(input);

        // 预期：[b 应该被识别为 TextNode
        var textNode = doc.Root.Children.Last() as TextNode;
        Assert.IsNotNull(textNode);
        Assert.IsTrue(textNode.Content.Contains("[b"));
    }

    [TestMethod]
    public void Test_MalformedNestedBrackets()
    {
        // 标签内部非法嵌套了另一个左括号 [url=[b]]
        string input = "[url=[b]]Link[/url]";
        var doc = GetAst(input);

        // 按照我们的逻辑，第一个 [url=[ 扫描到第二个 [ 时发现错误，回退为文本
        // 最终会解析成文本 + [b]]...
        Assert.IsInstanceOfType(doc.Root.Children[0], typeof(TextNode));
        Assert.IsTrue(((TextNode)doc.Root.Children[0]).Content.StartsWith("[url="));
    }

    [TestMethod]
    public void Test_LoneClosingTag()
    {
        // 只有闭合标签，没有开始标签
        string input = "Just a closing tag [/b]";
        var doc = GetAst(input);

        // ParseContent 会因为找不到匹配的 closingTag 而跳过处理，
        // ParseElement 会将其中的 [ / b ] 分别或合并识别为 Text
        var nodes = doc.Root.Children;
        Assert.IsFalse(nodes.Any(n => n is TagNode));
    }

    [TestMethod]
    public void Test_AttributeWithoutValue_Fallback()
    {
        // 属性格式错误，如 [color=]
        string input = "[color=]Text[/color]";
        var doc = GetAst(input);

        // 取决于实现：如果 ParseTagHeader 允许空属性，它是一个 TagNode；
        // 如果不允许且回退，它是一个 TextNode。
        // 根据我们的逻辑，它会寻找 AttrValue，找不到则 rawAttributesText 只包含 "="
        var firstNode = doc.Root.Children[0];
        Assert.AreEqual(UbbNodeType.Color, firstNode.Type);
    }

    [TestMethod]
    public void Test_CodeTag_ContentIsLiteral()
    {
        // [code] 内部的 [b] 不应该被解析成标签，而是纯文本
        string input = "[code]var x = [b]test[/b];[/code]";
        var doc = GetAst(input);

        var codeNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(UbbNodeType.Code, codeNode.Type);

        // 子节点应该只有一个 TextNode
        Assert.AreEqual(1, codeNode.Children.Count);
        var content = (codeNode.Children[0] as TextNode).Content;
        Assert.AreEqual("var x = [b]test[/b];", content);
    }

    [TestMethod]
    public void Test_NoUBBTag_WithMismatchedBrackets()
    {
        // [nobb] 用于展示 UBB 教程，内部包含各种乱码和括号
        string input = "[noubb][[[/noubb]";
        var doc = GetAst(input);

        var nobbNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(UbbNodeType.NoUBB, nobbNode.Type);
        Assert.AreEqual("[[", (nobbNode.Children[0] as TextNode).Content);
    }

    [TestMethod]
    public void Test_CodeTag_Unclosed_ConsumesUntilEOF()
    {
        // 如果没有闭合标签，内容应该一直到文档末尾
        string input = "[code]incomplete content...";
        var doc = GetAst(input);

        var codeNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual("incomplete content...", (codeNode.Children[0] as TextNode).Content);
    }

    [TestMethod]
    public void Test_NestedNoBB_Balanced()
    {
        // 输入：[noubb][noubb]Test[/noubb][/noubb]
        // 外部 noubb 的内容应该是 "[noubb]Test[/noubb]"
        string input = "[noubb][noubb]Test[/noubb][/noubb]";
        var doc = GetAst(input);

        var outerNode = doc.Root.Children[0] as TagNode;
        Assert.AreEqual(UbbNodeType.NoUBB, outerNode.Type);

        var content = (outerNode.Children[0] as TextNode).Content;
        Assert.AreEqual("[noubb]Test[/noubb]", content);
    }

    [TestMethod]
    public void Test_Noubb_With_Extra_Closing_Tag()
    {
        // 输入：[noubb]Test[/noubb][/noubb]
        // 这是你提到的情况。第一个 [/noubb] 会使 depth 归零，解析结束。
        // 剩下的 [/noubb] 应该作为后续的普通 Text 处理。
        string input = "[noubb]Test[/noubb][/noubb]";
        var doc = GetAst(input);

        Assert.AreEqual(2, doc.Root.Children.Count);
        Assert.IsInstanceOfType(doc.Root.Children[0], typeof(TagNode)); // noubb 节点
        Assert.IsInstanceOfType(doc.Root.Children[1], typeof(TextNode)); // 剩下的 [/noubb]
        Assert.AreEqual("[/noubb]", (doc.Root.Children[1] as TextNode).Content);
    }
    #endregion


}