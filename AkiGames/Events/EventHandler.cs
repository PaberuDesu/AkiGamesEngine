using AkiGames.Core;
using static AkiGames.Events.Input;

namespace AkiGames.Events
{
    public class EventHandler : GameComponent
    {
        public event Action? OnMouseEnterEvent;
        public event Action? OnMouseExitEvent;
        public event Action? OnMouseDownEvent;
        public event Action? OnMouseUpEvent;
        public event Action? OnDoubleClickEvent;
        public event Action? OnMouseUpOutsideEvent;
        public event Action? OnRMBUpEvent;
        public event Action? DeactivateEvent;
        public event Action<Vector2>? DragEvent;
        public event Action<int>? OnScrollEvent;
        public event Action<int>? OnScrollFromOutsideTheObjectEvent;
        public event Action<HotKey>? ProcessHotkeyEvent;

        public override void OnMouseEnter() => OnMouseEnterEvent?.Invoke();
        public override void OnMouseExit() => OnMouseExitEvent?.Invoke();
        public override void OnMouseDown() => OnMouseDownEvent?.Invoke();
        public override void OnMouseUp() => OnMouseUpEvent?.Invoke();
        public override void OnDoubleClick() => OnDoubleClickEvent?.Invoke();
        public override void OnMouseUpOutside() => OnMouseUpOutsideEvent?.Invoke();
        public override void OnRMBUp() => OnRMBUpEvent?.Invoke();
        public override void Deactivate() => DeactivateEvent?.Invoke();
        public override void Drag(Vector2 cursorPosOnObj) => DragEvent?.Invoke(cursorPosOnObj);
        public override void OnScroll(int scrollValue) => OnScrollEvent?.Invoke(scrollValue);
        public override void OnScrollFromOutsideTheObject(int scrollValue) => OnScrollFromOutsideTheObjectEvent?.Invoke(scrollValue);
        public override void ProcessHotkey(HotKey hotkey) => ProcessHotkeyEvent?.Invoke(hotkey);
    }
}