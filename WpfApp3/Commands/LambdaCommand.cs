﻿using SFC.Commands.Base;
using System;

namespace SFC.Commands
{
    internal class LambdaCommand : Command
    {
        private readonly Action<object> _Execute;
        private readonly Func<object, bool> _CanExecute;

        public LambdaCommand(Action onStartHeaterCommand)
        {
            OnStartHeaterCommand = onStartHeaterCommand;
        }

        public LambdaCommand(Action<object> Execute, Func<object, bool> CanExecute = null)
        {
            _Execute = Execute ?? throw new ArgumentException("Execute method can't be null!");
            if (CanExecute != null) _CanExecute = CanExecute;
        }

        public Action OnStartHeaterCommand { get; }

        public override bool CanExecute(object parameter)
        {
            return _CanExecute?.Invoke(parameter) ?? true;
        }

        public override void Execute(object parameter)
        {
            _Execute(parameter);
        }
    }
}
