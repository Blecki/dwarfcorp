﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Concurrent;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class GameComponent
    {
        public string Name { get; set; }

        public uint GlobalID { get; set; }

        public uint LocalID { get; set; }

        public GameComponent Parent { get; set; }

        public ConcurrentDictionary<uint, GameComponent> Children { get; set; }

        private static uint m_maxGlobalID = 0;
        private uint m_maxLocalID = 0;

        public bool IsVisible { get; set; }
        public bool IsActive { get; set; }
        public bool IsDead { get; set; }

        public List<string> Tags { get; set; }

        public ComponentManager Manager { get; set; }

        public virtual void ReceiveMessageRecursive(Message messageToReceive)
        {
            foreach(GameComponent child in Children.Values)
            {
                child.ReceiveMessageRecursive(messageToReceive);
            }
        }

        public GameComponent()
        {
            Children = new ConcurrentDictionary<uint, GameComponent>();
            Name = "uninitialized";
            IsVisible = true;
            IsActive = true;
            Tags = new List<string>();
        }

        public GameComponent(ComponentManager manager)
        {
            GlobalID = m_maxGlobalID;
            m_maxGlobalID++;

            LocalID = 0;

            Parent = null;
            Children = new ConcurrentDictionary<uint, GameComponent>();

            Name = "uninitialized";
            IsVisible = true;
            IsActive = true;
            Manager = manager;

            IsDead = false;

            manager.AddComponent(this);

            Tags = new List<string>();
        }

        public GameComponent(ComponentManager manager, string name, GameComponent parent) :
            this(manager)
        {
            Name = name;
            RemoveFromParent();

            if(parent != null)
            {
                parent.AddChild(this);
            }
            else
            {
                Parent = null;
            }
        }


        public uint GetNextLocalID()
        {
            return m_maxLocalID++;
        }


        public List<GameComponent> GetAllChildrenRecursive()
        {
            List<GameComponent> toReturn = new List<GameComponent>();

            toReturn.AddRange(Children.Values);

            foreach(GameComponent child in Children.Values)
            {
                toReturn.AddRange(child.GetAllChildrenRecursive());
            }

            return toReturn;
        }


        public virtual void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
        }


        public virtual void Render(GameTime gameTime, ChunkManager chunks, Camera camera, SpriteBatch spriteBatch, GraphicsDevice graphicsDevice, Effect effect, bool renderingForWater)
        {
        }

        public override int GetHashCode()
        {
            return (int) GlobalID;
        }

        public void SetActiveRecursive(bool active)
        {
            IsActive = active;

            foreach(GameComponent child in Children.Values)
            {
                child.SetActiveRecursive(active);
            }
        }

        public void SetVisibleRecursive(bool visible)
        {
            IsVisible = visible;

            foreach(GameComponent child in Children.Values)
            {
                child.SetVisibleRecursive(visible);
            }
        }

        public virtual void Die()
        {
            IsDead = true;

            foreach(GameComponent child in Children.Values)
            {
                child.Die();
            }
        }

        #region child_operators

        public bool HasChildWithType<T>() where T : GameComponent
        {
            return Children.Values.OfType<T>().Any();
        }

        public List<T> GetChildrenOfType<T>() where T : GameComponent
        {
            return (from child in Children
                where child.Value is T
                select (T) child.Value).ToList();
        }

        public bool HasChildWithName(string name)
        {
            return Children.Values.Any(child => child.Name == name);
        }

        public List<GameComponent> GetChildrenWithName(string name)
        {
            return Children.Values.Where(child => child.Name == name).ToList();
        }

        public bool HasChildWithGlobalID(uint id)
        {
            return Children.Values.Any(child => child.GlobalID == id);
        }

        public GameComponent GetChildWithGlobalID(uint id)
        {
            return Children.Values.FirstOrDefault(child => child.GlobalID == id);
        }

        public bool HasChildWithLocalID(uint id)
        {
            return Children.ContainsKey(id);
        }

        public GameComponent GetChildByLocalID(uint id)
        {
            return HasChildWithLocalID(id) ? Children[id] : null;
        }


        public void RemoveFromParent()
        {
            if(Parent != null)
            {
                Parent.RemoveChild(this);
            }
        }

        public void AddChild(GameComponent child)
        {
            if(HasChildWithGlobalID(child.GlobalID))
            {
                return;
            }

            child.LocalID = GetNextLocalID();
            Children[child.LocalID] = child;
            child.Parent = this;
        }

        public void RemoveChild(GameComponent child)
        {
            if(!HasChildWithGlobalID(child.GlobalID))
            {
                return;
            }

            GameComponent removed = null;

            Children.TryRemove(child.LocalID, out removed);
        }

        #endregion

        #region recursive_child_operators

        public bool HasChildWithNameRecursive(string name)
        {
            foreach(GameComponent child in Children.Values)
            {
                if(child.Name == name || child.HasChildWithNameRecursive(name))
                {
                    return true;
                }
            }

            return false;
        }

        public GameComponent GetRootComponent()
        {
            GameComponent p = this;

            while(p != null && p.Parent != Manager.RootComponent)
            {
                p = p.Parent;
            }

            return p;
        }

        public List<GameComponent> GetChildrenWithNameRecursive(string name)
        {
            List<GameComponent> toReturn = new List<GameComponent>();
            foreach(GameComponent child in Children.Values)
            {
                if(child.Name == name)
                {
                    toReturn.Add(child);
                }

                List<GameComponent> childList = child.GetChildrenWithNameRecursive(name);
                toReturn.AddRange(childList);
            }

            return toReturn;
        }

        public bool HasChildWithGlobalIDRecursive(uint id)
        {
            foreach(GameComponent child in Children.Values)
            {
                if(child.GlobalID == id || child.HasChildWithGlobalIDRecursive(id))
                {
                    return true;
                }
            }

            return false;
        }

        public GameComponent GetChildWithGlobalIDRecursive(uint id)
        {
            foreach(GameComponent child in Children.Values)
            {
                if(child.GlobalID == id)
                {
                    return child;
                }

                GameComponent grandChild = child.GetChildWithGlobalIDRecursive(id);

                if(grandChild != null)
                {
                    return grandChild;
                }
            }

            return null;
        }


        public bool HasChildWithTypeRecursive<T>() where T : GameComponent
        {
            foreach(GameComponent child in Children.Values)
            {
                if(child is T || child.HasChildWithTypeRecursive<T>())
                {
                    return true;
                }
            }

            return false;
        }

        public List<T> GetChildrenOfTypeRecursive<T>() where T : GameComponent
        {
            List<T> toReturn = new List<T>();

            foreach(KeyValuePair<uint, GameComponent> child in Children)
            {
                if(child.Value is T)
                {
                    toReturn.Add((T) child.Value);
                }

                toReturn.AddRange(child.Value.GetChildrenOfType<T>());
            }

            return toReturn;
        }

        #endregion
    }

}