
using System;

namespace Yarn
{
    // A value from inside Yarn.
    public class Value : IComparable, IComparable<Value> {
        public static readonly Value NULL = new Value();

        public enum Type {
            Number,  // a constant number
            String,  // a string
            Bool,    // a boolean value
            Variable, // the name of a variable; will be expanded at runtime
            Object, // We can't tell what it is, but the client code wants us to store it.
            Null,    // the null value
        }

        public Value.Type type { get; internal set; }

        private Object backingValue;

        public float AsNumber {
            get {
                switch (type) {
                    case Type.Number:
                        return (backingValue as float?).Value;
                    case Type.String:
                        try {
                            return float.Parse (backingValue.ToString());
                        }  catch (FormatException) {
                            return 0.0f;
                        }
                    case Type.Bool:
                        return (backingValue as bool?).Value ? 1.0f : 0.0f;
                    case Type.Null:
                    case Type.Object:
                        return 0.0f;
                    default:
                        throw new InvalidOperationException ("Cannot cast to number from " + type.ToString());
                }
            }
        }

        public bool AsBool {
            get {
                switch (type) {
                    case Type.Number:
                        var v = (backingValue as float?).Value;
                        return !float.IsNaN(v) && v != 0.0f;
                    case Type.String:
                        return !String.IsNullOrEmpty(backingValue.ToString()); // What??
                    case Type.Bool:
                        return (backingValue as bool?).Value;
                    case Type.Null:
                        return false;
                    case Type.Object:
                        return true;
                    default:
                        throw new InvalidOperationException ("Cannot cast to bool from " + type.ToString());
                }
            }
        }

        public string AsString {
            get {
                if (backingValue == null)
                    return "null";
                return backingValue.ToString();
            }
        }

        public Object AsObject
        {
            get
            {
                return backingValue;
            }
        }

        // Create a null value
        public Value () : this(null) { }

        // Create a value with a C# object
        public Value(object value)
        {
            // Copy an existing value
            if (typeof(Value).IsInstanceOfType(value))
            {
                var otherValue = value as Value;
                type = otherValue.type;
                backingValue = otherValue.backingValue;
            }
            else
            {
                backingValue = value;

                if (value == null)
                    type = Type.Null;
                else if (value.GetType() == typeof(string))
                    type = Type.String;
                else if (value.GetType() == typeof(int) ||
                    value.GetType() == typeof(float) ||
                    value.GetType() == typeof(double))
                {
                    type = Type.Number;
                    backingValue = System.Convert.ToSingle(value);
                }
                else if (value.GetType() == typeof(bool))
                    type = Type.Bool;
                else
                    type = Type.Object;
            }
        }

        public virtual int CompareTo(object obj) {
            if (obj == null) return 1;

            // soft, fast coercion
            var other = obj as Value;

            // not a value
            if( other == null ) throw new ArgumentException("Object is not a Value");

            // it is a value!
            return this.CompareTo(other);
        }

        public virtual int CompareTo(Value other) {
            if (other == null) {
                return 1;
            }

            if (other.type == this.type)
            {
                switch (this.type)
                {
                    case Type.Null:
                        return 0;
                    case Type.String:
                        return this.AsString.CompareTo(other.AsString);
                    case Type.Number:
                        return this.AsNumber.CompareTo(other.AsNumber);
                    case Type.Bool:
                        return this.AsBool.CompareTo(other.AsBool);
                    default:
                        break; // Let the string test do it.
                }
            }

            // try to do a string test at that point!
            return this.AsString.CompareTo(other.AsString);
        }

        public override bool Equals (object obj)
        {
            if (obj == null || GetType() != obj.GetType()) {
                return false;
            }

            var other = (Value)obj;

            switch (this.type) {
            case Type.Number:
                return this.AsNumber == other.AsNumber;
            case Type.String:
                return this.AsString == other.AsString;
            case Type.Bool:
                return this.AsBool == other.AsBool;
            case Type.Null:
                return other.type == Type.Null || other.AsNumber == 0 || other.AsBool == false; // Why does null = 0 or false?
                case Type.Object:
                    return Object.ReferenceEquals(this.backingValue, other.backingValue);
            default:
                throw new ArgumentOutOfRangeException ();
            }

        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            // TODO: yeah hay maybe fix this
            //  - Just use 'this' as the hash.
            if(backingValue != null ) {
                return backingValue.GetHashCode();
            }

            return 0;
        }

        public override string ToString ()
        {
            return string.Format ("[Value: type={0}, AsNumber={1}, AsBool={2}, AsString={3}]", type, AsNumber, AsBool, AsString);
        }

        public static Value operator+ (Value a, Value b) {
            // catches:
            // undefined + string
            // number + string
            // string + string
            // bool + string
            // null + string
            if (a.type == Type.String || b.type == Type.String ) {
                // we're headed for string town!
                return new Value( a.AsString + b.AsString );
            }

            // catches:
            // number + number
            // bool (=> 0 or 1) + number
            // null (=> 0) + number
            // bool (=> 0 or 1) + bool (=> 0 or 1)
            // null (=> 0) + null (=> 0)
            if ((a.type == Type.Number || b.type == Type.Number) ||
                (a.type == Type.Bool && b.type == Type.Bool) ||
                (a.type == Type.Null && b.type == Type.Null)
            ) {
                return new Value( a.AsNumber + b.AsNumber );
            }

            throw new System.ArgumentException(
                string.Format("Cannot add types {0} and {1}.", a.type, b.type )
            );
        }

        public static Value operator- (Value a, Value b) {
            if (a.type == Type.Number && (b.type == Type.Number || b.type == Type.Null) ||
                b.type == Type.Number && (a.type == Type.Number || a.type == Type.Null)
            ) {
                return new Value( a.AsNumber - b.AsNumber );
            }

            throw new System.ArgumentException(
                string.Format("Cannot subtract types {0} and {1}.", a.type, b.type )
            );
        }

        public static Value operator* (Value a, Value b) {
            if (a.type == Type.Number && (b.type == Type.Number || b.type == Type.Null) ||
                b.type == Type.Number && (a.type == Type.Number || a.type == Type.Null)
            ) {
                return new Value( a.AsNumber * b.AsNumber );
            }

            throw new System.ArgumentException(
                string.Format("Cannot multiply types {0} and {1}.", a.type, b.type )
            );
        }

        public static Value operator/ (Value a, Value b) {
            if (a.type == Type.Number && (b.type == Type.Number || b.type == Type.Null) ||
                b.type == Type.Number && (a.type == Type.Number || a.type == Type.Null)
            ) {
                return new Value( a.AsNumber / b.AsNumber );
            }

            throw new System.ArgumentException(
                string.Format("Cannot divide types {0} and {1}.", a.type, b.type )
            );
        }

        public static Value operator %(Value a, Value b) {
            if (a.type == Type.Number && (b.type == Type.Number || b.type == Type.Null) ||
                b.type == Type.Number && (a.type == Type.Number || a.type == Type.Null)) {
                return new Value (a.AsNumber % b.AsNumber);
            }
            throw new System.ArgumentException(
                string.Format("Cannot modulo types {0} and {1}.", a.type, b.type )
            );
        }

        public static Value operator- (Value a) {
            if( a.type == Type.Number ) {
                return new Value( -a.AsNumber );
            }
            if (a.type == Type.Null &&
                a.type == Type.String &&
               (a.AsString == null || a.AsString.Trim() == "")
            ) {
                return new Value( -0 );
            }
            return new Value( float.NaN );
        }

        // Define the is greater than operator.
        public static bool operator >  (Value operand1, Value operand2)
        {
            return operand1.CompareTo(operand2) == 1;
        }

        // Define the is less than operator.
        public static bool operator <  (Value operand1, Value operand2)
        {
            return operand1.CompareTo(operand2) == -1;
        }

        // Define the is greater than or equal to operator.
        public static bool operator >=  (Value operand1, Value operand2)
        {
            return operand1.CompareTo(operand2) >= 0;
        }

        // Define the is less than or equal to operator.
        public static bool operator <=  (Value operand1, Value operand2)
        {
            return operand1.CompareTo(operand2) <= 0;
        }
    }
}
