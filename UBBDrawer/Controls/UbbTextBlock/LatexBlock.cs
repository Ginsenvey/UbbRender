using CSharpMath.Rendering;
using CSharpMath.SkiaSharp;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SkiaSharp;
using SkiaSharp.Views.Windows;
using System;
using System.Drawing;

namespace LatexRender.Render
{
    public sealed partial class LatexBlock : SKXamlCanvas
    {
        private readonly MathPainter _painter = new MathPainter();
        private string _latex = "";
        private float _totalHeight = 0;
        private float _maxLineWidth = 0;

        // 定义控件的内边距属性
        private Thickness _padding = new Thickness(5);
        public Thickness Padding
        {
            get => _padding;
            set
            {
                _padding = value;
                InvalidateMeasure(); // 重要：标记需要重新测量
                Invalidate();
            }
        }

        // 行间距属性
        private float _lineSpacing = 0.2f;
        public double LineSpacing
        {
            get => _lineSpacing;
            set
            {
                _lineSpacing = (float)value;
                InvalidateMeasure(); // 重要：标记需要重新测量
                Invalidate();
            }
        }

        // LaTeX公式属性
        public string LaTeX
        {
            get => _latex;
            set
            {
                _latex = value;
                InvalidateMeasure(); // 重要：标记需要重新测量
                Invalidate();
            }
        }

        // 字体大小属性
        public double FontSize
        {
            get => _painter.FontSize;
            set
            {
                _painter.FontSize = (float)value;
                InvalidateMeasure(); // 重要：标记需要重新测量
                Invalidate();
            }
        }

        // 水平对齐属性
        public HorizontalAlignment HorizontalContentAlignment { get; set; } = HorizontalAlignment.Center;

        // 垂直对齐属性
        public VerticalAlignment VerticalContentAlignment { get; set; } = VerticalAlignment.Center;

        // 重要：重写MeasureOverride方法，报告内容所需尺寸
        // 添加字段来缓存测量结果
        private float _measuredTotalHeight = 0;
        private float _measuredMaxWidth = 0;
        private string _lastMeasuredLaTeX = "";
        private float _lastMeasuredFontSize = 0;
        private Thickness _lastMeasuredPadding = new Thickness(0);
        private float _lastMeasuredLineSpacing = 0;

        // 修改 MeasureOverride 方法
        protected override Windows.Foundation.Size MeasureOverride(Windows.Foundation.Size availableSize)
        {
            // 如果 LaTeX 为空，返回最小尺寸
            if (string.IsNullOrEmpty(_latex))
            {
                _measuredTotalHeight = 0;
                _measuredMaxWidth = 0;
                return new Windows.Foundation.Size(0, 0);
            }

            // 检查是否需要重新测量（缓存优化）
            bool needsReMeasure =
                _latex != _lastMeasuredLaTeX ||
                _painter.FontSize != _lastMeasuredFontSize ||
                !_padding.Equals(_lastMeasuredPadding) ||
                _lineSpacing != _lastMeasuredLineSpacing;

            if (!needsReMeasure && _measuredTotalHeight > 0)
            {
                return new Windows.Foundation.Size(
                    _measuredMaxWidth + (float)(Padding.Left + Padding.Right),
                    _measuredTotalHeight + (float)(Padding.Top + Padding.Bottom)
                );
            }

            // 重置测量结果
            _measuredTotalHeight = 0;
            _measuredMaxWidth = 0;

            try
            {
                // 关键修正：处理可用宽度
                float availableWidth = (float)availableSize.Width;
                bool isWidthConstrained = !double.IsInfinity(availableWidth) && !double.IsNaN(availableWidth) && availableWidth > 0;

                // 关键修正：处理可用高度 - 检查是否是无限高度
                bool isHeightInfinite = double.IsInfinity(availableSize.Height);

                if (isWidthConstrained)
                {
                    availableWidth = Math.Max(10f, availableWidth - (float)(Padding.Left + Padding.Right));
                }
                else
                {
                    availableWidth = 1000f; // 合理的默认宽度
                }

                // 分割并测量每一行
                string[] lines = _latex.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);

                System.Diagnostics.Debug.WriteLine($"测量 {lines.Length} 行，可用宽={availableWidth:F1}，高度无限={isHeightInfinite}");

                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    string line = lines[i].Trim();

                    try
                    {
                        _painter.LaTeX = line;
                        var rect = _painter.Measure(availableWidth);

                        if (rect.Width <= 0 || rect.Height <= 0)
                        {
                            // 使用默认尺寸
                            rect = new RectangleF(0, 0, 100, 30);
                        }

                        float lineHeight = rect.Height;

                        _measuredMaxWidth = Math.Max(_measuredMaxWidth, rect.Width);
                        _measuredTotalHeight += lineHeight;

                        // 添加行间距（除了最后一行）
                        if (i < lines.Length - 1)
                        {
                            _measuredTotalHeight += lineHeight * _lineSpacing;
                        }
                    }
                    catch (Exception)
                    {
                        // 使用默认行尺寸
                        _measuredMaxWidth = Math.Max(_measuredMaxWidth, 100f);
                        _measuredTotalHeight += 30f;

                        if (i < lines.Length - 1)
                        {
                            _measuredTotalHeight += 30f * _lineSpacing;
                        }
                    }
                }

                // 确保有最小尺寸
                if (_measuredTotalHeight <= 0)
                {
                    _measuredTotalHeight = Math.Max(30f, 30f * lines.Length);
                }
                if (_measuredMaxWidth <= 0)
                {
                    _measuredMaxWidth = 100f;
                }

                // 关键修正：计算返回尺寸
                float totalWidth = _measuredMaxWidth + (float)(Padding.Left + Padding.Right);
                float totalHeight = _measuredTotalHeight + (float)(Padding.Top + Padding.Bottom);

                System.Diagnostics.Debug.WriteLine($"测量结果: 内容={_measuredMaxWidth:F1}x{_measuredTotalHeight:F1}, 总计={totalWidth:F1}x{totalHeight:F1}");

                // 缓存测量结果
                _lastMeasuredLaTeX = _latex;
                _lastMeasuredFontSize = _painter.FontSize;
                _lastMeasuredPadding = _padding;
                _lastMeasuredLineSpacing = _lineSpacing;

                // 返回所需尺寸
                return new Windows.Foundation.Size(totalWidth, totalHeight);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"MeasureOverride 失败: {ex.Message}");

                // 失败时返回一个合理的默认尺寸
                string[] lines = _latex.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                _measuredTotalHeight = Math.Max(30f, 30f * lines.Length);
                _measuredMaxWidth = 200f;

                return new Windows.Foundation.Size(
                    _measuredMaxWidth + (float)(Padding.Left + Padding.Right),
                    _measuredTotalHeight + (float)(Padding.Top + Padding.Bottom)
                );
            }
        }

        // 添加这个方法：强制重新测量
        public void InvalidateMeasureAndArrange()
        {
            InvalidateMeasure();
            InvalidateArrange();
            Invalidate();
        }

        // 修改 OnPaintSurface 方法中的绘制逻辑
        protected override void OnPaintSurface(SKPaintSurfaceEventArgs e)
        {
            base.OnPaintSurface(e);
            var canvas = e.Surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            if (string.IsNullOrEmpty(_latex) || _measuredTotalHeight <= 0)
                return;

            try
            {
                // 获取控件实际尺寸
                float actualWidth = (float)this.ActualWidth;
                float actualHeight = (float)this.ActualHeight;

                System.Diagnostics.Debug.WriteLine($"绘制开始: 控件尺寸={actualWidth:F1}x{actualHeight:F1}");

                // 绘制调试边界 - 整个控件
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    var borderPaint = new SKPaint
                    {
                        Color = SKColors.Red.WithAlpha(0x30),
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1
                    };
                    canvas.DrawRect(0, 0, actualWidth, actualHeight, borderPaint);

                    // 绘制Padding区域
                    var paddingPaint = new SKPaint
                    {
                        Color = SKColors.Blue.WithAlpha(0x20),
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 1,
                        PathEffect = SKPathEffect.CreateDash(new float[] { 5, 5 }, 0)
                    };
                    canvas.DrawRect(
                        (float)Padding.Left,
                        (float)Padding.Top,
                        actualWidth - (float)(Padding.Left + Padding.Right),
                        actualHeight - (float)(Padding.Top + Padding.Bottom),
                        paddingPaint
                    );
                }

                // 计算内容可用宽度
                float contentWidth = actualWidth - (float)(Padding.Left + Padding.Right);
                if (contentWidth <= 0)
                {
                    System.Diagnostics.Debug.WriteLine($"内容宽度无效: {contentWidth:F1}");
                    return;
                }

                // 分割多行公式
                string[] lines = _latex.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 0) return;

                // 总是从上到下绘制（适合滚动）
                float currentY = (float)Padding.Top;

                System.Diagnostics.Debug.WriteLine($"开始绘制 {lines.Length} 行，起始Y={currentY:F1}");

                // 绘制每一行
                for (int i = 0; i < lines.Length; i++)
                {
                    if (string.IsNullOrWhiteSpace(lines[i]))
                        continue;

                    string line = lines[i].Trim();
                    _painter.LaTeX = line;
                    var rect = _painter.Measure(contentWidth);

                    // 计算水平对齐
                    float x = (float)Padding.Left;

                    switch (HorizontalContentAlignment)
                    {
                        case HorizontalAlignment.Right:
                            x = actualWidth - rect.Width - (float)Padding.Right;
                            break;
                        case HorizontalAlignment.Center:
                        case HorizontalAlignment.Stretch:
                            x = (float)Padding.Left + (contentWidth - rect.Width) / 2;
                            break;
                    }

                    // 绘制调试：行边界
                    if (System.Diagnostics.Debugger.IsAttached)
                    {
                        var linePaint = new SKPaint
                        {
                            Color = SKColors.Green.WithAlpha(0x20),
                            Style = SKPaintStyle.Stroke,
                            StrokeWidth = 1
                        };
                        canvas.DrawRect(x, currentY + rect.Y, rect.Width, rect.Height, linePaint);
                    }

                    // 绘制当前行
                    float drawY = currentY + rect.Height; // Y坐标需要加上行高
                    _painter.Draw(canvas, new SKPoint(x, drawY));

                    System.Diagnostics.Debug.WriteLine($"第{i + 1}行: 位置({x:F1},{drawY:F1}), 尺寸({rect.Width:F1}x{rect.Height:F1})");

                    // 移动到下一行位置
                    currentY += rect.Height;

                    // 添加行间距（除了最后一行）
                    if (i < lines.Length - 1)
                    {
                        currentY += rect.Height * _lineSpacing;
                    }
                }

                System.Diagnostics.Debug.WriteLine($"绘制结束，最终Y={currentY:F1}");

                // 绘制总内容高度指示线
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    var totalHeightPaint = new SKPaint
                    {
                        Color = SKColors.Orange,
                        Style = SKPaintStyle.Stroke,
                        StrokeWidth = 2
                    };
                    float totalContentHeight = _measuredTotalHeight + (float)(Padding.Top + Padding.Bottom);
                    canvas.DrawLine(0, totalContentHeight, actualWidth, totalContentHeight, totalHeightPaint);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"绘制公式失败: {ex.Message}");
                var paint = new SKPaint { Color = SKColors.Red, TextSize = 14 };
                canvas.DrawText($"渲染错误", 10, 20, paint);
            }
        }
    }     
}