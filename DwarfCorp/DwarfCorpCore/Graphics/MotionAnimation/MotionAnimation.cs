using Microsoft.Xna.Framework;
using Newtonsoft.Json;

namespace DwarfCorp
{
    [JsonObject(IsReference = true)]
    public class MotionAnimation
    {
        public delegate void Completed();

        public MotionAnimation(float time, bool loop)
        {
            Time = new Timer(time, !loop);
            OnComplete += MotionAnimation_OnComplete;
        }

        public Timer Time { get; set; }
        public event Completed OnComplete;

        private void MotionAnimation_OnComplete()
        {
        }

        public virtual bool IsDone()
        {
            if (Time.TriggerOnce && Time.HasTriggered)
            {
                return true;
            }
            return false;
        }

        public virtual Matrix GetTransform()
        {
            return Matrix.Identity;
        }

        public virtual void Update(DwarfTime t)
        {
            Time.Update(t);

            if (IsDone())
            {
                OnComplete.Invoke();
            }
        }
    }
}