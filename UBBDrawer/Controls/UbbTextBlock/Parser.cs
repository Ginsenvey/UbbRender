using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Documents;
using System;
using System.Collections.Generic;
using System.Linq;

namespace UbbRender.Common;






public class Parser
{
    private readonly List<Token> _tokens;
    private int _position;
    private readonly Stack<TagNode> _nodeStack = new();
    private readonly UbbDocument _document;
    // 自闭合标签列表
    private static readonly HashSet<string> _selfClosingTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "br","line" 
    };

    // 块级标签列表（需要创建新段落）
    private static readonly HashSet<string> _blockTags = new(StringComparer.OrdinalIgnoreCase)
    {
        "quote", "code", "list", "center", "left", "right", "align"
    };

    public Parser(List<Token> tokens)
    {
        _tokens = tokens;
        _document = new UbbDocument();
        _nodeStack.Push(_document.Root);
    }

    public UbbDocument Parse()
    {
        _position = 0;

        while (_position < _tokens.Count)
        {
            var token = _tokens[_position];

            switch (token.Type)
            {
                case TokenType.Text:
                    ProcessTextToken(token);
                    break;

                case TokenType.OpenTag:
                    ProcessOpenTagToken(token);
                    break;

                case TokenType.CloseTag:
                    ProcessCloseTagToken(token);
                    break;

                case TokenType.SelfCloseTag:
                    ProcessSelfCloseTagToken(token);
                    break;

                case TokenType.NewLine:
                    ProcessNewLineToken(token);
                    break;
            }

            _position++;
        }

        // 清理未闭合的标签
        while (_nodeStack.Count > 1)
        {
            _nodeStack.Pop();
        }

        return _document;
    }

    private void ProcessTextToken(Token token)
    {
        if (!string.IsNullOrWhiteSpace(token.Value))
        {
            var textNode = new TextNode(token.Value);

            // 检查栈是否为空
            if (_nodeStack.Count > 0)
            {
                // 添加到栈顶节点
                _nodeStack.Peek().AddChild(textNode);
            }
            else
            {
                // 栈为空，添加到文档根节点
                _document.Root.AddChild(textNode);
            }

            _document.AllNodes.Add(textNode);
        }
    }

    private void ProcessOpenTagToken(Token token)
    {
        var tagInfo = ParseTagInfo(token.Value);

        // 检查是否为自闭合标签
        if (_selfClosingTags.Contains(tagInfo.Name))
        {
            ProcessSelfClosingTag(tagInfo);
            return;
        }
        // 检查是否为表情标签
        if (IsEmoticonTag(tagInfo.Name))
        {
            ProcessEmoticonTag(tagInfo);
            return;
        }

        // 检查是否为分割线标签
        if (tagInfo.Name.Equals("line", StringComparison.OrdinalIgnoreCase))
        {
            ProcessLineTag(tagInfo);
            return;
        }
        // 创建新节点
        var newNode = CreateTagNode(tagInfo);
        _nodeStack.Peek().AddChild(newNode);
        _document.AllNodes.Add(newNode);

        // 块级标签处理
        if (_blockTags.Contains(tagInfo.Name))
        {
            // 确保块级标签内有段落容器
            var paragraph = TagNode.Create(UbbNodeType.Paragraph);
            newNode.AddChild(paragraph);
            _nodeStack.Push(paragraph);
        }

        _nodeStack.Push(newNode);
    }
    private bool IsEmoticonTag(string tagName)
    {
        return EmoticonRules.IsEmoticonTag(tagName);
    }

    // 处理表情标签
    private void ProcessEmoticonTag(TagInfo tagInfo)
    {
        var node = TagNode.Create(UbbNodeType.Emoji, tagInfo.Attributes);
        // 将完整的标签名作为属性保存
        node.Attributes["code"] = tagInfo.Name;

        _nodeStack.Peek().AddChild(node);
        _document.AllNodes.Add(node);

        // 表情标签是自闭合的，不入栈
    }

    // 处理分割线标签
    private void ProcessLineTag(TagInfo tagInfo)
    {
        var node = TagNode.Create(UbbNodeType.Divider, tagInfo.Attributes);

        _nodeStack.Peek().AddChild(node);
        _document.AllNodes.Add(node);

        // 分割线标签是自闭合的，不入栈
    }
    private void ProcessCloseTagToken(Token token)
    {
        var tagName = token.Value.ToLowerInvariant();
        var nodeTypeToFind = GetNodeTypeFromTagName(tagName);

        // 查找栈中匹配的标签
        var nodesToPop = new List<TagNode>();
        var found = false;

        // 临时复制栈以进行搜索
        var tempStack = new Stack<TagNode>(_nodeStack.Reverse()); // 需要反转以获得正确的顺序

        foreach (var node in tempStack)
        {
            if (node.Type == nodeTypeToFind)
            {
                found = true;
                break;
            }
            nodesToPop.Add(node);
        }

        if (found)
        {
            // 弹出所有直到匹配标签的节点
            foreach (var nodeToPop in nodesToPop)
            {
                if (_nodeStack.Count > 0 && _nodeStack.Peek() == nodeToPop)
                {
                    _nodeStack.Pop();
                }
            }

            // 弹出匹配的标签本身
            if (_nodeStack.Count > 0 && _nodeStack.Peek().Type == nodeTypeToFind)
            {
                _nodeStack.Pop();
            }

            // 如果是块级标签，检查是否需要弹出段落容器
            if (_blockTags.Contains(tagName))
            {
                // 如果栈顶是段落，并且这个段落是块级标签的子节点，则弹出它
                if (_nodeStack.Count > 0 && _nodeStack.Peek().Type == UbbNodeType.Paragraph)
                {
                    // 检查这个段落的父节点是否是刚刚关闭的块级标签
                    var paragraph = _nodeStack.Peek();
                    var paragraphParent = paragraph.Parent as TagNode;

                    // 如果段落的父节点是刚刚关闭的块级标签类型，则弹出段落
                    if (paragraphParent != null && paragraphParent.Type == nodeTypeToFind)
                    {
                        _nodeStack.Pop();
                    }
                }
            }
        }
    }

    private void ProcessSelfCloseTagToken(Token token)
    {
        var tagInfo = ParseTagInfo(token.Value);
        if (IsEmoticonTag(tagInfo.Name))
        {
            ProcessEmoticonTag(tagInfo);
            return;
        }
        ProcessSelfClosingTag(tagInfo);
    }

    private void ProcessSelfClosingTag(TagInfo tagInfo)
    {
        var node = CreateTagNode(tagInfo);
        _nodeStack.Peek().AddChild(node);
        _document.AllNodes.Add(node);
    }

    private void ProcessNewLineToken(Token token)
    {
        // 创建 LineBreak 节点
        var lineBreak = TagNode.Create(UbbNodeType.LineBreak);

        // 添加到当前节点
        if (_nodeStack.Count > 0)
        {
            _nodeStack.Peek().AddChild(lineBreak);
        }
        else
        {
            _document.Root.AddChild(lineBreak);
        }
        _document.AllNodes.Add(lineBreak);
    }


    private TagNode CreateTagNode(TagInfo tagInfo)
    {
        var nodeType = GetNodeTypeFromTagName(tagInfo.Name);
        var attributes = tagInfo.Attributes;
        //专用属性名
        if (attributes.ContainsKey("value"))
        {
            var newAttributes = new Dictionary<string, string>(attributes);
            var value = newAttributes["value"];
            newAttributes.Remove("value");

            // 根据标签类型确定正确的属性名
            switch (nodeType)
            {
                case UbbNodeType.Image:
                    newAttributes["src"] = value;
                    break;
                case UbbNodeType.Size:
                    newAttributes["size"] = value;
                    break;
                case UbbNodeType.Font:
                    newAttributes["font"] = value;
                    break;
                case UbbNodeType.Color:
                    newAttributes["color"] = value;
                    break;
                case UbbNodeType.Url:
                    newAttributes["href"] = value;
                    break;
                case UbbNodeType.Audio:
                    newAttributes["src"] = value;
                    break;
                case UbbNodeType.Video:
                    newAttributes["src"] = value;
                    break;
                case UbbNodeType.Align:
                    newAttributes["align"] = value;
                    break;
                case UbbNodeType.Code:
                    newAttributes["language"] = value;
                    break;
                case UbbNodeType.Quote:
                    newAttributes["author"] = value;
                    break;
                case UbbNodeType.Emoji:
                    newAttributes["code"] = value;
                    break;
                default:
                    // 其他标签保持 value 属性
                    newAttributes["value"] = value;
                    break;
            }
            attributes = newAttributes;
        }

        var node = TagNode.Create(nodeType, attributes);
        return node;
    }
    /// <summary>
    /// 从标签名获取节点类型
    /// </summary>
    /// <param name="tagName"></param>
    /// <returns></returns>
    private UbbNodeType GetNodeTypeFromTagName(string tagName)
    {
        if (IsEmoticonTag(tagName))
            return UbbNodeType.Emoji;
        return tagName.ToLowerInvariant() switch
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
            "audio" => UbbNodeType.Audio,
            "video" => UbbNodeType.Video,
            "code" => UbbNodeType.Code,
            "quote" => UbbNodeType.Quote,
            "align" => UbbNodeType.Align,
            "left" => UbbNodeType.Left,
            "center" => UbbNodeType.Center,
            "right" => UbbNodeType.Right,
            "list" => UbbNodeType.List,
            "*" => UbbNodeType.ListItem,
            "p" => UbbNodeType.Paragraph,
            "br" => UbbNodeType.LineBreak,
            "line" => UbbNodeType.Divider,
            "math"=>UbbNodeType.Latex,
            _ => UbbNodeType.Text // 未知标签作为文本处理
        };
    }

    private TagInfo ParseTagInfo(string tagContent)
    {
        var tagInfo = new TagInfo();
        if (IsEmoticonTag(tagContent))
        {
            tagInfo.Name = tagContent.ToLowerInvariant();
            return tagInfo;
        }
        // 分离标签名和属性
        var spaceIndex = tagContent.IndexOf(' ');
        if (spaceIndex > 0)
        {
            tagInfo.Name = tagContent.Substring(0, spaceIndex).ToLowerInvariant();
            var attrString = tagContent.Substring(spaceIndex + 1).Trim();

            // 解析属性
            tagInfo.Attributes = ParseAttributes(attrString);
        }
        else
        {
            // 处理带等号的简写形式，如 [size=16] 或 [img=www.xxx.png]
            if (tagContent.Contains('='))
            {
                var parts = tagContent.Split('=', 2);
                tagInfo.Name = parts[0].ToLowerInvariant();
                var value = parts[1].Trim('"').Trim('\'');

                // 暂时都使用 "value" 作为属性名
                tagInfo.Attributes["value"] = value;
            }
            else
            {
                tagInfo.Name = tagContent.ToLowerInvariant();
            }
        }

        return tagInfo;
    }

    private Dictionary<string, string> ParseAttributes(string attrString)
    {
        var attributes = new Dictionary<string, string>();

        var parts = attrString.Split(' ');
        foreach (var part in parts)
        {
            if (string.IsNullOrWhiteSpace(part))
                continue;

            var equalIndex = part.IndexOf('=');
            if (equalIndex > 0)
            {
                var key = part.Substring(0, equalIndex).ToLowerInvariant();
                var value = part.Substring(equalIndex + 1).Trim('"').Trim('\'');
                attributes[key] = value;
            }
            else
            {
                attributes["value"] = part.Trim('"').Trim('\'');
            }
        }

        return attributes;
    }

    private class TagInfo
    {
        public string Name { get; set; }
        public Dictionary<string, string> Attributes { get; set; } = new();
    }
}
/// <summary>
/// 解析入口点
/// </summary>
public class UbbParser
{
    public static UbbDocument Parse(string ubbText)
    {
        // 词法分析
        var tokenizer = new Tokenizer(ubbText);
        var tokens = tokenizer.Tokenize();

        // 语法分析
        var parser = new Parser(tokens);
        var document = parser.Parse();

        return document;
    }
}

