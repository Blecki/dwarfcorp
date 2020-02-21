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

    /// <summary>
    /// An act is an another Name for a "Behavior". Behaviors are linked together into an "behavior tree". Each behavior is a coroutine
    /// which can either be running, succeed, or fail. 
    /// </summary>
    public class Act
    {
        public enum Status
        {
            Running,
            Fail,
            Success
        }


        public List<Act> Children { get; set; }

        public string Name = "Act";
        public bool IsInitialized { get; set; }

        [JsonIgnore]
        public IEnumerator<Status> Enumerator;

        [JsonIgnore]
        public Act LastTickedChild { get; set; }

        public bool IsCanceled { get; set; }

        public Act()
        {
            IsInitialized = false;
            Children = new List<Act>();
        }

        public Status Tick()
        {
            if(Enumerator == null)
                Initialize();

            if (Enumerator != null)
                Enumerator.MoveNext();
            else
                return Status.Fail;

            return Enumerator.Current;
        }

        public virtual void Initialize()
        {
            Enumerator = Run().GetEnumerator();
            IsInitialized = true;
        }

        public virtual IEnumerable<Status> Run()
        {
            throw new NotImplementedException();
        }

        public virtual void OnCanceled()
        {
            IsCanceled = true;

            if(Children != null)
                foreach (Act child in Children)
                    child.OnCanceled();
        }
    }
}