using UbbRender.Common;
using CommunityToolkit.Mvvm.ComponentModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UBBParser.Parser;

namespace UBBDrawer;
public class UbbTreeItem:ObservableObject
{
    // 显示在 TreeView 中的文本
    private string _displayName=string.Empty;
    public string DisplayName
    {
        get=> _displayName;
        set=>SetProperty(ref _displayName,value);
    }

    // 节点类型，用于图标显示或其他样式
    private UbbNodeType _nodeType;
    public UbbNodeType NodeType
    {
        get=> _nodeType;
        set=>SetProperty(ref _nodeType,value);
    }

    // 如果是文本节点，显示内容
    private string _content=string.Empty;
    public string Content
    {
        get=> _content;
        set=>SetProperty(ref _content,value);
    }

    // 标签节点的属性
    private Dictionary<string, string> _atrributes=new();
    public Dictionary<string, string> Attributes
    {
        get => _atrributes;
        set=> SetProperty(ref _atrributes, value);
    }
    private ObservableCollection<UbbTreeItem> _children=new();
    // 子节点集合
    public ObservableCollection<UbbTreeItem> Children
    {
        get => _children;
        set=>SetProperty(ref _children,value);
    }
    private UbbNode _originalNode;
    // 原始节点引用（可选，便于后续操作）
    public UbbNode OriginalNode
    {
        get=> _originalNode;
        set=>SetProperty(ref _originalNode,value);
    }

}
public static class UbbTreeConverter
{
    public static ObservableCollection<UbbTreeItem> ConvertToTreeItems(UbbDocument document)
    {
        var rootItems = new ObservableCollection<UbbTreeItem>();

        if (document?.Root == null)
            return rootItems;

        // 处理根节点的子节点
        foreach (var childNode in document.Root.Children)
        {
            var treeItem = ConvertNode(childNode);
            if (treeItem != null)
            {
                rootItems.Add(treeItem);
            }
        }

        return rootItems;
    }

    private static UbbTreeItem ConvertNode(UbbNode node)
    {
        if (node == null)
            return null;

        var treeItem = new UbbTreeItem
        {
            NodeType = node.Type,
            OriginalNode = node
        };

        // 设置显示名称
        switch (node.Type)
        {
            case UbbNodeType.Document:
                treeItem.DisplayName = "[文档根节点]";
                break;

            case UbbNodeType.Text:
                var textNode = node as TextNode;
                treeItem.DisplayName = $"文本: {Truncate(textNode?.Content??"null文本", 20)}";
                treeItem.Content = textNode?.Content??"null内容";
                break;

            case UbbNodeType.Bold:
                treeItem.DisplayName = "[粗体]";
                break;

            case UbbNodeType.Italic:
                treeItem.DisplayName = "[斜体]";
                break;

            case UbbNodeType.Underline:
                treeItem.DisplayName = "[下划线]";
                break;

            case UbbNodeType.Strikethrough:
                treeItem.DisplayName = "[删除线]";
                break;

            case UbbNodeType.Size:
                treeItem.DisplayName = "[字体大小]";
                break;

            case UbbNodeType.Font:
                treeItem.DisplayName = "[字体]";
                break;

            case UbbNodeType.Color:
                treeItem.DisplayName = "[颜色]";
                break;

            case UbbNodeType.Url:
                treeItem.DisplayName = "[链接]";
                break;

            case UbbNodeType.Image:
                treeItem.DisplayName = "[图片]";
                break;

            case UbbNodeType.Audio:
                treeItem.DisplayName = "[音频]";
                break;

            case UbbNodeType.Video:
                treeItem.DisplayName = "[视频]";
                break;

            case UbbNodeType.Code:
                treeItem.DisplayName = "[代码块]";
                break;

            case UbbNodeType.Quote:
                treeItem.DisplayName = "[引用]";
                break;

            case UbbNodeType.Align:
                treeItem.DisplayName = "[对齐]";
                break;

            case UbbNodeType.Left:
                treeItem.DisplayName = "[左对齐]";
                break;

            case UbbNodeType.Center:
                treeItem.DisplayName = "[居中]";
                break;

            case UbbNodeType.Right:
                treeItem.DisplayName = "[右对齐]";
                break;

            case UbbNodeType.List:
                treeItem.DisplayName = "[列表]";
                break;

            case UbbNodeType.ListItem:
                treeItem.DisplayName = "[列表项]";
                break;

            case UbbNodeType.Paragraph:
                treeItem.DisplayName = "[段落]";
                break;

            case UbbNodeType.LineBreak:
                treeItem.DisplayName = "[换行]";
                break;

            default:
                treeItem.DisplayName = $"[{node.Type}]";
                break;
        }

        // 如果有属性，添加属性信息
        if (node is TagNode tagNode && tagNode.Attributes.Count > 0)
        {
            treeItem.Attributes = new Dictionary<string, string>(tagNode.Attributes);

            // 根据标签类型显示特定的属性
            var attributeInfo = "";

            switch (node.Type)
            {
                case UbbNodeType.Size:
                    var size = tagNode.GetAttribute("size", "");
                    if (!string.IsNullOrEmpty(size))
                        attributeInfo = $" size={size}";
                    break;

                case UbbNodeType.Font:
                    var font = tagNode.GetAttribute("font", "");
                    if (!string.IsNullOrEmpty(font))
                        attributeInfo = $" font={font}";
                    break;

                case UbbNodeType.Color:
                    var color = tagNode.GetAttribute("color", "");
                    if (!string.IsNullOrEmpty(color))
                        attributeInfo = $" color={color}";
                    break;

                case UbbNodeType.Url:
                    var href = tagNode.GetAttribute("href", tagNode.GetAttribute("url", ""));
                    if (!string.IsNullOrEmpty(href))
                        attributeInfo = $" href={href}";
                    break;

                case UbbNodeType.Image:
                    var src = tagNode.GetAttribute("src", "");
                    if (!string.IsNullOrEmpty(src))
                        attributeInfo = $" src={src}";
                    break;

                case UbbNodeType.Audio:
                    var audioSrc = tagNode.GetAttribute("src", "");
                    if (!string.IsNullOrEmpty(audioSrc))
                        attributeInfo = $" src={audioSrc}";
                    break;

                case UbbNodeType.Video:
                    var videoSrc = tagNode.GetAttribute("src", "");
                    if (!string.IsNullOrEmpty(videoSrc))
                        attributeInfo = $" src={videoSrc}";
                    break;

                case UbbNodeType.Code:
                    var language = tagNode.GetAttribute("language", "");
                    if (!string.IsNullOrEmpty(language))
                        attributeInfo = $" language={language}";
                    break;

                case UbbNodeType.Quote:
                    var author = tagNode.GetAttribute("author", "");
                    if (!string.IsNullOrEmpty(author))
                        attributeInfo = $" author={author}";
                    break;

                case UbbNodeType.Align:
                    var align = tagNode.GetAttribute("align", "");
                    if (!string.IsNullOrEmpty(align))
                        attributeInfo = $" align={align}";
                    break;

                case UbbNodeType.List:
                    var listType = tagNode.GetAttribute("type", "");
                    if (!string.IsNullOrEmpty(listType))
                        attributeInfo = $" type={listType}";
                    break;
            }

            // 如果没有特定的属性处理，显示所有属性
            if (string.IsNullOrEmpty(attributeInfo))
            {
                attributeInfo = " (";
                bool first = true;
                foreach (var attr in tagNode.Attributes)
                {
                    if (!first)
                        attributeInfo += "; ";
                    attributeInfo += $"{attr.Key}={attr.Value}";
                    first = false;
                }
                attributeInfo += ")";
            }

            if (!string.IsNullOrEmpty(attributeInfo))
            {
                treeItem.DisplayName += attributeInfo;
            }
        }

        // 递归处理子节点
        foreach (var child in node.Children)
        {
            var childTreeItem = ConvertNode(child);
            if (childTreeItem != null)
            {
                treeItem.Children.Add(childTreeItem);
            }
        }

        return treeItem;
    }

    private static string Truncate(string text, int maxLength)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
    }
}

