using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using tantargyvalaszto.Models;

namespace tantargyvalaszto.ViewModels;

public class MainWindowViewModel : ViewModelBase
{
    private static readonly Color[] ColorPalette = new[]
    {
        Color.Parse("#3498DB"), Color.Parse("#E74C3C"), Color.Parse("#2ECC71"),
        Color.Parse("#F1C40F"), Color.Parse("#9B59B6"), Color.Parse("#1ABC9C"),
        Color.Parse("#E67E22"), Color.Parse("#34495E"), Color.Parse("#D35400"),
        Color.Parse("#C0392B"), Color.Parse("#16A085"), Color.Parse("#8E44AD")
    };

    private string _newSubjectName = string.Empty;
    private string _errorMessage = string.Empty;
    private string _selectedResultText = "Adj hozzá legalább 2 tárgyat!";
    private double _wheelAngle;
    private bool _isSpinning;
    private bool _isDarkTheme = true;

    public ObservableCollection<SubjectItem> Subjects { get; } = new();

    public string NewSubjectName
    {
        get => _newSubjectName;
        set { _newSubjectName = value; OnPropertyChanged(); }
    }

    public string ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public string SelectedResultText
    {
        get => _selectedResultText;
        set { _selectedResultText = value; OnPropertyChanged(); }
    }

    // DINAMIKUS SZÁMLÁLÓ PROPERTIE
    public string RemainingCountText => $"Még felvehető: {25 - Subjects.Count} / 25 db";

    public double WheelAngle
    {
        get => _wheelAngle;
        set { _wheelAngle = value; OnPropertyChanged(); }
    }

    public bool IsSpinning
    {
        get => _isSpinning;
        set
        {
            _isSpinning = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(CanSpin));
            OnPropertyChanged(nameof(CanAdd));
            UpdateCommandStates();
        }
    }

    public bool IsDarkTheme
    {
        get => _isDarkTheme;
        set
        {
            _isDarkTheme = value;
            OnPropertyChanged();
            ApplyTheme();
        }
    }

    public bool CanSpin => Subjects.Count >= 2 && !IsSpinning;
    public bool CanAdd => !IsSpinning;

    public ICommand AddSubjectCommand { get; }
    public ICommand RemoveSubjectCommand { get; }
    public ICommand SpinCommand { get; }
    public ICommand ToggleThemeCommand { get; }

    public MainWindowViewModel()
    {
        AddSubjectCommand = new SimpleRelayCommand(_ => AddSubject(), _ => CanAdd);
        RemoveSubjectCommand = new SimpleRelayCommand(param => RemoveSubject(param as SubjectItem), _ => !IsSpinning);
        SpinCommand = new SimpleRelayCommand(async _ => await SpinWheelAsync(), _ => CanSpin);
        ToggleThemeCommand = new SimpleRelayCommand(_ => IsDarkTheme = !IsDarkTheme);

        Subjects.CollectionChanged += (_, _) =>
        {
            UpdateStatusText();
            UpdateCommandStates();
            OnPropertyChanged(nameof(RemainingCountText)); // Számláló frissítése
        };
    }

    private void UpdateStatusText()
    {
        if (IsSpinning) return;

        if (Subjects.Count == 0)
            SelectedResultText = "Adj hozzá legalább 2 tárgyat!";
        else if (Subjects.Count == 1)
            SelectedResultText = "Még 1 tárgy szükséges a sorsoláshoz!";
        else if (!SelectedResultText.StartsWith("Kisorsolva:"))
            SelectedResultText = "Sorsolj egy tárgyat!";

        if (Subjects.Count < 25 && ErrorMessage.Contains("25"))
        {
            ErrorMessage = string.Empty;
        }
    }

    private void UpdateCommandStates()
    {
        OnPropertyChanged(nameof(CanSpin));
        ((SimpleRelayCommand)SpinCommand).RaiseCanExecuteChanged();
        ((SimpleRelayCommand)AddSubjectCommand).RaiseCanExecuteChanged();
        ((SimpleRelayCommand)RemoveSubjectCommand).RaiseCanExecuteChanged();
    }

    private void AddSubject()
    {
        ErrorMessage = string.Empty;
        string trimmedName = NewSubjectName.Trim();

        if (string.IsNullOrWhiteSpace(trimmedName))
        {
            ErrorMessage = "A tárgy neve nem lehet üres!";
            return;
        }

        if (trimmedName.Length > 25)
        {
            ErrorMessage = "A tárgy neve maximum 25 karakter lehet!";
            return;
        }

        if (Subjects.Count >= 25)
        {
            ErrorMessage = "Maximum 25 tárgy vihető fel!";
            return;
        }

        Color allocatedColor = ColorPalette[Subjects.Count % ColorPalette.Length];
        Subjects.Add(new SubjectItem
        {
            Name = trimmedName,
            Color = allocatedColor,
            Brush = new SolidColorBrush(allocatedColor)
        });

        NewSubjectName = string.Empty;
    }

    private void RemoveSubject(SubjectItem? item)
    {
        if (item != null && !IsSpinning)
        {
            Subjects.Remove(item);
        }
    }

    private async Task SpinWheelAsync()
    {
        if (!CanSpin) return;

        IsSpinning = true;
        ErrorMessage = string.Empty;
        SelectedResultText = "Sorsolás...";

        Random rnd = new Random();
        int winnerIndex = rnd.Next(0, Subjects.Count);

        double sliceAngle = 360.0 / Subjects.Count;
        double targetSliceCenter = (winnerIndex * sliceAngle) + (sliceAngle / 2.0);
        double targetAngle = (360 * 5) + (270 - targetSliceCenter);

        double currentAngle = WheelAngle % 360;
        double totalRotation = targetAngle - currentAngle;

        int steps = 100;
        for (int i = 0; i < steps; i++)
        {
            double progress = (double)i / steps;
            double easeOut = 1 - Math.Pow(1 - progress, 3);
            WheelAngle = currentAngle + (totalRotation * easeOut);
            await Task.Delay(16);
        }

        WheelAngle = targetAngle % 360;
        SelectedResultText = $"Kisorsolva: {Subjects[winnerIndex].Name}";
        IsSpinning = false;
    }

    private void ApplyTheme()
    {
        if (Application.Current != null)
        {
            Application.Current.RequestedThemeVariant = IsDarkTheme ? ThemeVariant.Dark : ThemeVariant.Light;
        }
    }
}