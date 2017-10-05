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
    [Saving.SaveableObject(0)]
    public class GameComponent : Saving.ISaveableObject
    {
        public string Name { get; set; }
        public uint GlobalID { get; set; }
        public GameComponent Parent { get; set; }
        public Flag Flags = 0;
        public List<string> Tags { get; set; }
        public List<GameComponent> SerializableChildren;
        public List<GameComponent> Children { get; set; }

        private class SaveNugget : Saving.Nugget
        {
            public uint GlobalID;
            public string Name;
            public Flag Flags;
            public List<String> Tags;
            public Saving.Nugget Parent;
            public List<Saving.Nugget> Children;            
        }

        protected virtual Saving.Nugget PrepareSaveNugget(Saving.Saver SaveSystem)
        {
            return new SaveNugget
            {
                AssociatedType = typeof(GameComponent), // Have to explicitely set since the save system
                Version = 0,                            // isn't being invoked to create nugget.
                GlobalID = GlobalID,
                Name = Name,
                Flags = Flags,
                Tags = Tags,
                Parent = SaveSystem.SaveObject(Parent),
                Children = SerializableChildren.Select(c => SaveSystem.SaveObject(c)).ToList()
            };
        }

        protected virtual void LoadFromSaveNugget(Saving.Loader SaveSystem, Saving.Nugget From)
        {
            var n = SaveSystem.UpgradeNugget(From, 0) as SaveNugget; // Need to explicitly invoke since save
                // system was not involved for base type nugget.

            GlobalID = n.GlobalID;
            Name = n.Name;
            Flags = n.Flags;
            Tags = n.Tags;

            Parent = SaveSystem.LoadObject(n.Parent) as GameComponent;
            SerializableChildren = n.Children.Select(c => SaveSystem.LoadObject(c) as GameComponent).ToList();
        }

        Saving.Nugget Saving.ISaveableObject.SaveToNugget(Saving.Saver SaveSystem)
        {
            return PrepareSaveNugget(SaveSystem);
        }

        void Saving.ISaveableObject.LoadFromNugget(Saving.Loader SaveSystem, Saving.Nugget From)
        {
            LoadFromSaveNugget(SaveSystem, From);
        }

        #region Serialization

        public void PrepareForSerialization()
        {
            SerializableChildren = Children.Where(c => c.IsFlagSet(Flag.ShouldSerialize) && c != this).ToList();
        }

        public void PostSerialization(ComponentManager manager)
        {
            Children = SerializableChildren;
            Children.RemoveAll(c => c == this);
            SerializableChildren = null;
        }

        public virtual void CreateCosmeticChildren(ComponentManager manager)
        {

        }

        #endregion

        #region Flags

        [Flags]
        public enum Flag
        {
            Visible = 1,
            Active = 2,
            Dead = 4,
            ShouldSerialize = 8
        }

        public bool IsFlagSet(Flag F)
        {
            return (Flags & F) == F;
        }

        public void SetFlag(Flag F, bool Value)
        {
            if (Value)
                Flags |= F;
            else
                Flags &= ~F;
        }

        public void SetFlagRecursive(Flag F, bool Value)
        {
            SetFlag(F, Value);
            foreach (var child in Children)
                child.SetFlagRecursive(F, Value);
        }

        #endregion
        
        // Todo: Get rid of these helpers.
        public bool IsVisible
        {
            get { return IsFlagSet(Flag.Visible); }
            set { SetFlag(Flag.Visible, value); }
        }

        public bool Active
        {
            get { return IsFlagSet(Flag.Active); }
            set { SetFlag(Flag.Active, value); }
        }

        public bool IsDead
        {
            get { return IsFlagSet(Flag.Dead); }
            set { SetFlag(Flag.Dead, value); }
        }
        
        public WorldManager World { get; set; }

        public ComponentManager Manager { get { return World.ComponentManager; } }

        public virtual void ReceiveMessageRecursive(Message messageToReceive)
        {
            foreach(GameComponent child in Children)
            {
                child.ReceiveMessageRecursive(messageToReceive);
            }
        }

        #region Constructors

        public GameComponent()
        {
            World = null;
            Children = new List<GameComponent>();
            Tags = new List<string>();
            Name = "uninitialized";

            SetFlag(Flag.Active, true);
            SetFlag(Flag.Visible, true);
            SetFlag(Flag.Dead, false);
            SetFlag(Flag.ShouldSerialize, true);
        }

        public GameComponent(ComponentManager Manager) : this()
        {
            System.Diagnostics.Debug.Assert(Manager != null, "Manager cannot be null");

            World = Manager.World;
            Parent = null;

            Manager.AddComponent(this);
        }

        public GameComponent(string name, ComponentManager manager) :
            this(manager)
        {
            Name = name;
        }

        #endregion

        public virtual void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera,
            SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            
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

            var localList = new List<GameComponent>(Children);
            foreach (var child in localList)
                child.Die();

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
                toReturn += Name;

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
        /// <param name="self">if set to <c>true</c> add this component to the list if it qualifies.</param>
        /// <returns>The first component of type T.</returns>
        public T GetComponent<T>() where T : GameComponent
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
                System.Diagnostics.Debug.Assert(child.Parent == null, "Child was already added to another component.");

                Children.Add(child);
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

            while(p.Parent != null && !Object.ReferenceEquals(p.Parent, Manager.RootComponent))
                p = p.Parent;

            return p;
        }

        public IEnumerable<GameComponent> EnumerateAll()
        {
            yield return this;
            foreach (var child in Children)
                foreach (var grandChild in child.EnumerateAll())
                    yield return grandChild;
        }

        #endregion
    }
}
