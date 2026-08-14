using System;
using System.Threading.Tasks;
using System.Windows.Input;

namespace WpfMediatRLearningApp.Core
{
    /// <summary>
    /// A standard implementation of ICommand that supports async operations.
    /// In WPF, Command.Execute is void, so we must handle exceptions here 
    /// to prevent the application from crashing silently.
    /// </summary>
    public class RelayCommand : ICommand
    {
        private readonly Func<Task> _execute;
        private readonly Func<bool>? _canExecute;

        public RelayCommand(Func<Task> execute, Func<bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public event EventHandler? CanExecuteChanged;

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public async void Execute(object? parameter)
        {
            try
            {
                // Execute the async task. 
                // Note: We do not await here because ICommand.Execute returns void.
                await _execute();
            }
            catch (Exception ex)
            {
                // Section 10.4: Handle errors at the boundary. 
                // In production, you might use a centralized dialog service instead of MessageBox.
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}", "Error", 
                    System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }

        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
