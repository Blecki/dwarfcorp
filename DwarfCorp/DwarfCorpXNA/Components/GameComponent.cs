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

    /// <summary>
    /// This class is responsible for handling components. "Components" are one of the most important parts of the 
    /// DwarfCorp engine. Everything in the game is a collection of components. A collection of components is called an "entity".
    /// Components live in a tree-like structure, they have parents and children. Most components (called Locatable components)
    /// also have a position and orientation.
    /// 
    /// By adding and removing components to an entity, functionality can be changed.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class GameComponent
    {
        /// <summary>
        /// Gets or sets the name (used mostly for debugging purposes).
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the global identifier (all GameComponents have a global ID).
        /// </summary>
        /// <value>
        /// The global identifier.
        /// </value>
        public uint GlobalID { get; set; }

        /// <summary>
        /// Gets or sets the parent. If null, this is the root component.
        /// </summary>
        public GameComponent Parent { get; set; }

        /// <summary>
        /// List of GameComponent children attached to this one.
        /// </summary>
        [JsonIgnore]
        public List<GameComponent> Children { get; set; }

        #region Serialization
        public List<GameComponent> SerializableChildren;

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


        [Flags]
        public enum Flag
        {
            Visible = 1,
            Active = 2,
            Dead = 4,
            ShouldSerialize = 8
        }

        public Flag Flags = 0;

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
            set { SetFlag(Flag.Dead, value); }
        }
        
        /// <summary>
        /// Gets or sets the tags. Tags are just arbitrary strings attached to objects.
        /// </summary>
        /// <value>
        /// The tags.
        /// </value>
        public List<string> Tags { get; set; }

        
        /// <summary>
        /// Gets or sets the world.
        /// </summary>
        [JsonIgnore]
        public WorldManager World { get; set; }

        /// <summary>
        /// Gets the component manager (from world)
        /// </summary>
        [JsonIgnore]
        public ComponentManager Manager { get { return World.ComponentManager; } }

        public virtual void ReceiveMessageRecursive(Message messageToReceive)
        {
            foreach(GameComponent child in Children)
            {
                child.ReceiveMessageRecursive(messageToReceive);
            }
        }

        [OnDeserialized]
        void OnDeserializing(StreamingContext context)
        {
            // Assume the context passed in is a WorldManager
            World = ((WorldManager) context.Context);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class.
        /// </summary>
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


        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class, while adding it to the component manager.
        /// </summary>
        /// <param name="manager">The component manager to add the component to</param>
        public GameComponent(ComponentManager Manager) : this()
        {
            System.Diagnostics.Debug.Assert(Manager != null, "Manager cannot be null");

            World = Manager.World;
            Parent = null;

            Manager.AddComponent(this);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GameComponent"/> class.
        /// </summary>
        /// <param name="name">The name of the component.</param>
        /// <param name="parent">The parent component.</param>
        /// <param name="manager">The component manager.</param>
        public GameComponent(string name, ComponentManager manager) :
            this(manager)
        {
            Name = name;
        }

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
        /// Renders the component to the selection buffer (for selecting stuff on screen).
        /// </summary>
        /// <param name="gameTime">The game time.</param>
        /// <param name="chunks">The chunks.</param>
        /// <param name="camera">The camera.</param>
        /// <param name="spriteBatch">The sprite batch.</param>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="effect">The shader to use.</param>
        public virtual void RenderSelectionBuffer(DwarfTime gameTime, ChunkManager chunks, Camera camera,
            SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Shader effect)
        {
            
        }

        // Todo: Lift to utility class
        /// <summary>
        /// Converts the global identifier into a color for rendering to 
        /// the selection buffer.
        /// </summary>
        /// <returns></returns>
        public Color GetGlobalIDColor()
        {
            // 0xFFFFFFFF
            // 0xRRGGBBAA
            int r = (int)(GlobalID >> 24);
            int g = (int)((GlobalID >> 16) & 0x000000FF);
            int b = (int)((GlobalID >> 8) & 0x000000FF);
            int a = (int)((GlobalID) & 0x000000FF);
            //return new Color {PackedValue = GlobalID};
            return new Color(r, g, b, a);
        }

        /// <summary>
        /// Converts a packed color representation of a global ID to a global ID.
        /// </summary>
        /// <param name="color">The color (packed representaiton)</param>
        /// <returns></returns>
        public static uint GlobalIDFromColor(Color color)
        {
            // 0xFFFFFFFF
            // 0xRRGGBBAA
            //return color.PackedValue;
            uint id = 0;
            id = id | (uint) (color.R << 24);
            id = id | (uint) (color.G << 16);
            id = id | (uint) (color.B << 8);
            id = id | (uint) (color.A);
            return id;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
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

            if(Parent == Manager.RootComponent)
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

        #region child_operators

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

        #endregion

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
    }
}
