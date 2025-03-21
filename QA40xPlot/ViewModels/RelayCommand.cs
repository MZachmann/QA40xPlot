using System.Windows.Input;
using static ABI.System.Windows.Input.ICommand_Delegates;

namespace QA40xPlot.ViewModels
{
    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object?> _canExecute;

		public RelayCommand(Action<object> execute)
        {
			_execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = x => true;
		}

		public RelayCommand(Action<object>? execute, Predicate<object?> canExecute)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter)
        {
            return (_canExecute == null) || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            if (parameter != null)
            {
				_execute(parameter);
			}
		}

        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}
