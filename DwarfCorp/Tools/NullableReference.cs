using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    public struct NullableReference<T> where T: class
    {
        [JsonProperty] private T _Value;

        public NullableReference(T _Value)
        {
            this._Value = _Value;
        }        

        public static implicit operator NullableReference<T>(T _Value)
        {
            return new NullableReference<T>(_Value);
        }

        public bool HasValue(out T Value)
        {
            Value = _Value;
            return _Value != null;
        }
    }
}
