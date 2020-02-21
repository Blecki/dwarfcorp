using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public class Always : Act
    {
        public Act.Status AlwaysStatus = Act.Status.Success;

        public Always(Act.Status status)
        {
            AlwaysStatus = status;
        }

        public override IEnumerable<Status> Run()
        {
            LastTickedChild = this;
            yield return AlwaysStatus;
            yield break;
        }
    }

}