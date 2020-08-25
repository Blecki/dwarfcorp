using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    public struct MaybeNull<T> where T: class
    {
        [JsonProperty] private T _Value;

        public MaybeNull(T _Value)
        {
            this._Value = _Value;
        }        

        public static implicit operator MaybeNull<T>(T _Value)
        {
            return new MaybeNull<T>(_Value);
        }

        public bool HasValue(out T Value)
        {
            Value = _Value;
            return _Value != null;
        }

        public bool HasValue()
        {
            return _Value != null;
        }

        public bool ReferenceEquals(MaybeNull<T> Other)
        {
            return Object.ReferenceEquals(_Value, Other._Value);
        }

        public override string ToString()
        {
            if (_Value == null)
                return "NULL";
            return _Value.ToString();
        }
    }
}
