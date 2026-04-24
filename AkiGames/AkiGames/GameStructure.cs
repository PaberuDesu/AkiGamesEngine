using Microsoft.Xna.Framework;
using AkiGames.Core;
using static AkiGames.Events.Input;
using System;

namespace AkiGames
{
    public abstract class GameStructure: IDisposable
    {
        protected GameTime gameTime;
        private bool _startFlag = true;

        public virtual void AkiGamesAwakeTree()
        {
            Game1.UpdateAction += Update;
            Awake();
        }

        public virtual void Awake() { }

        private void Update(GameTime gt)
        {
            if (_startFlag)
            {
                _startFlag = false;
                Start();
            }
            gameTime = gt;
            Update();
        }
        public virtual void Start() { }
        public virtual void Update() { }
        
        public virtual void OnMouseEnter(){}
        public virtual void OnMouseExit(){}
        public virtual void OnMouseDown(){}
        public virtual void OnMouseUp(){}
        public virtual void OnDoubleClick(){}
        public virtual void OnMouseUpOutside(){} // if pressed on something, dragged out of it and stopped pressing
        public virtual void OnRMBUp(){}
        public virtual void Deactivate(){}
        public virtual void Drag(
            Vector2 cursorPosOnObj
        ){}
        public virtual void OnScroll(
            int scrollValue
        ){}
        public virtual void OnScrollFromOutsideTheObject(
            int scrollValue
        ){}
        public virtual void ProcessHotkey(
            HotKey hotkey
        ){}

        public virtual void Dispose()
        {
            Game1.UpdateAction -= Update;
        }
    }
}