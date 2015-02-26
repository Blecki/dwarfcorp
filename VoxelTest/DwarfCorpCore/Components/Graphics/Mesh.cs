using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    /// <summary>
    /// This component represents an instance of a particular primitive (such as intersecting billboards, or a box), or a mesh. 
    /// Efficiently drawn by the instance manager using state batching.
    /// </summary>
    [JsonObject(IsReference = true)]
    public class Mesh : Tinter
    {
        public string ModelType { get; set; }
        [JsonIgnore]
        public InstanceData Instance { get; set; }
        private bool instanceVisible = true;
        private bool checkHeight = false;

        [OnDeserialized]
        protected void OnDeserialized(StreamingContext context)
        {
            Instance = PlayState.InstanceManager.AddInstance(ModelType, GlobalTransform, Tint);
            instanceVisible = true;
        }

        public Mesh()
        {
            
        }

        public Mesh(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, string modelType, bool addToOctree) :
            base(name, parent, localTransform, Vector3.Zero, Vector3.Zero, addToOctree)
        {
            ModelType = modelType;
            Instance = PlayState.InstanceManager.AddInstance(ModelType, GlobalTransform, Tint);
            instanceVisible = true;
        }

        private bool firstIter = true;

        public override void Update(DwarfTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if(Instance != null  && (HasMoved || firstIter || Instance.Color != TargetTint))
            {
                Instance.Color = TargetTint;
                Instance.Transform = GlobalTransform;
                firstIter = false;
            }

            if(checkHeight)
            {
                checkHeight = false;
            }

            if(IsVisible != instanceVisible)
            {
                SetVisible(IsVisible);
            }
        }

        public override void Die()
        {
            PlayState.InstanceManager.Instances[ModelType].Remove(Instance);
            base.Die();
        }

        public void SetVisible(bool value)
        {
            if(Instance != null)
            {
                if(value && !instanceVisible)
                {
                    PlayState.InstanceManager.Instances[ModelType].Add(Instance);
                }
                else if(!value && instanceVisible)
                {
                    PlayState.InstanceManager.Instances[ModelType].Remove(Instance);
                }
            }

            instanceVisible = value;
        }

        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if(messageToReceive.MessageString == "Chunk Modified")
            {
                checkHeight = true;
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }
    }

}