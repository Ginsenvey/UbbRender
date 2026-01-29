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
    private void ParseContent(UbbNode parent, UbbNodeType? closingTag)
    {
        while (_index < _tokens.Count)
        {
            var token = Peek();

            // 检查是否遇到了闭合标签 [/xxx]
            if (token.Type == TokenType.LeftBracket && PeekNext()?.Type == TokenType.Slash)
            {
                var nextTagNameToken = PeekOffset(2);
                if (nextTagNameToken?.Type == TokenType.TagName)
                {
                    string foundClosingName = nextTagNameToken.Value;

                    // 情况 A: 正好是当前正在等待的闭合标签 (如在 [i] 中找到了 [/i])
                    if (closingTag != null && closingTag== MapToNodeType(foundClosingName))
                    {
                        Consume(); // [
                        Consume(); // /
                        Consume(); // TagName
                        if (Peek().Type == TokenType.RightBracket) Consume(); // ]
                        return; // 正常结束当前标签的内容解析
                    }

                    // 情况 B: 这是一个闭合标签，但不匹配当前标签 (如在 [i] 中找到了 [/b])
                    if (closingTag != null)
                    {
                        // 检查这是否是一个“合法”的闭合标签（在全局枚举中存在）
                        // 如果是其他层的标签，我们直接退出，不消费 Token，让上层去匹配它
                        if (IsKnownTagName(foundClosingName))
                        {
                            return;
                        }
                    }
                }
            }

            // 正常解析元素
            var node = ParseElement();
            if (node != null)
            {
                parent.AddChild(node);

                // 递归向下
                if (node is TagNode tag && !IsSelfClosing(tag.Type))
                {
                    ParseContent(tag, tag.Type);
                }
            }
        }
    }

    // 新增辅助方法：判断是否是已定义的 UBB 标签
    private bool IsKnownTagName(string name)
    {
        return MapToNodeType(name) != UbbNodeType.Text;
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

    private bool IsSelfClosing(UbbNodeType type)
    {
        return type switch
        {
            UbbNodeType.Divider => true, // [hr]
            UbbNodeType.LineBreak => true, // [br]
            UbbNodeType.Emoji => true, // [ac01]
            _ => false
        };
    }

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
            "size" => UbbNodeType.Size,
            "font" => UbbNodeType.Font,
            "color" => UbbNodeType.Color,
            "url" => UbbNodeType.Url,
            "img" => UbbNodeType.Image,
            "audio"=>UbbNodeType.Audio,
            "video"=>UbbNodeType.Video,
            "code" => UbbNodeType.Code,
            "quote" => UbbNodeType.Quote,
            "align" => UbbNodeType.Align,
            "left" => UbbNodeType.Left,
            "right"=>UbbNodeType.Right,
            "list" => UbbNodeType.List,
            "*"=> UbbNodeType.ListItem,
            "hr" => UbbNodeType.Divider,
            "br" => UbbNodeType.LineBreak,
            "math"=>UbbNodeType.Latex,
            "bili" => UbbNodeType.Bilibili,
            _ => UbbNodeType.Text
        };
    }
    #endregion
}