using System;
using System.Windows.Input;

namespace StartreckSimulator.ViewModels
{
    public class Command : ICommand
    {
        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }

        private readonly Action _action;
        private readonly Action<object> _actionParam;
        private readonly Func<bool> _condition;

        public bool CanExecute(object parameter)
        {
            return _condition();
        }

        public void Execute(object parameter)
        {
            if (_action != null)
            {
                _action();
            }
            else
            {
                _actionParam(parameter);
            }
        }

        public Command(Action action, Func<bool> condition)
        {
            _action = action;
            _condition = condition;
        }

        public Command(Action<object> action, Func<bool> condition)
        {
            _actionParam = action;
            _condition = condition;
        }

        public Command(Action action)
        {
            _action = action;
            _condition = () => true;
        }

        public Command(Action<object> action)
        {
            _actionParam = action;
            _condition = () => true;
        }
    }
}