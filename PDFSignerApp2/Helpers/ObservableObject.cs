using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PDFSignerApp.Helpers
{
    /// <summary>
    /// Base class for objects that implement property change notification.
    /// </summary>
    /// <example>
    /// <see cref="MainViewModel"/> inherits from <see cref="ObservableObject"/> to notify the view of property changes.
    /// </example>
    public class ObservableObject : INotifyPropertyChanged
    {
        /// <summary>
        /// Notifies subscribers that a property value has changed.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
