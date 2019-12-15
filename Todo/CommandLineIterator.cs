using System;
using System.Collections.Generic;
using System.Linq;

namespace TodoList
{
    public struct CommandLineIterator
    {   
        public readonly String[] Arguments;
        public readonly int Place;

        public String Peek()
        {
            return Arguments[Place].ToLower();
        }

        public CommandLineIterator(String[] Arguments, int Place)
        {
            this.Arguments = Arguments;
            this.Place = Place;
        }

        public CommandLineIterator Advance()
        {
            return new CommandLineIterator(Arguments, Place + 1);
        }   

        public bool AtEnd()
        {
            return Place >= Arguments.Length;
        } 
    }
}