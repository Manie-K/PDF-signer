using System.Windows.Input;

namespace PDFSignerApp.Helpers
{
    /// <summary>
    /// A command that can be bound to UI elements, allowing for execution of actions with optional conditions.
    /// </summary>
    /// <example> Buttons in <see cref="PDFSignView"/> view. </example>
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        /// <summary>
        /// Consturctor for RelayCommand that takes an action to execute and a function to determine if it can execute.
        /// <paramref name="canExecute"/> Represents a function that returns a boolean indicating whether the command can execute.
        /// <paramref name="execute"/> Represents the action to execute when the command is invoked.
        /// </summary>
        public RelayCommand(Action execute, Func<bool> canExecute)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        /// <summary>
        /// Tests whether the command can execute in its current state.
        /// </summary>
        /// <returns> True if command can execute, False otherwise. </returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute.Invoke();
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        public void Execute(object? parameter)
        {
            _execute.Invoke();
        }
    }
}