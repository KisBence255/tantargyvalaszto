using Avalonia.Media;

namespace tantargyvalaszto.Models;

public class SubjectItem
{
    public string Name { get; set; } = string.Empty;
    public IBrush Brush { get; set; } = Brushes.Gray;
    public Color Color { get; set; } = Colors.Gray;
}