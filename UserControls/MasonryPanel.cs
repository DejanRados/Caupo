using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Caupo.UserControls
{
    public class MasonryPanel : Panel
    {
        public double ItemWidth { get; set; } = 260;

        protected override Size MeasureOverride(Size availableSize)
        {
            double width = double.IsInfinity(availableSize.Width) ? ItemWidth : availableSize.Width;
            int columns = Math.Max(1, (int)(width / ItemWidth));
            double[] heights = new double[columns];

            foreach (UIElement child in InternalChildren)
            {
                child.Measure(new Size(ItemWidth, double.PositiveInfinity));
                int column = Array.IndexOf(heights, heights.Min());
                heights[column] += child.DesiredSize.Height;
            }

            return new Size(width, heights.Max());
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            int columns = Math.Max(1, (int)(finalSize.Width / ItemWidth));
            double[] heights = new double[columns];

            foreach (UIElement child in InternalChildren)
            {
                int column = Array.IndexOf(heights, heights.Min());
                child.Arrange(new Rect(column * ItemWidth, heights[column], ItemWidth, child.DesiredSize.Height));
                heights[column] += child.DesiredSize.Height;
            }

            return finalSize;
        }
    }
}