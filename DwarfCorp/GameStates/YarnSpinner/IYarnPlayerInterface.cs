using System.Collections.Generic;
using System.Linq;
using DwarfCorp.Gui;
using LibNoise;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Rectangle = Microsoft.Xna.Framework.Rectangle;
using DwarfCorp.GameStates;
using System;

namespace DwarfCorp
{
    public interface IYarnPlayerInterface
    {
        void Output(String S);
        void Speak(String S);
        bool CancelSpeech();
        bool AdvanceSpeech(DwarfTime gameTime); // Returns true if speech must continue.
        void ClearOutput();
        void ClearChoices();
        void AddChoice(String Option, Action Callback);
        void DoneAddingChoices();
        void SetLanguage(Language Language);
        void SetPortrait(String Gfx, int FrameWidth, int FrameHeight, float Speed, List<int> Frames);
        void ShowPortrait();
        void HidePortrait();
        void EndConversation();
        void BeginTrade(TradeEnvoy Envoy, Faction PlayerFaction);
        void BeginMarket(TradeEnvoy Envoy, Faction PlayerFaction);
        void WaitForTrade(Action<Play.Trading.TradeDialogResult, Trade.TradeTransaction> Callback);
        void EndTrade();
        void WaitForMarket(Action<Gui.Widgets.MarketDialogResult, Trade.MarketTransaction> Callback);
        void EndMarket();

    }
}
