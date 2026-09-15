using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using tantargyvalaszto.Models;

namespace tantargyvalaszto.Controls;

public class WheelControl : Control
{
    public static readonly StyledProperty<IEnumerable<SubjectItem>?> ItemsProperty =
        AvaloniaProperty.Register<WheelControl, IEnumerable<SubjectItem>?>(nameof(Items));

    public static readonly StyledProperty<double> AngleProperty =
        AvaloniaProperty.Register<WheelControl, double>(nameof(Angle));

    public IEnumerable<SubjectItem>? Items
    {
        get => GetValue(ItemsProperty);
        set => SetValue(ItemsProperty, value);
    }

    public double Angle
    {
        get => GetValue(AngleProperty);
        set => SetValue(AngleProperty, value);
    }

    static WheelControl()
    {
        AffectsRender<WheelControl>(ItemsProperty, AngleProperty);
        ItemsProperty.Changed.AddClassHandler<WheelControl>((control, e) => control.OnItemsPropertyChanged(e));
    }

    private void OnItemsPropertyChanged(AvaloniaPropertyChangedEventArgs e)
    {
        if (e.OldValue is INotifyCollectionChanged oldCollection)
        {
            oldCollection.CollectionChanged -= OnCollectionChanged;
        }
        if (e.NewValue is INotifyCollectionChanged newCollection)
        {
            newCollection.CollectionChanged += OnCollectionChanged;
        }
        InvalidateVisual();
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        InvalidateVisual();
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        var bounds = Bounds;
        double size = Math.Min(bounds.Width, bounds.Height);
        if (size <= 20) return;

        Point center = new Point(bounds.Width / 2, bounds.Height / 2);
        double radius = (size / 2) - 15;

        var itemList = Items != null ? new List<SubjectItem>(Items) : new List<SubjectItem>();
        int count = itemList.Count;

        // --- 0 ELEM ESETÉN: Alapértelmezett üres kerék váza ---
        if (count == 0)
        {
            context.DrawEllipse(null, new Pen(Brushes.DarkGray, 2), center, radius, radius);
            context.DrawEllipse(null, new Pen(Brushes.DarkGray, 1), center, radius * 0.2, radius * 0.2);

            // 8 küllő rajzolása az üres kerék vizuális megjelenítéséhez
            for (int i = 0; i < 8; i++)
            {
                double ang = i * (Math.PI / 4.0);
                Point pOuter = new Point(center.X + radius * Math.Cos(ang), center.Y + radius * Math.Sin(ang));
                Point pInner = new Point(center.X + (radius * 0.2) * Math.Cos(ang), center.Y + (radius * 0.2) * Math.Sin(ang));
                context.DrawLine(new Pen(Brushes.DimGray, 1), pInner, pOuter);
            }

            DrawPointer(context, center, radius);
            return;
        }

        // Transzformációs mátrix az elforgatáshoz
        double radians = Math.PI * Angle / 180.0;
        var transformMatrix = Matrix.CreateTranslation(-center.X, -center.Y)
                            * Matrix.CreateRotation(radians)
                            * Matrix.CreateTranslation(center.X, center.Y);

        // --- 1 ELEM ESETÉN: Teljes kör kirajzolása (megelőzi a vonallá torzulást) ---
        if (count == 1)
        {
            using (context.PushTransform(transformMatrix))
            {
                context.DrawEllipse(itemList[0].Brush, new Pen(Brushes.White, 2), center, radius, radius);

                double fontSize = Math.Clamp(radius / 5, 12, 20);
                var formattedText = new FormattedText(
                    itemList[0].Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    fontSize,
                    Brushes.White
                );

                Point textPos = new Point(center.X - formattedText.Width / 2, center.Y - formattedText.Height / 2);
                context.DrawText(formattedText, textPos);
            }

            DrawPointer(context, center, radius);
            return;
        }

        // --- 2 VAGY TÖBB ELEM ESETÉN: Cikkelyes elrendezés ---
        double sweepAngle = 360.0 / count;

        using (context.PushTransform(transformMatrix))
        {
            for (int i = 0; i < count; i++)
            {
                double startDegrees = i * sweepAngle;

                // Cikkely
                var geometry = CreatePieSliceGeometry(center, radius, startDegrees, sweepAngle);
                context.DrawGeometry(itemList[i].Brush, new Pen(Brushes.White, 1.5), geometry);

                // Szöveg elhelyezése
                double midAngle = startDegrees + (sweepAngle / 2.0);
                double midRad = midAngle * Math.PI / 180.0;

                double textDistance = radius * 0.60;
                Point textCenter = new Point(
                    center.X + textDistance * Math.Cos(midRad),
                    center.Y + textDistance * Math.Sin(midRad)
                );

                double fontSize = Math.Clamp(radius / (count * 1.2), 9, 15);

                var formattedText = new FormattedText(
                    itemList[i].Name,
                    System.Globalization.CultureInfo.CurrentCulture,
                    FlowDirection.LeftToRight,
                    Typeface.Default,
                    fontSize,
                    Brushes.White
                );

                var textMatrix = Matrix.CreateTranslation(-formattedText.Width / 2, -formattedText.Height / 2)
                                * Matrix.CreateRotation(midRad)
                                * Matrix.CreateTranslation(textCenter.X, textCenter.Y);

                using (context.PushTransform(textMatrix))
                {
                    context.DrawText(formattedText, new Point(0, 0));
                }
            }
        }

        DrawPointer(context, center, radius);
    }

    private void DrawPointer(DrawingContext context, Point center, double radius)
    {
        Point topPointer = new Point(center.X, center.Y - radius - 5);
        PathGeometry arrow = new PathGeometry();
        using (var ctx = arrow.Open())
        {
            ctx.BeginFigure(topPointer, true);
            ctx.LineTo(new Point(topPointer.X - 10, topPointer.Y - 15));
            ctx.LineTo(new Point(topPointer.X + 10, topPointer.Y - 15));
            ctx.EndFigure(true);
        }
        context.DrawGeometry(Brushes.Red, new Pen(Brushes.DarkRed, 1), arrow);
    }

    private StreamGeometry CreatePieSliceGeometry(Point center, double radius, double startDegrees, double sweepDegrees)
    {
        var geometry = new StreamGeometry();
        using (var ctx = geometry.Open())
        {
            double startRad = startDegrees * Math.PI / 180.0;
            double endRad = (startDegrees + sweepDegrees) * Math.PI / 180.0;

            Point p1 = new Point(center.X + radius * Math.Cos(startRad), center.Y + radius * Math.Sin(startRad));
            Point p2 = new Point(center.X + radius * Math.Cos(endRad), center.Y + radius * Math.Sin(endRad));

            ctx.BeginFigure(center, true);
            ctx.LineTo(p1);
            ctx.ArcTo(p2, new Size(radius, radius), 0, sweepDegrees > 180, SweepDirection.Clockwise);
            ctx.EndFigure(true);
        }
        return geometry;
    }
}