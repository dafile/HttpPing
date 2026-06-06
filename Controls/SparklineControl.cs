using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace HttpPing.Controls
{
    public class SparklineControl : Control
    {
        public static readonly DependencyProperty ItemsSourceProperty =
            DependencyProperty.Register(nameof(ItemsSource), typeof(IList<long>), typeof(SparklineControl),
                new PropertyMetadata(null, OnDataChanged));

        public static readonly DependencyProperty StrokeBrushProperty =
            DependencyProperty.Register(nameof(StrokeBrush), typeof(Brush), typeof(SparklineControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50))));

        public static readonly DependencyProperty FillBrushProperty =
            DependencyProperty.Register(nameof(FillBrush), typeof(Brush), typeof(SparklineControl),
                new PropertyMetadata(new SolidColorBrush(Color.FromArgb(0x40, 0x4C, 0xAF, 0x50))));

        public IList<long> ItemsSource
        {
            get => (IList<long>)GetValue(ItemsSourceProperty);
            set => SetValue(ItemsSourceProperty, value);
        }

        public Brush StrokeBrush
        {
            get => (Brush)GetValue(StrokeBrushProperty);
            set => SetValue(StrokeBrushProperty, value);
        }

        public Brush FillBrush
        {
            get => (Brush)GetValue(FillBrushProperty);
            set => SetValue(FillBrushProperty, value);
        }

        static SparklineControl()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(SparklineControl),
                new FrameworkPropertyMetadata(typeof(SparklineControl)));
        }

        private static void OnDataChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((SparklineControl)d).InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            base.OnRender(dc);

            var data = ItemsSource;
            if (data == null || data.Count < 2)
            {
                // Draw placeholder line
                var pen = new Pen(StrokeBrush, 1);
                pen.Freeze();
                dc.DrawLine(pen, new Point(0, ActualHeight / 2), new Point(ActualWidth, ActualHeight / 2));
                return;
            }

            var w = ActualWidth;
            var h = ActualHeight;
            if (w < 1 || h < 1) return;

            var padding = 2.0;
            var drawW = w - padding * 2;
            var drawH = h - padding * 2;

            var maxVal = data.Max();
            if (maxVal <= 0) maxVal = 1;

            var points = new Point[data.Count];
            for (int i = 0; i < data.Count; i++)
            {
                var x = padding + (data.Count == 1 ? drawW / 2 : drawW * i / (data.Count - 1));
                var y = padding + drawH - (drawH * data[i] / maxVal);
                points[i] = new Point(x, y);
            }

            // Draw fill polygon
            var fillPoints = new List<Point>(points);
            fillPoints.Add(new Point(points[^1].X, h - padding));
            fillPoints.Add(new Point(points[0].X, h - padding));
            var fillGeometry = new StreamGeometry();
            using (var ctx = fillGeometry.Open())
            {
                ctx.BeginFigure(fillPoints[0], true, true);
                ctx.PolyLineTo(fillPoints.Skip(1).ToList(), true, true);
            }
            fillGeometry.Freeze();
            dc.DrawGeometry(FillBrush, null, fillGeometry);

            // Draw line
            var linePen = new Pen(StrokeBrush, 1.5);
            linePen.Freeze();
            var lineGeometry = new StreamGeometry();
            using (var ctx = lineGeometry.Open())
            {
                ctx.BeginFigure(points[0], false, false);
                ctx.PolyLineTo(points.Skip(1).ToList(), true, true);
            }
            lineGeometry.Freeze();
            dc.DrawGeometry(null, linePen, lineGeometry);

            // Draw last point dot
            var lastPt = points[^1];
            dc.DrawEllipse(StrokeBrush, null, lastPt, 3, 3);
        }
    }
}
