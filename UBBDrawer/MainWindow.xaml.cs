using ColorCode;
using CommunityToolkit.WinUI;
using CommunityToolkit.WinUI.Controls;
using CommunityToolkit.WinUI.Controls.MarkdownTextBlockRns;
using Html2Markdown;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.VisualBasic;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UbbRender.Common;
using Windows.ApplicationModel.DataTransfer;
using Windows.Foundation;
using Windows.Foundation.Collections;
using UBBParser.Parser;
using static System.Runtime.InteropServices.JavaScript.JSType;
// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace UBBDrawer
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        public MarkdownConfig LiveMarkdownConfig { get; set; }
        public ObservableCollection<UbbTreeItem> DataSource { get; set; } = new();

        public MainWindow()
        {
            this.InitializeComponent();
            LiveMarkdownConfig = new MarkdownConfig() {
                ImageProvider = new AdvancedImageProvider(),  
            }; 
        }
        
        private void in_TextChanged(object sender, TextChangedEventArgs e)
        {
            // 把 UBB 文本传给渲染控件，触发渲染
            render.UbbText = i.Text;
            var doc=UbbParser.Parse(i.Text);
            de.Text = AstPrinter.PrintUbbAst(i.Text);
            DataSource = UbbTreeConverter.ConvertToTreeItems(doc);
            this.view.ItemsSource = DataSource;
        }
        

        


        private void draw_OnLinkClicked(object sender, LinkClickedEventArgs e)
        {
            de.Text = e.Uri.ToString();
            
            return;
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            string text=AstPrinter.PrintUbbAst(i.Text);
            var package = new DataPackage();
            package.SetText(text);
            Clipboard.SetContent(package);
        }
    }


    public class AdvancedImageProvider : IImageProvider
    {
        public AdvancedImageProvider() { }
        public async Task<Image> GetImage(string url)
        {
            var image = new Image();
            image.Stretch = Stretch.Uniform;
            image.MaxWidth = 400;  // 限制最大宽度
            image.HorizontalAlignment = HorizontalAlignment.Left;
            image.Margin = new Thickness(0, 5, 0, 5);
            var bitmapImage = new BitmapImage(new Uri(url));
            image.Source = bitmapImage;
            return image;
        }

        public bool ShouldUseThisProvider(string url)
        {
            return true;
        }
    }
    public partial class NodeTypeToSymbolConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is UbbNodeType nodeType)
            {
                return nodeType switch
                {
                    UbbNodeType.Document => FluentIcons.Common.Symbol.Document,      // 文档图标
                    UbbNodeType.Text => FluentIcons.Common.Symbol.TextAdd,          // 文本图标
                    UbbNodeType.Bold => FluentIcons.Common.Symbol.TextBold,          // 粗体
                    UbbNodeType.Italic => FluentIcons.Common.Symbol.TextItalic,        // 斜体
                    UbbNodeType.Strikethrough => FluentIcons.Common.Symbol.TextStrikethrough, // 删除线
                    UbbNodeType.Underline => FluentIcons.Common.Symbol.TextUnderline,   // 下划线
                    UbbNodeType.Url => FluentIcons.Common.Symbol.Link,           // 链接
                    UbbNodeType.Code => FluentIcons.Common.Symbol.Code,          // 代码
                    UbbNodeType.Quote => FluentIcons.Common.Symbol.CommentQuote,         // 引用
                    UbbNodeType.List => FluentIcons.Common.Symbol.List,          // 列表
                    UbbNodeType.ListItem => FluentIcons.Common.Symbol.TextBulletList,    // 列表项
                    UbbNodeType.Paragraph => FluentIcons.Common.Symbol.TextParagraph,     // 段落
                    UbbNodeType.Image => FluentIcons.Common.Symbol.Image,         // 图片
                    UbbNodeType.Video => FluentIcons.Common.Symbol.Video,         // 视频
                    UbbNodeType.Audio => FluentIcons.Common.Symbol.MusicNote2,        // 音频
                    UbbNodeType.Right=> FluentIcons.Common.Symbol.TextAlignRight,     // 右对齐
                    UbbNodeType.Center=> FluentIcons.Common.Symbol.TextAlignCenter,    // 居中对齐
                    UbbNodeType.Left=> FluentIcons.Common.Symbol.TextAlignLeft,      // 左对齐
                    UbbNodeType.Align=> FluentIcons.Common.Symbol.TextAlignJustify,    // 对齐
                    UbbNodeType.Size=> FluentIcons.Common.Symbol.TextFontSize,     // 字体大小
                    UbbNodeType.Font=> FluentIcons.Common.Symbol.TextFont,           // 字体
                    UbbNodeType.Color=> FluentIcons.Common.Symbol.Color,          // 颜色
                    UbbNodeType.LineBreak => FluentIcons.Common.Symbol.ArrowDownRight,    // 换行
                    _ => FluentIcons.Common.Symbol.Tag                          // 默认图标
                };
            }

            return FluentIcons.Common.Symbol.Tag; // 默认图标
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

        public class AstPrinter
        {
        /// <summary>
        /// 将UBB文本解析为AST，并以字符串形式输出树结构
        /// </summary>
        /// <param name="ubbText">UBB文本</param>
        /// <returns>AST的字符串表示</returns>
        public static string PrintUbbAst(string ubbText)
        {
            try
            {
                // 解析UBB为文档树
                var document = UbbParser.Parse(ubbText);

                // 构建树形字符串
                var sb = new StringBuilder();
                sb.AppendLine("=== UBB AST 树结构 ===");
                sb.AppendLine($"输入文本为: \"{ubbText}\"");
                sb.AppendLine();
                sb.AppendLine("树结构:");

                // 递归打印节点
                PrintNode(document.Root, sb, 0, new List<bool>());

                

                return sb.ToString();
            }
            catch (Exception ex)
            {
                return $"解析失败: {ex.Message}\n{ex.StackTrace}";
            }
        }

        /// <summary>
        /// 递归打印节点
        /// </summary>
        private static void PrintNode(UbbNode node, StringBuilder sb, int depth, List<bool> isLastList)
        {
            // 构建缩进
            var indent = new StringBuilder();
            for (int i = 0; i < depth; i++)
            {
                if (i < isLastList.Count - 1)
                {
                    indent.Append(isLastList[i] ? "    " : "│   ");
                }
                else if (i == isLastList.Count - 1)
                {
                    indent.Append(isLastList[i] ? "└── " : "├── ");
                }
            }

            // 节点信息
            string nodeInfo;
            if (node is TextNode textNode)
            {
                // 转义特殊字符以便于阅读
                var escapedContent = textNode.Content
                    .Replace("\n", "\\n")
                    .Replace("\r", "\\r")
                    .Replace("\t", "\\t");
                nodeInfo = $"Text: \"{escapedContent}\"";
            }
            else if (node is TagNode tagNode)
            {
                nodeInfo = $"Tag: {tagNode.Type}";
                if (tagNode.Attributes.Count > 0)
                {
                    var attrs = new List<string>();
                    foreach (var attr in tagNode.Attributes)
                    {
                        attrs.Add($"{attr.Key}=\"{attr.Value}\"");
                    }
                    nodeInfo += $" [{string.Join(", ", attrs)}]";
                }

                // 添加父节点信息（如果有）
                if (tagNode.Parent != null)
                {
                    nodeInfo += $" (父: {tagNode.Parent.Type})";
                }
            }
            else
            {
                nodeInfo = $"Unknown: {node.GetType().Name}";
            }

            sb.AppendLine($"{indent}{nodeInfo}");

            // 递归处理子节点
            var childCount = node.Children.Count;
            for (int i = 0; i < childCount; i++)
            {
                var isLast = i == childCount - 1;
                var newIsLastList = new List<bool>(isLastList) { isLast };
                PrintNode(node.Children[i], sb, depth + 1, newIsLastList);
            }
        }

        /// <summary>
        /// 统计节点信息
        /// </summary>
        private static (int total, int tagNodes, int textNodes, int maxDepth) CountNodes(UbbNode node, int depth = 0)
        {
            int total = 1;
            int tagNodes = node is TagNode ? 1 : 0;
            int textNodes = node is TextNode ? 1 : 0;
            int maxDepth = depth;

            foreach (var child in node.Children)
            {
                var childStats = CountNodes(child, depth + 1);
                total += childStats.total;
                tagNodes += childStats.tagNodes;
                textNodes += childStats.textNodes;
                maxDepth = Math.Max(maxDepth, childStats.maxDepth);
            }

            return (total, tagNodes, textNodes, maxDepth);
        }

        /// <summary>
        /// 检查关键关系
        /// </summary>
        private static void CheckCriticalRelationships(UbbNode node, StringBuilder sb)
        {
            var issues = new List<string>();

            CheckNodeRelationships(node, issues, new HashSet<UbbNode>());

            if (issues.Count == 0)
            {
                sb.AppendLine("✓ 没有发现明显的问题");
            }
            else
            {
                sb.AppendLine($"发现 {issues.Count} 个潜在问题:");
                foreach (var issue in issues)
                {
                    sb.AppendLine($"  • {issue}");
                }
            }
        }

        private static void CheckNodeRelationships(UbbNode node, List<string> issues, HashSet<UbbNode> visited)
        {
            if (visited.Contains(node))
            {
                issues.Add($"检测到循环引用: {node.Type}");
                return;
            }

            visited.Add(node);

            // 检查文本节点是否在正确的父节点中
            if (node is TextNode textNode)
            {
                if (node.Parent is TagNode parentTag)
                {
                    // 检查文本节点是否在某些样式标签内
                    var styleTags = new HashSet<UbbNodeType>
                {
                    UbbNodeType.Strikethrough,
                    UbbNodeType.Bold,
                    UbbNodeType.Italic,
                    UbbNodeType.Underline,
                    UbbNodeType.Color,
                    UbbNodeType.Size,
                    UbbNodeType.Font
                };

                    if (styleTags.Contains(parentTag.Type))
                    {
                        // 这是正常的，文本在样式标签内
                    }
                    else if (parentTag.Type == UbbNodeType.Document)
                    {
                        // 文本直接位于文档根节点下，可能是正常的
                    }
                    else
                    {
                        // 其他情况
                    }
                }
            }

            // 检查子节点的父节点指针是否正确
            foreach (var child in node.Children)
            {
                if (child.Parent != node)
                {
                    issues.Add($"节点 {child.Type} 的父节点指向错误: 期望 {node.Type}，实际 {child.Parent?.Type ?? null}");
                }

                CheckNodeRelationships(child, issues, visited);
            }

            visited.Remove(node);
        }
    }
}