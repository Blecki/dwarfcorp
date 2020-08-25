using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Net.Configuration;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public partial class GameComponent
    {
        public string Name { get; set; }
        public uint GlobalID { get; set; }
        [JsonIgnore] public Gui.Widget GuiTag = null;

        [JsonProperty] private uint ParentID = ComponentManager.InvalidID;
        [JsonIgnore] private GameComponent CachedParent = null;

        [JsonIgnore]
        public GameComponent Parent
        {
            get
            {
                if (World == null || ParentID == ComponentManager.InvalidID)
                    return null;

                if (CachedParent == null)
                    CachedParent = Manager.FindComponent(ParentID);

                return CachedParent;
            }
            set
            {
                CachedParent = value;
                ParentID = value != null ? value.GlobalID : ComponentManager.InvalidID;
            }
        }

        public Flag Flags = 0;
        public List<string> Tags { get; set; }
        public List<uint> SerializableChildren;

        [JsonIgnore]
        public List<GameComponent> Children { get; set; }

        [JsonIgnore]
        public WorldManager World { get;  set; }

        [JsonIgnore]
        public ComponentManager Manager { get { return World.ComponentManager; } }


        #region Serialization

        public void PrepareForSerialization()
        {
            SerializableChildren = Children.Where(c => c.IsFlagSet(Flag.ShouldSerialize) && c != this)
                .Select(c => c.GlobalID).ToList();
        }

        public void PostSerialization(ComponentManager Manager)
        {
            Children = SerializableChildren.Select(id => Manager.FindComponent(id)).ToList();
            Children.RemoveAll(c => c == this || c == null);
            SerializableChildren = null;
        }

        [OnDeserialized]
        void OnDeserializing(StreamingContext context)
        {
            // Assume the context passed in is a WorldManager
            World = ((WorldManager)context.Context);
        }

        //public virtual void CreateCosmeticChildren(ComponentManager Manager)
        //{

        //}

        #endregion

        #region Flags

        [Flags]
        public enum Flag
        {
            Visible = 1,
            Active = 2,
            Dead = 4,
            ShouldSerialize = 8,
            RotateBoundingBox = 64,
            DontUpdate = 128,
        }

        public bool IsFlagSet(Flag F)
        {
            return (Flags & F) == F;
        }

        public GameComponent SetFlag(Flag F, bool Value)
        {
            if (Value)
                Flags |= F;
            else
                Flags &= ~F;

            return this;
        }

        public void SetFlagRecursive(Flag F, bool Value)
        {
            SetFlag(F, Value);
            foreach (var child in Children ?? Enumerable.Empty<GameComponent>())
                if (child != null) child.SetFlagRecursive(F, Value); //A null child is a problem, no?
        }

        #endregion
        
        // Todo: Get rid of these helpers.
        [JsonIgnore] 
        public bool IsVisible
        {
            get { return IsFlagSet(Flag.Visible); }
            set { SetFlag(Flag.Visible, value); }
        }

        [JsonIgnore]
        public bool Active
        {
            get { return IsFlagSet(Flag.Active); }
            set { SetFlag(Flag.Active, value); }
        }

        [JsonIgnore]
        public bool IsDead
        {
            get { return IsFlagSet(Flag.Dead); }
            set {
                SetFlag(Flag.Dead, value);
            }
        }
        
        // Todo: Kill
        public virtual void ReceiveMessageLater(Message message)
        {
            if (Manager == null)
            {
                return;
            }

            Manager.ReceiveMessageLater(this, message);
        }

        // Todo: Kill
        public virtual void ReceiveMessageRecursive(Message messageToReceive)
        {
            var children = Children.ToArray();
            // Todo: Possible race condition?
            foreach(GameComponent child in children)
            {
                child.ReceiveMessageRecursive(messageToReceive);
            }
        }

        #region Constructors

        public GameComponent()
        {
        }

        public GameComponent(ComponentManager Manager) : this()
        {
            global::System.Diagnostics.Debug.Assert(Manager != null, "Manager cannot be null");

            Children = new List<GameComponent>();
            Tags = new List<string>();
            Name = "uninitialized";

            Flags = Flag.Active | Flag.Visible | Flag.ShouldSerialize;
            Parent = null;

            if (Manager == null)
                throw new InvalidProgramException("Null manager given to game component.");

            World = Manager.World;
            Manager.AddComponent(this);
        }

        public GameComponent(string name, ComponentManager manager) :
            this(manager)
        {
            Name = name;
        }

        #endregion

        //public virtual void Update(DwarfTime Time, ChunkManager Chunks, Camera Camera) {
        //}

        public virtual void UpdatePaused(DwarfTime Time, ChunkManager Chunks, Camera Camera)
        { }

        public virtual void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera,
            SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            
        }

        public override bool Equals(object obj)
        {
            return Object.ReferenceEquals(this, obj);
        }

        public override int GetHashCode()
        {
            return (int) GlobalID;
        }

        // Todo: Can these two functions be unified?
        /// <summary> This is intended to be used like Die(), but is garunteed only
        /// to clean up memory, without doing things like animations.</summary>
        public virtual void Delete()
        {
            if (IsDead)
                return;

            IsDead = true;

            RemoveFromOctTree();

            var localList = new List<GameComponent>(Children);
            foreach (var child in localList)
                child.Delete();

            if (Parent != null) Parent.RemoveChild(this);

            Active = false;
            IsVisible = false;
            Manager.RemoveComponent(this);
        }

        /// <summary>
        /// Deletes this instance and calls Die() on all descendents. Removes this instance from the parent.
        /// </summary>
        public virtual void Die()
        {
            // Todo: Split into this and 'OnDie' event function.
            if(IsDead)
                return;

            IsDead = true;

            RemoveFromOctTree();
            if (OnDestroyed != null) OnDestroyed();

            var localList = new List<GameComponent>(Children);
            foreach (var child in localList)
                if (child != null) child.Die();

            if (Parent != null) Parent.RemoveChild(this);

            Active = false;
            IsVisible = false;
            Manager.RemoveComponent(this);
        }

        /// <summary>
        /// Gets a description of this component.
        /// </summary>
        /// <returns>Descriptive text about the component</returns>
        public virtual string GetDescription()
        {
            string toReturn = "";

            if (Object.ReferenceEquals(Parent, Manager.RootComponent))
            {
                toReturn += Name;
                if (IsReserved)
                {
                    if (ReservedFor.GetRoot().GetComponent<Creature>().HasValue(out var creature))
                        toReturn += " (Reserved for " + creature.Stats.FullName + ")";
                    else
                        toReturn += " (Reserved)";
                }
            }

            foreach (GameComponent component in Children)
            {
                string componentDesc = component.GetDescription();

                if (!String.IsNullOrEmpty(componentDesc))
                {
                    toReturn += "\n ";
                    toReturn += componentDesc;
                }
            }

            return toReturn;
        }

        #region heirarchy operators

        /// <summary>
        /// Gets the first child component of the specified type from among the children.
        /// </summary>
        /// <typeparam name="T">The type</typeparam>
        /// <returns>The first component of type T.</returns>
        public MaybeNull<T> GetComponent<T>() where T : GameComponent
        {
            return EnumerateAll().OfType<T>().FirstOrDefault();
        }


        /// <summary>
        /// Adds the child if it has not been added already.
        /// </summary>
        /// <param name="child">The child.</param>
        public GameComponent AddChild(GameComponent child)
        {
            if (child == this)
            {
                throw new InvalidOperationException("Object added to itself");
            }
            lock (Children)
            {
                global::System.Diagnostics.Debug.Assert(child.Parent == null, "Child was already added to another component. Child is a " + child.GetType().Name);

                Children.Add(child);

                if (this != Manager.RootComponent)
                {
                    child.Active = Active;
                    child.IsVisible = IsVisible;
                    child.IsDead = IsDead;
                }

                child.Parent = this;
            }

            return child;
        }

        /// <summary>
        /// Removes the child if it exists.
        /// </summary>
        /// <param name="child">The child.</param>
        public void RemoveChild(GameComponent child)
        {
            lock (Children)
            {
                Children.Remove(child);
            }
        }

        /// <summary>
        /// Gets the anscestor of this component which has no parent.
        /// </summary>
        /// <returns>The anscestor of this component with no parent.</returns>
        public GameComponent GetRoot()
        {
            var p = this;

            while(!p.IsRoot())
                p = p.Parent;

            return p;
        }

        public bool IsRoot()
        {
            return Parent == null || Object.ReferenceEquals(Parent, Manager.RootComponent);
        }

        public IEnumerable<GameComponent> EnumerateAll()
        {
            yield return this;
            foreach (var child in Children ?? Enumerable.Empty<GameComponent>())
                foreach (var grandChild in child.EnumerateAll())
                    yield return grandChild;
        }

        public IEnumerable<GameComponent> EnumerateChildren()
        {
            foreach (var child in Children)
                yield return child;
        }

        #endregion
    }
}
