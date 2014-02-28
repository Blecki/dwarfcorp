using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    /// <summary>
    /// A behavior which wraps a coroutine. (Something which returns IEnumerable(status)))
    /// </summary>
    [Newtonsoft.Json.JsonObject(IsReference = true)]
    public class Wrap : Act
    {
        private Func<IEnumerable<Status>> Function { get; set; }

        public Wrap(Func<IEnumerable<Status>> fn)
        {
            Name = fn.Method.Name;
            Function = fn;
        }

        public override void Initialize()
        {
            Enumerator = Run().GetEnumerator();
        }

        public override IEnumerable<Status> Run()
        {
            return Function();
        }
    }

}