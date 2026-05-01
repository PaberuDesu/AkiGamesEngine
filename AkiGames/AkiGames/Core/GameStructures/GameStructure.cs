global using AkiGames.Core.GameStructures;
using System;
using Microsoft.Xna.Framework;
using static AkiGames.Events.Input;
using AkiGames.Scripts;

namespace AkiGames.Core.GameStructures
{
    public abstract class GameStructure: IDisposable
    {
        protected GameTime gameTime;
        private bool _startFlag = true;
        protected virtual ObjectIdSpace CurrentObjectIdSpace => ObjectIdSpace.Main;

        public virtual void AkiGamesAwakeTree()
        {
            using var _ = GameObject.UseObjectIdSpace(CurrentObjectIdSpace);
            Game1.UpdateAction += Update;
            Awake();
        }

        public virtual void Awake() { }

        private void Update(GameTime gt)
        {
            using var _ = GameObject.UseObjectIdSpace(CurrentObjectIdSpace);

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
