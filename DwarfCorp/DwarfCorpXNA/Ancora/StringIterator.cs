using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ancora
{
    public struct StringIterator
    {
        private String Data;
        private int Place;

        public String GetLocationDescription()
        {
            var line = Data.Take(Place).Count(c => c == '\n');
            var column = 0;
            var p = Place - 1;
            while (p >= 0 && Data[p] != '\n')
            {
                --p;
                ++column;
            }

            return String.Format("{0}:{1}", line, column);
        }

        public StringIterator(String Data)
        {
            this.Data = Data;
            this.Place = 0;
        }

        public StringIterator(String Data, int Offset)
        {
            this.Data = Data;
            this.Place = Offset;
        }

        public String Peek(int Count)
        {
            if (Place + Count > Data.Length) throw new IndexOutOfRangeException();
            return Data.Substring(Place, Count);
        }

        public char Next
        {
            get
            {
                if (AtEnd) throw new IndexOutOfRangeException();
                return Data[Place];
            }
        }

        public bool AtEnd
        {
            get { return Place >= Data.Length; }
        }

        public StringIterator Advance()
        {
            return new StringIterator(Data, Place + 1);
        }

        public StringIterator Advance(int Count)
        {
            return new StringIterator(Data, Place + Count);
        }

        public StringIterator Rewind()
        {
            return new StringIterator(Data, Math.Max(0, Place - 1));
        }

        public StringIterator Rewind(int Count)
        {
            return new StringIterator(Data, Math.Max(0, Place - Count));
        }
    }
}
