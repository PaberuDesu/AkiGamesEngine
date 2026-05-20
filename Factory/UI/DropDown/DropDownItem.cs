namespace AkiGames.UI.DropDown
{
    public class DropDownItem : HoverInteractor
    {
        private DropDown _dropDown;

        public override void Start()
        {
            base.Start();
            _dropDown = gameObject.Parent.Parent.GetComponent<DropDown>();
        }
        public override void OnMouseUp() => _dropDown?.Hide();
    }
}