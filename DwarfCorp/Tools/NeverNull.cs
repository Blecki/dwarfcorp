using Newtonsoft.Json;
using System;

namespace DwarfCorp
{
    public struct NeverNull<T> where T: class
    {
        [JsonProperty] private T _Value;
        public T Value => _Value;

        public NeverNull(T _Value)
        {
            if (_Value == null) throw new InvalidOperationException();
            this._Value = _Value;
        }        

        public static implicit operator NeverNull<T>(T _Value)
        {
            return new NeverNull<T>(_Value);
        }
    }
}
