using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DwarfCorp
{
    public class ModelInstanceComponent : TintableComponent
    {
        public string ModelType { get; set;}
        public InstanceData Instance { get; set; }
        private bool InstanceVisible = true;
        private bool CheckHeight = false;
        public ModelInstanceComponent(ComponentManager manager, string name, GameComponent parent, Matrix localTransform, string modelType, bool addToOctree) :
            base(manager, name, parent, localTransform, Vector3.Zero, Vector3.Zero, addToOctree)
        {
            ModelType = modelType;
            Instance = PlayState.InstanceManager.AddInstance(ModelType, GlobalTransform, Tint);
            InstanceVisible = true;
            IsStatic = true;
        }

        bool firstIter = true;

        public override void Update(GameTime gameTime, ChunkManager chunks, Camera camera)
        {
            base.Update(gameTime, chunks, camera);

            if (HasMoved || firstIter || Instance.Color != TargetTint)
            {
                Instance.Color = TargetTint;
                Instance.Transform = GlobalTransform;
                firstIter = false;
            }

            if (CheckHeight)
            {
                CheckHeight = false;
            }

            if (IsVisible != InstanceVisible)
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
            if (Instance != null)
            {
                if (value && !InstanceVisible)
                {
                    PlayState.InstanceManager.Instances[ModelType].Add(Instance);
                }
                else if (!value && InstanceVisible)
                {
                    PlayState.InstanceManager.Instances[ModelType].Remove(Instance);
                }
            }

            InstanceVisible = value;
        }
        public override void ReceiveMessageRecursive(Message messageToReceive)
        {
            if (messageToReceive.MessageString == "Chunk Modified")
            {
                CheckHeight = true;
            }
            base.ReceiveMessageRecursive(messageToReceive);
        }



    }
}
