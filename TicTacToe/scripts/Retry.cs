namespace AkiGames.Scripts
{
    public class Retry : GameComponent
    {
        private static FieldController fieldController = null;

        public override void Awake() =>
            fieldController = gameObject.Parent.Children[0].GetComponent<FieldController>();

        public override void OnMouseUp() => fieldController.Restart();
    }
}