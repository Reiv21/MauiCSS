using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace MauiCSS.ViewModels;

// base class for all ViewModels.
public abstract class BaseViewModel : INotifyPropertyChanged
{
    // MAUI subscribes to this event in bindings
    public event PropertyChangedEventHandler? PropertyChanged;

    // Fires PropertyChanged for the calling property.
    // [CallerMemberName] fills in the property name automatically so no need to type it manually
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    // use this in every property setter instead of writing the same code
    // returns false and does nothing if the value didnt changed
    // returns true if value changed, setter can then run extra logic
    // ref is needed so the method can actually write to the backing field
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return false;
        }

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
