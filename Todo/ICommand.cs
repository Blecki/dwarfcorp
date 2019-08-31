using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoList
{
    internal interface ICommand
    {
        void Invoke();
    }
}