using UBBParser.Scanner;

namespace UBBParser.Parser;

public class UBBParser
{
    private readonly IReadOnlyList<Token> _tokens;
    private int _index = 0;
    private readonly List<UbbNode> _allNodes = new();

    public UBBParser(IEnumerable<Token> tokens)
    {
        _tokens = tokens.ToList();
    }

    private Token Peek() => _index < _tokens.Count ? _tokens[_index] : new Token(TokenType.EOF, "", -1);
    private Token Consume() => _tokens[_index++];

    public UbbDocument Parse()
    {
        var doc = new UbbDocument();
        // 递归解析根节点，直到 EOF
        ParseContent(doc.Root, null);
        return doc;
    }

    /// <param name="parent">当前父节点</param>
    /// <param name="closingTag">期望遇到的闭合标签名，为 null 则解析到 EOF</param>
    private void ParseContent(UbbNode parent, string closingTag = null)
    {
        while (_index < _tokens.Count)
        {
            var token = Peek();

            // 处理闭合标签 [/tag]
            if (token.Type == TokenType.LeftBracket && PeekNext()?.Type == TokenType.Slash)
            {
                var nextTag = PeekOffset(2);
                if (nextTag != null && closingTag != null &&
                    nextTag.Value.Equals(closingTag, StringComparison.OrdinalIgnoreCase))
                {
                    // 消耗掉 [/tag] 全套 token
                    Consume(); // [
                    Consume(); // /
                    Consume(); // tagName
                    if (Peek().Type == TokenType.RightBracket) Consume(); // ]
                    return; // 匹配成功，返回上层递归
                }
            }

            // 解析一个原子元素并加入父节点
            var node = ParseElement();
            if (node != null)
            {
                parent.AddChild(node);
                // 如果是需要闭合的标签节点，则开启递归
                if (node is TagNode tag && !IsSelfClosing(tag.Type))
                {
                    ParseContent(tag, GetTagNameFromType(tag.Type));
                }
            }
        }
    }

    private UbbNode ParseElement()
    {
        var token = Consume();
        switch (token.Type)
        {
            case TokenType.Text:
                return new TextNode(token.Value);

            case TokenType.Dollar:
            case TokenType.DoubleDollar:
                bool isBlock = token.Type == TokenType.DoubleDollar;
                string latex = "";
                if (Peek().Type == TokenType.Text) latex = Consume().Value;
                if (Peek().Type == token.Type) Consume(); // 消耗结尾的 $ 或 $$
                return new LatexNode(latex, isBlock);

            case TokenType.LeftBracket:
                return ParseTagHeader();

            default:
                return null;
        }
    }

    private TagNode ParseTagHeader()
    {
        // 此时已消耗 '['，接下来应是 TagName
        if (Peek().Type != TokenType.TagName) return null;

        string name = Consume().Value;
        var attributes = new Dictionary<string, string>();
        int attrCount = 0;

        // 解析属性 [tag=val1,val2]
        while (Peek().Type != TokenType.RightBracket && Peek().Type != TokenType.EOF)
        {
            var t = Consume();
            if (t.Type == TokenType.Equal || t.Type == TokenType.Comma)
            {
                if (Peek().Type == TokenType.AttrValue)
                {
                    string key = attrCount == 0 ? "default" : attrCount.ToString();
                    attributes[key] = Consume().Value;
                    attrCount++;
                }
            }
        }

        if (Peek().Type == TokenType.RightBracket) Consume();

        return TagNode.Create(MapToNodeType(name), attributes);
    }

    #region 辅助工具

    private Token PeekNext() => PeekOffset(1);
    private Token PeekOffset(int offset) => (_index + offset < _tokens.Count) ? _tokens[_index + offset] : null;

    private bool IsSelfClosing(UbbNodeType type) =>
        type is UbbNodeType.LineBreak or UbbNodeType.Divider or UbbNodeType.Emoji or UbbNodeType.Image;

    private string GetTagNameFromType(UbbNodeType type) => type.ToString().ToLower();

    private UbbNodeType MapToNodeType(string tagName)
    {
        tagName = tagName.ToLower();
        // 处理 CC98 特有的表情前缀
        if (tagName.StartsWith("ac") || tagName.StartsWith("em") || tagName.StartsWith("cc98"))
            return UbbNodeType.Emoji;

        return tagName switch
        {
            "b" => UbbNodeType.Bold,
            "i" => UbbNodeType.Italic,
            "u" => UbbNodeType.Underline,
            "del" => UbbNodeType.Strikethrough,
            "url" => UbbNodeType.Url,
            "img" => UbbNodeType.Image,
            "hr" => UbbNodeType.Divider,
            "br" => UbbNodeType.LineBreak,
            "code" => UbbNodeType.Code,
            "quote" => UbbNodeType.Quote,
            "list" => UbbNodeType.List,
            "*" => UbbNodeType.ListItem,
            "bilibili" => UbbNodeType.Bilibili,
            _ => UbbNodeType.Text
        };
    }
    #endregion
}