using ColorCode;
using CommunityToolkit.WinUI.UI.Controls.Markdown.Render;
using Microsoft.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.ApplicationModel.DataTransfer;
using Windows.UI;
using Windows.UI.Text;
using static System.Net.WebRequestMethods;
using LatexRender.Render;
using UBBParser.Parser;

namespace UbbRender.Common;


// 渲染策略接口
public interface IRenderStrategy
{
    void Render(UbbNode node, RenderContext context);
}
public class TextRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TextNode textNode && !string.IsNullOrWhiteSpace(textNode.Content))
        {
            string content = textNode.Content;
            var run = new Run { Text = content };
            context.AddInline(run);
        }
    }
    
}

// 粗体渲染策略
public class BoldRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var bold = new Bold();
        context.BeginInlineContainer(bold);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
}

// 斜体渲染策略
public class ItalicRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var italic = new Italic();
        context.BeginInlineContainer(italic);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
}

// 下划线渲染策略
public class UnderlineRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var underline = new Underline();
        context.BeginInlineContainer(underline);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
}

// 删除线渲染策略
public class StrikethroughRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var span = new Span();
        span.TextDecorations = Windows.UI.Text.TextDecorations.Strikethrough;
        context.BeginInlineContainer(span);
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }
        context.EndInlineContainer();
    }
    private string CollectText(UbbNode node)
    {
        var text = "";
        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                text += textNode.Content;
            }
            else
            {
                text += CollectText(child);
            }
        }
        return text;
    }
}

// 字体大小渲染策略
public class SizeRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            
            var sizeStr = tagNode.GetAttribute("size");
            if(int.TryParse(sizeStr,out int sizeInt))
            {
                double pixels = sizeStr.Contains("px") ? sizeInt : ConvertUbbSizeToPixels(sizeInt);
                var span = new Span { FontSize = pixels };
                context.BeginInlineContainer(span);
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
                context.EndInlineContainer();
            }
            else
            {
                // 默认处理子节点
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
            }
        }
    }
    private double ConvertUbbSizeToPixels(int ubbSize)
    {
        // 简单的分段线性插值
        if (ubbSize <= 1) return 8;   // 最小尺寸
        if (ubbSize == 2) return 10;  // 较小
        if (ubbSize == 3) return 13;  
        if (ubbSize == 4) return 17;  // 插值
        if (ubbSize == 5) return 22;  
        if (ubbSize == 6) return 26;  // 插值
        if (ubbSize == 7) return 30;  // 插值
        if (ubbSize == 8) return 32;  // 插值
        if (ubbSize == 9) return 34;  // 插值
        if (ubbSize == 10) return 35; // 接近36
        if (ubbSize == 11) return 35.5;
        if (ubbSize == 12) return 35.8;
        if (ubbSize == 13) return 36; 

        // 对于更大的值，使用渐进增长
        if (ubbSize > 13)
        {
            // 超过13后缓慢增长
            return 36 + (ubbSize - 13) * 0.5;
        }

        return 14; // 默认值
    }
}

// 链接渲染策略
public class UrlRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var hyperlink = new Hyperlink();
            var url = tagNode.GetAttribute("href");
            if (!string.IsNullOrEmpty(url))
            {
                try
                {
                    // 使用RelativeOrAbsolute，允许相对路径
                    hyperlink.NavigateUri = new Uri(url, UriKind.RelativeOrAbsolute);
                }
                catch { }
            }
            // 设置样式
            hyperlink.Foreground = new SolidColorBrush(Colors.LightSeaGreen);
            hyperlink.TextDecorations = TextDecorations.Underline;
            // 点击事件
            hyperlink.Click += (sender, e) =>
            {
                if (hyperlink.NavigateUri != null)
                {
                    // TODO: 打开链接
                }
            };
            context.BeginInlineContainer(hyperlink);
            foreach (var child in node.Children)
            {
                context.RenderNode(child);
            }
            context.EndInlineContainer();
        }
    }
}

// 图片渲染策略
public class ImageRenderStrategy : IRenderStrategy
{
    public async void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var src = tagNode.GetAttribute("src");
            if (string.IsNullOrEmpty(src))
            {
                // 尝试从子节点获取URL（对于 [img]url[/img] 格式）
                foreach (var child in node.Children)
                {
                    if (child is TextNode textNode)
                    {
                        src = textNode.Content;
                        break;
                    }
                }
            }

            if (!string.IsNullOrEmpty(src))
            {
                var image = new Image()
                {
                    MaxWidth = (double)context.Properties["ImageMaxWidth"],
                    Stretch = Stretch.Uniform,
                };
                var hyperlinkButton = new HyperlinkButton
                {
                    Content = image,
                    Padding = new Thickness(1),
                    Background = new SolidColorBrush(Colors.Transparent),
                    HorizontalAlignment = HorizontalAlignment.Left,
                    HorizontalContentAlignment=HorizontalAlignment.Stretch
                };
                LoadImageAsync(image, src);

                context.AddToContainer(hyperlinkButton);
            }
        }
    }

    private async void LoadImageAsync(Image image, string src)
    {
        try
        {
            if (src.StartsWith("http", StringComparison.OrdinalIgnoreCase))
            {
                var bitmapImage = new BitmapImage(new Uri(src));
                image.Source = bitmapImage;
            }
            else
            {
                // 加载本地图片
                var bitmapImage = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage();
                var file = await Windows.Storage.StorageFile.GetFileFromPathAsync(src);
                var stream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read);
                await bitmapImage.SetSourceAsync(stream);
                image.Source = bitmapImage;
            }
        }
        catch
        {
            // 加载失败时显示占位符
            image.Source = new Microsoft.UI.Xaml.Media.Imaging.BitmapImage(
                new Uri("ms-appx:///Assets/ImageError.png"));
        }
    }
}

// 代码块渲染策略
public class CodeRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        var border = new Border
        {
            Background = (Brush)context.Properties["CodeBackground"],
            Padding = new Thickness(12),
            Margin = new Thickness(4, 2, 2, 0),
            CornerRadius = new CornerRadius(4)
        };

        var grid = new Grid();

        // 定义行：第一行用于显示语言标签，第二行用于代码
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 创建语言标签容器
        var languageContainer = new Grid
        {
            Margin = new Thickness(0, 0, 0, 0),
        };

        // 获取语言名称
        var languageName = node is TagNode tagNode ? tagNode.GetAttribute("language") : "cpp";
        if (string.IsNullOrEmpty(languageName))
            languageName = "cpp";

        // 获取友好的语言显示名称
        var language = ColorCode.Languages.FindById(languageName) ?? ColorCode.Languages.Cpp;
        var displayName = GetLanguageDisplayName(language, languageName);

        // 创建语言标签
        var languageTag = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x7A, 0xCC)), // 半透明蓝色
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var languageText = new TextBlock
        {
            Text = displayName,
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC)), // 蓝色文字
            VerticalAlignment=VerticalAlignment.Center
        };

        languageTag.Child = languageText;
        languageContainer.Children.Add(languageTag);

        // 添加到 Grid 的第一行
        Grid.SetRow(languageContainer, 0);
        grid.Children.Add(languageContainer);

        // 2. 创建代码区域
        var codeContainer = new Grid();
        Grid.SetRow(codeContainer, 1);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var textBlock = new RichTextBlock
        {
            FontFamily = new FontFamily("Consolas, 'Cascadia Code', monospace"),
            FontSize = (double)context.Properties["FontSize"] * 0.9,
            IsTextSelectionEnabled = true
        };

        var codeText = CollectText(node);
        var formatter = new RichTextBlockFormatter();
        formatter.FormatRichTextBlock(codeText, language, textBlock);
        AddCopyButton(languageContainer, codeText);
        scrollViewer.Content = textBlock;
        codeContainer.Children.Add(scrollViewer);

        grid.Children.Add(codeContainer);

        border.Child = grid;
        context.AddToContainer(border);
    }

    private string CollectText(UbbNode node)
    {
        var text = "";
        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                text += textNode.Content;
            }
            else
            {
                text += CollectText(child);
            }
        }
        return text;
    }
    private string GetLanguageDisplayName(ColorCode.ILanguage language, string languageName)
    {
        // 如果语言对象有 Name 属性，使用它
        if (!string.IsNullOrEmpty(language.Name))
            return language.Name;

        // 否则根据语言 ID 返回友好名称
        return languageName.ToLowerInvariant() switch
        {
            "cpp" => "C++",
            "csharp" or "cs" => "C#",
            "javascript" or "js" => "JavaScript",
            "typescript" or "ts" => "TypeScript",
            "python" or "py" => "Python",
            "java" => "Java",
            "html" => "HTML",
            "css" => "CSS",
            "sql" => "SQL",
            "php" => "PHP",
            "ruby" or "rb" => "Ruby",
            "go" => "Go",
            "rust" => "Rust",
            "swift" => "Swift",
            "kotlin" => "Kotlin",
            "dart" => "Dart",
            "markdown" or "md" => "Markdown",
            "yaml" => "YAML",
            "json" => "JSON",
            "xml" => "XML",
            "bash" or "sh" => "Bash",
            "powershell" or "ps" => "PowerShell",
            "plaintext" or "text" => "Plain Text",
            _ => CultureInfo.CurrentCulture.TextInfo.ToTitleCase(languageName)
        };
    }
    private void AddCopyButton(Grid languageContainer, string codeText)
    {
        var copyButton = new Button
        {
            Content = new FluentIcons.WinUI.SymbolIcon { Symbol=FluentIcons.Common.Symbol.Copy,FontSize=16},
            Margin = new Thickness(0, 0, 8, 0),
            Padding=new Thickness(0,0,0,0),
            BorderThickness = new Thickness(1),
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        copyButton.Click += (sender, e) =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(codeText);
            Clipboard.SetContent(dataPackage);

        };

        languageContainer.Children.Add(copyButton);
    }
  
}

// 引用渲染策略
public class QuoteRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();

        // 增加引用块嵌套层级
        int originalQuoteLevel = context.QuoteNestingLevel;
        context.QuoteNestingLevel++;
        bool isOutermostQuote = (context.QuoteNestingLevel == 1);

        var border = CreateQuoteBorder(context,isOutermostQuote);
        var contentPanel = new StackPanel
        {
            Spacing = 8,
            Margin = new Thickness(0)
        };

        // 添加作者信息（如果有）
        AddAuthorInfo(node, contentPanel, context);

        // 保存当前容器状态以便恢复
        var previousContainer = context.Container;
        var previousPanelStack = context.PanelStack != null ?
            new Stack<Panel>(context.PanelStack) : new Stack<Panel>();

        // 切换到新的内容容器
        context.Container = contentPanel;
        if (context.PanelStack != null)
        {
            context.PanelStack.Clear();
            context.PanelStack.Push(contentPanel);
        }

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 确保最后的文本块被结束
        context.FinalizeCurrentTextBlock();

        // 恢复之前的容器状态
        context.Container = previousContainer;
        if (context.PanelStack != null)
        {
            context.PanelStack.Clear();
            foreach (var panel in previousPanelStack.Reverse())
            {
                context.PanelStack.Push(panel);
            }
        }

        // 将内容面板添加到边框
        border.Child = contentPanel;

        // 将整个引用块添加到容器
        context.AddToContainer(border);
    }
    private Border CreateQuoteBorder(RenderContext context, bool isOutermostQuote)
    {
        var border = new Border
        {
            BorderBrush = new SolidColorBrush(Color.FromArgb(255, 255, 196, 174)),
            BorderThickness = new Thickness(2, 0, 0, 0),
            Padding = new Thickness(6, 6, 6, 6),
        };
        // 只有最外层引用块有背景色
        if (isOutermostQuote)
        {
            border.Background = (Brush)context.Properties["QuoteBackground"] ?? new SolidColorBrush(Color.FromArgb(255, 232, 244, 249));
            border.Margin = new Thickness(4, 6, 4, 6);
        }
        else
        {
            border.Background = new SolidColorBrush(Colors.Transparent);
            border.BorderThickness = new Thickness(2, 0, 0, 0);
            border.BorderBrush = new SolidColorBrush(Color.FromArgb(255, 220, 176, 154));
            border.Margin = new Thickness(2, 2, 0, 2); // 内层缩进
        }
        return border;
    }

    private void AddAuthorInfo(UbbNode node, StackPanel contentPanel, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var author = tagNode.GetAttribute("author");
            if (!string.IsNullOrEmpty(author))
            {
                var authorPanel = CreateAuthorPanel(author, context);
                contentPanel.Children.Add(authorPanel);
            }
        }
    }

    private StackPanel CreateAuthorPanel(string author, RenderContext context)
    {
        var authorPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 8,
            Margin = new Thickness(0, 0, 0, 4)
        };

        // 作者图标
        var icon = new TextBlock
        {
            Text = "💬",
            FontSize = (double)context.Properties["FontSize"],
            VerticalAlignment = VerticalAlignment.Center
        };

        // 作者文本
        var authorText = new TextBlock
        {
            Text = $"{author} 说：",
            FontWeight = FontWeights.SemiBold,
            FontSize = (double)context.Properties["FontSize"],
            Foreground = new SolidColorBrush(Color.FromArgb(255, 0, 120, 215)),
            VerticalAlignment = VerticalAlignment.Center
        };

        authorPanel.Children.Add(icon);
        authorPanel.Children.Add(authorText);

        return authorPanel;
    }
}

// 段落渲染策略
public class ParagraphRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();

    }
}

// 换行渲染策略
public class LineBreakRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        var breakContext = AnalyzeContext(node);
        
        // 决策树：根据上下文决定如何处理
        if (ShouldIgnoreLineBreak(breakContext))
        {
            // 完全忽略，不渲染任何内容
            return;
        }
        else if (ShouldCreateParagraphBreak(breakContext))
        {
            // 创建段落分隔（结束当前文本块）
            context.FinalizeCurrentTextBlock();
        }
        else if (ShouldCreateSoftLineBreak(breakContext))
        {
            // 创建软换行（在当前文本块内）
            CreateSoftLineBreak(context);
        }
        else
        {
            // 默认：创建标准换行
            CreateStandardLineBreak(context);
        }
    }
    private class LineBreakContext
    {
        public bool IsInsideQuote { get; set; }
        public bool IsInsideParagraph { get; set; }
        public bool IsInsideBlockContainer { get; set; }
        public bool IsDocumentRoot { get; set; }
        public bool IsFirstInParent { get; set; }
        public bool IsLastInParent { get; set; }
        public bool AfterBlockClose { get; set; } // 是否在块级标签关闭后
        public int ConsecutiveLineBreaks { get; set; } // 连续换行符数量
    }
    /// <summary>
    /// 换行处理逻辑的统一入口点
    /// </summary>
    /// <param name="node"></param>
    /// <returns></returns>
    private LineBreakContext AnalyzeContext(UbbNode node)
    {
        var context = new LineBreakContext();

        // 分析节点位置
        var current = node;
        while (current.Parent != null)
        {
            if (current.Parent.Type == UbbNodeType.Quote)
                context.IsInsideQuote = true;
            if (current.Parent.Type == UbbNodeType.Paragraph)
                context.IsInsideParagraph = true;
            if (IsBlockContainer(current.Parent.Type))
                context.IsInsideBlockContainer = true;
            if (current.Parent.Type == UbbNodeType.Document)
                context.IsDocumentRoot = true;

            current = current.Parent;
        }

        // 分析兄弟节点关系
        if (node.Parent != null)
        {
            var siblings = node.Parent.Children.ToList();
            var index = siblings.IndexOf(node);

            context.IsFirstInParent = index == 0;
            context.IsLastInParent = index == siblings.Count - 1;

            // 检查是否在块级标签关闭后
            if (index > 0)
            {
                var prevSibling = siblings[index - 1];
                context.AfterBlockClose = IsBlockClosingTag(prevSibling);
            }
        }

        // 计算连续换行符数量
        context.ConsecutiveLineBreaks = CountConsecutiveLineBreaks(node);

        return context;
    }
    private bool ShouldIgnoreLineBreak(LineBreakContext context)
    {
        // 规则1：在文档根节点下，且是第一个或最后一个换行
        if (context.IsDocumentRoot && (context.IsFirstInParent || context.IsLastInParent))
            return true;
            
        // 规则2：紧跟在块级标签关闭后的第一个换行
        if (context.AfterBlockClose && context.IsFirstInParent)
            return true;
            
        // 规则3：在引用块内，但前面已经有换行了
        if (context.IsInsideQuote && context.ConsecutiveLineBreaks > 1)
            return true; // 忽略多余的换行
            
        return false;
    }
    
    private bool ShouldCreateParagraphBreak(LineBreakContext context)
    {
        // 规则1：在文档根节点下，有两个以上连续换行
        if (context.IsDocumentRoot && context.ConsecutiveLineBreaks >= 2)
            return true;
            
        // 规则2：不在任何容器内，且是显著的分隔
        if (!context.IsInsideQuote && !context.IsInsideParagraph && 
            !context.IsInsideBlockContainer)
            return true;
            
        return false;
    }
    
    private bool ShouldCreateSoftLineBreak(LineBreakContext context)
    {
        // 规则1：在段落内
        if (context.IsInsideParagraph)
            return true;
            
        // 规则2：在引用块或代码块内
        if (context.IsInsideQuote || context.IsInsideBlockContainer)
            return true;
            
        return false;
    }
    
    private void CreateSoftLineBreak(RenderContext context)
    {
        // 在当前文本块内添加换行
        context.AddInline(new LineBreak());
    }
    
    private void CreateStandardLineBreak(RenderContext context)
    {
        // 结束当前文本块，开始新的文本块
        context.FinalizeCurrentTextBlock();
    }
    
    private int CountConsecutiveLineBreaks(UbbNode node)
    {
        if (node.Parent == null)
            return 1;
            
        var siblings = node.Parent.Children.ToList();
        var index = siblings.IndexOf(node);
        int count = 1;
        
        // 向前查找连续的换行节点
        for (int i = index - 1; i >= 0; i--)
        {
            if (siblings[i].Type == UbbNodeType.LineBreak)
                count++;
            else
                break;
        }
        
        // 向后查找连续的换行节点
        for (int i = index + 1; i < siblings.Count; i++)
        {
            if (siblings[i].Type == UbbNodeType.LineBreak)
                count++;
            else
                break;
        }
        
        return count;
    }
    
    private bool IsBlockContainer(UbbNodeType type)
    {
        return type == UbbNodeType.Quote || 
               type == UbbNodeType.Code || 
               type == UbbNodeType.List;
    }
    
    private bool IsBlockClosingTag(UbbNode node)
    {
        // 检查节点是否表示块级元素的结束
        // 这需要根据你的AST结构来判断
        return node.Type == UbbNodeType.Quote || 
               node.Type == UbbNodeType.Code;
    }
}


// 对齐渲染策略
public class AlignRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        if (node is TagNode tagNode)
        {
            var align = tagNode.GetAttribute("align", "left").ToLower();
            RenderHelper.ApplyAlignToContext(align,node,context);
        }
    }
}

// 左对齐渲染策略
public class LeftRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        string align = "left";
        RenderHelper.ApplyAlignToContext(align, node, context);
    }
}

// 居中渲染策略
public class CenterRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        string align = "center";
        RenderHelper.ApplyAlignToContext(align, node, context);
    }
}

// 右对齐渲染策略
public class RightRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        string align = "right";
        RenderHelper.ApplyAlignToContext(align, node, context);
    }
}

public class ColorRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var colorStr = tagNode.GetAttribute("color");
            if (!string.IsNullOrEmpty(colorStr))
            {
                var span = new Span();

                // 尝试解析颜色
                try
                {
                    var color = ParseColor(colorStr);
                    span.Foreground = new SolidColorBrush(color);
                }
                catch
                {
                    // 解析失败，使用默认颜色
                }

                context.BeginInlineContainer(span);

                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }

                context.EndInlineContainer();
            }
            else
            {
                // 没有颜色值，直接渲染子节点
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
            }
        }
    }

    private Color ParseColor(string colorStr)
    {
        // 移除可能的#
        colorStr = colorStr.Trim().TrimStart('#');

        // 支持颜色名称
        var colorName = colorStr.ToLower();
        switch (colorName)
        {
            case "black": return Colors.Black;
            case "white": return Colors.White;
            case "red": return Colors.Red;
            case "green": return Colors.Green;
            case "blue": return Color.FromArgb(255,142,130,254);
            case "gray":
            case "grey": return Colors.Gray;
            case "yellow": return Colors.Yellow;
            case "purple": return Colors.Purple;
            case "orange": return Colors.Orange;
            default:
                // 尝试解析十六进制颜色
                if (colorStr.Length == 6)
                {
                    var r = Convert.ToByte(colorStr.Substring(0, 2), 16);
                    var g = Convert.ToByte(colorStr.Substring(2, 2), 16);
                    var b = Convert.ToByte(colorStr.Substring(4, 2), 16);
                    return Color.FromArgb(255, r, g, b);
                }
                return Colors.Black; // 默认黑色
        }
    }
}

// 字体渲染策略
public class FontRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        if (node is TagNode tagNode)
        {
            var fontName = tagNode.GetAttribute("font");
            if (!string.IsNullOrEmpty(fontName))
            {
                var span = new Span();

                try
                {
                    span.FontFamily = new FontFamily(fontName);
                }
                catch
                {
                    // 字体无效，使用默认字体
                }

                context.BeginInlineContainer(span);

                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }

                context.EndInlineContainer();
            }
            else
            {
                // 没有字体值，直接渲染子节点
                foreach (var child in node.Children)
                {
                    context.RenderNode(child);
                }
            }
        }
    }
}
public class EmojiRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        // 不要FinalizeCurrentTextBlock(),保持在当前文本流
        if (node is TagNode tagNode)
        {
            var emoticonCode = tagNode.GetAttribute("code");
            if (!string.IsNullOrEmpty(emoticonCode))
            {
                var imageUrl = GetEmoticonUrl(emoticonCode);
                try
                {
                    if (!string.IsNullOrEmpty(imageUrl))
                    {
                        var image = new Image
                        {
                            Source = LoadImageFromUrl(imageUrl),
                            Margin = new Thickness(4, 0, 4, 0),
                            MaxWidth =32,
                            MaxHeight=32,
                            Stretch=Stretch.UniformToFill
                        };
                        var inlineContainer = new InlineUIContainer { Child = image };
                        context.AddInline(inlineContainer);
                    }
                    else
                    {
                        var run = new Run { Text = $"[{emoticonCode}]" };
                        context.AddInline(run);
                    }
                }
                catch(Exception ex)
                {
                    var run = new Run { Text = $"[{ex.Message}]" };
                    context.AddInline(run);
                }
            }
        }
    }
    private string GetEmoticonUrl(string code)
    {
        return EmoticonRules.GetEmoticonUrl(code);
    }
    private ImageSource LoadImageFromUrl(string url)
    {
        try
        {
            var bitmapImage = new BitmapImage(new Uri(url));
            return bitmapImage;
        }
        catch
        {
            return null;
        }
    }

}

public class LatexRenderStrategy : IRenderStrategy
{
    public void Render(UbbNode node, RenderContext context)
    {
        context.FinalizeCurrentTextBlock();
        var border = new Border
        {
            Background = (Brush)context.Properties["CodeBackground"],
            Padding = new Thickness(12),
            Margin = new Thickness(4, 2, 2, 0),
            CornerRadius = new CornerRadius(4)
        };

        var grid = new Grid();

        // 定义行：第一行用于显示语言标签，第二行用于代码
        grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

        // 创建语言标签容器
        var languageContainer = new Grid
        {
            Margin = new Thickness(0, 0, 0, 0),
        };


        // 创建语言标签
        var languageTag = new Border
        {
            Background = new SolidColorBrush(Color.FromArgb(0x33, 0x00, 0x7A, 0xCC)), // 半透明蓝色
            Padding = new Thickness(8, 4, 8, 4),
            CornerRadius = new CornerRadius(4),
            HorizontalAlignment = HorizontalAlignment.Left
        };

        var languageText = new TextBlock
        {
            Text = "Latex",
            FontSize = 12,
            FontWeight = FontWeights.SemiBold,
            Foreground = new SolidColorBrush(Color.FromArgb(0xFF, 0x00, 0x7A, 0xCC)), // 蓝色文字
            VerticalAlignment = VerticalAlignment.Center
        };

        languageTag.Child = languageText;
        languageContainer.Children.Add(languageTag);

        // 添加到 Grid 的第一行
        Grid.SetRow(languageContainer, 0);
        grid.Children.Add(languageContainer);

        // 2. 创建代码区域
        var codeContainer = new Grid();
        Grid.SetRow(codeContainer, 1);

        var scrollViewer = new ScrollViewer
        {
            HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };

        var textBlock = new LatexBlock
        {
            FontSize = 24,
            Width=400
        };

        var codeText = CollectText(node);
        textBlock.LaTeX= codeText;
        AddCopyButton(languageContainer, codeText);
        scrollViewer.Content = textBlock;
        codeContainer.Children.Add(scrollViewer);

        grid.Children.Add(codeContainer);

        border.Child = grid;
        context.AddToContainer(border);
    }

    private string CollectText(UbbNode node)
    {
        var text = "";
        foreach (var child in node.Children)
        {
            if (child is TextNode textNode)
            {
                text += textNode.Content;
            }
            else
            {
                text += CollectText(child);
            }
        }
        return text;
    }
    private void AddCopyButton(Grid languageContainer, string codeText)
    {
        var copyButton = new Button
        {
            Content = new FluentIcons.WinUI.SymbolIcon { Symbol = FluentIcons.Common.Symbol.Copy, FontSize = 16 },
            Margin = new Thickness(0, 0, 8, 0),
            Padding = new Thickness(0, 0, 0, 0),
            BorderThickness = new Thickness(1),
            Width = 32,
            Height = 32,
            HorizontalAlignment = HorizontalAlignment.Right
        };

        copyButton.Click += (sender, e) =>
        {
            var dataPackage = new DataPackage();
            dataPackage.SetText(codeText);
            Clipboard.SetContent(dataPackage);
        };

        languageContainer.Children.Add(copyButton);
    }

}

public class RenderHelper
{
    public static void ApplyAlignToContext(string align,UbbNode node, RenderContext context)
    {
        //使用Grid进行对齐控制
        var panel = new Grid();

        switch (align)
        {
            case "center":
                panel.HorizontalAlignment = HorizontalAlignment.Center;
                break;
            case "right":
                panel.HorizontalAlignment = HorizontalAlignment.Right;
                break;
            default:
                panel.HorizontalAlignment = HorizontalAlignment.Left;
                break;
        }

        // 保存当前容器
        var previousContainer = context.Container;
        var previousPanelStack = context.PanelStack != null ? new Stack<Panel>(context.PanelStack) : null;

        context.Container = panel;
        if (context.PanelStack == null)
        {
            context.PanelStack = new Stack<Panel>();
        }
        context.PanelStack.Push(panel);

        // 渲染子节点
        foreach (var child in node.Children)
        {
            context.RenderNode(child);
        }

        // 确保当前文本块结束
        context.FinalizeCurrentTextBlock();

        // 恢复之前的容器
        context.Container = previousContainer;
        if (previousPanelStack != null)
        {
            context.PanelStack.Clear();
            foreach (var p in previousPanelStack.Reverse())
                context.PanelStack.Push(p);
        }
        else
        {
            // 如果之前没有 PanelStack，则清空当前栈
            context.PanelStack.Clear();
        }

        // 将 panel 添加回之前的容器
        context.AddToContainer(panel);
    }
}