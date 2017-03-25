using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace DwarfCorp
{
    public struct DwarfBux
    {
        private decimal _value;

        public decimal Value
        {
            get { return _value; }
            set { _value = decimal.Round(value, 2); }
        }

        public DwarfBux(decimal value)
        {
            _value = 0;
            Value = value;
        }

        public override string ToString()
        {
            return string.Format("${0}", _value.ToString("N"));
        }

        public static implicit operator decimal(DwarfBux d)
        {
            return d._value;
        }

        public static implicit operator DwarfBux(decimal d)
        {
            return new DwarfBux()
            {
                _value = d
            };
        }

        public static DwarfBux operator -(DwarfBux a)
        {
            return new DwarfBux(-a.Value);
        }

        public static DwarfBux operator+(DwarfBux a, DwarfBux b)
        {
            return new DwarfBux()
            {
                Value = a._value + b._value
            };
        }

        public static DwarfBux operator -(DwarfBux a, DwarfBux b)
        {
            return new DwarfBux()
            {
                Value = a._value + b._value
            };
        }

        public static DwarfBux operator *(DwarfBux a, DwarfBux b)
        {
            return new DwarfBux() {Value = a.Value*b.Value};
        }

        public static DwarfBux operator *(DwarfBux a, int value)
        {
            return new DwarfBux()
            {
                Value = a.Value*(decimal) value
            };
        }

        public static DwarfBux operator *(int value, DwarfBux a)
        {
            return new DwarfBux()
            {
                Value = a.Value * (decimal)value
            };
        }

        public static DwarfBux operator *(DwarfBux a, float value)
        {
            return new DwarfBux()
            {
                Value = a._value*(decimal)value
            };
        }

        public static DwarfBux operator *(DwarfBux a, double value)
        {
            return new DwarfBux()
            {
                Value = a._value*(decimal)value
            };
        }

        public static DwarfBux operator *(DwarfBux a, decimal value)
        {
            return new DwarfBux()
            {
                Value = a._value*(decimal)value
            };
        }

        public static DwarfBux operator *(float value, DwarfBux a)
        {
            return a*value;
        }

        public static DwarfBux operator *(double value, DwarfBux a)
        {
            return a*value;
        }

        public static DwarfBux operator *(decimal value, DwarfBux a)
        {
            return a*value;
        }

        public static DwarfBux operator /(DwarfBux a, decimal value)
        {
            return new DwarfBux() {Value = a.Value/value};
        }

        public static DwarfBux operator /(DwarfBux a, float value)
        {
            return new DwarfBux() { Value = a.Value / (decimal)value };
        }

        public static DwarfBux operator /(DwarfBux a, double value)
        {
            return new DwarfBux() { Value = a.Value / (decimal)value };
        }

        public static DwarfBux operator+(DwarfBux a, float value)
        {
            return new DwarfBux(a._value + (decimal)value);
        }

        public static DwarfBux operator +(float value, DwarfBux a)
        {
            return new DwarfBux(a._value + (decimal)value);
        }

        public static DwarfBux operator -(DwarfBux a, float value)
        {
            return new DwarfBux(a._value - (decimal)value);
        }

        public static DwarfBux operator -(float value, DwarfBux a)
        {
            return new DwarfBux(a._value - (decimal)value);
        }
        public static bool operator >(DwarfBux a, float value)
        {
            return a.Value > (decimal)value;
        }


        public static bool operator <(DwarfBux a, float value)
        {
            return a.Value < (decimal)value;
        }

        public static bool operator >(float value, DwarfBux a)
        {
            return a.Value < (decimal)value;
        }


        public static bool operator <(float value, DwarfBux a)
        {
            return a.Value > (decimal)value;
        }
    }
}
