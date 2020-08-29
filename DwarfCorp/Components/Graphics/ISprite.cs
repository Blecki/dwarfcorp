using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using DwarfCorp.GameStates;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Newtonsoft.Json;

namespace DwarfCorp
{
    public interface ISprite
    {
        bool HasAnimation(CharacterMode Mode, SpriteOrientation Orientation);
        void SetCurrentAnimation(string name, bool Play);
        void Blink(float blinkTime);
        void ResetAnimations(CharacterMode mode);
        void ReloopAnimations(CharacterMode mode);
        void PauseAnimations();
        void PlayAnimations();
        int GetCurrentFrame();
        bool HasValidAnimation();
        bool IsDone();
        void SetDrawSilhouette(bool DrawSilhouette);
    }
}
