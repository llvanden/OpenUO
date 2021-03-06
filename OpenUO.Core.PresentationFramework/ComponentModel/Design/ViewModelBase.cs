﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Windows;
using System.Windows.Threading;
using OpenUO.Core.PresentationFramework.Input;

namespace OpenUO.Core.PresentationFramework.ComponentModel.Design
{
    public abstract class ViewModelBase : PropertyChangedNotifierBase, IDataErrorInfo
    {
        private readonly List<CommandBase> _commands;
        private readonly ConcurrentDictionary<string, object> _errors;

        public List<CommandBase> Commands
        {
            get { return _commands; }
        }

        public Dispatcher Dispatcher
        {
            get { return Dispatcher.CurrentDispatcher; }
        }

        public bool IsDesignMode
        {
            get { return (bool)(DesignerProperties.IsInDesignModeProperty.GetMetadata(typeof (DependencyObject)).DefaultValue); }
        }

        public string Error
        {
            get { return string.Empty; }
        }

        public ViewModelBase()
        {
            _commands = new List<CommandBase>();
            _errors = new ConcurrentDictionary<string, object>();
        }

#if NET_45
        protected override void OnPropertyChanged([CallerMemberName] string propertyName = null)
#else
        protected override void OnPropertyChanged(string propertyName = null)
#endif
        {
            foreach (CommandBase command in _commands)
            {
                command.RaiseCanExecuteChanged();
            }

            base.OnPropertyChanged(propertyName);
        }

        protected CommandBase CreateCommand(Action execute)
        {
            return CreateCommand(execute, null);
        }

        protected CommandBase CreateCommand(Action execute, Func<bool> canExecute)
        {
            if (canExecute == null)
            {
                canExecute = () => true;
            }

            CommandBase command = new RelayCommand(execute, canExecute);

            RegisterCommand(command);

            return command;
        }

        protected CommandBase CreateCommand<T>(Action<T> execute)
        {
            return CreateCommand(execute, null);
        }

        protected CommandBase CreateCommand<T>(Action<T> execute, Func<T, bool> canExecute)
        {
            if (canExecute == null)
            {
                canExecute = (o) => true;
            }

            CommandBase command = new RelayCommand<T>(execute, canExecute);

            RegisterCommand(command);

            return command;
        }

        protected bool RegisterCommand(CommandBase command)
        {
            if (!_commands.Contains(command))
            {
                _commands.Add(command);
                return false;
            }

            return true;
        }

        protected void CancelError<T>(Expression<Func<T>> propertyExpression, object error)
        {
            string propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
            CancelError(propertyName, error);
        }

        protected void CancelError(string propertyName, object error)
        {
            object value;

            if (_errors.TryRemove(propertyName, out value))
            {
                OnPropertyChanged(propertyName);
            }
        }

        protected object GetError<T>(Expression<Func<T>> propertyExpression)
        {
            string propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
            return GetError(propertyName);
        }

        protected object GetError(string propertyName)
        {
            object error = null;

            if (_errors.ContainsKey(propertyName))
            {
                _errors.TryRemove(propertyName, out error);
            }

            return error;
        }

        protected void RegisterError<T>(Expression<Func<T>> propertyExpression, object error)
        {
            string propertyName = PropertySupport.ExtractPropertyName(propertyExpression);
            RegisterError(propertyName, error);
        }

        protected void RegisterError(string propertyName, object error)
        {
            _errors[propertyName] = error;
            OnPropertyChanged(propertyName);
        }

        protected DispatcherTimer CreateTimer(TimeSpan interval, EventHandler onTick)
        {
            return CreateTimer(interval, onTick, false);
        }

        protected DispatcherTimer CreateTimer(TimeSpan interval, EventHandler onTick, bool start)
        {
            var timer = new DispatcherTimer();

            timer.Interval = interval;
            timer.Tick += onTick;

            if (start)
            {
                timer.Start();
            }

            return timer;
        }

        public string this[string columnName]
        {
            get
            {
                object result = GetError(columnName);
                if (result != null)
                {
                    return result.ToString();
                }
                return null;
            }
        }
    }
}