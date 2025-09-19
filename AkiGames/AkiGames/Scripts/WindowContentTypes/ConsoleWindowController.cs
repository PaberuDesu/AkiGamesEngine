using AkiGames.Scripts.Window;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class ConsoleWindowController : WindowController
    {
        private Text _textComponent = null;
        private static string _output = "";
        private static bool _logChanged = false;
        
        private ScrollableListController _contentList;

        private static int _lines = 0;
        private const int _maxLines = 50;

        public override void Awake()
        {
            scrollableContent = gameObject.Children[3].Children[0].Children[0];
            _contentList = scrollableContent.GetComponent<ScrollableListController>();
            _textComponent = _contentList.gameObject.Children[0].GetComponent<Text>();
            base.Awake();
        }

        public static void Log(object line)
        {
            string newOutput = $"{line}\n";
            _output += newOutput;
            _logChanged = true;

            _lines += CountLineBreaks(newOutput);
            if (_lines > _maxLines)
            {
                _output = RemoveFirstLines(_output, _lines - _maxLines);
                _lines = _maxLines;
            }
        }

        private static int CountLineBreaks(string input)
        {
            int count = 0;
            for (int i = 0; i < input.Length; i++)
            {
                if (input[i] == '\n')
                {
                    count++;
                }
                else if (input[i] == '\r')
                {
                    count++;
                    if (i + 1 < input.Length && input[i + 1] == '\n')
                        i++; // Skip following \n
                }
            }
            return count;
        }

        private static string RemoveFirstLines(string text, int linesToRemove)
        {
            if (linesToRemove <= 0) return text;

            int index = 0;
            for (int i = 0; i < linesToRemove; i++)
            {
                int newLinePos = text.IndexOf('\n', index);
                if (newLinePos == -1)
                {
                    // Not enough lines - return empty string
                    return string.Empty;
                }
                index = newLinePos + 1;
            }
            return text[index..];
        }

        public override void Update()
        {
            if (_logChanged)
            {
                bool isListScrolledToBottom = _contentList.IsLimitReached;
                _textComponent.text = _output;
                _logChanged = false;
                if (isListScrolledToBottom) _contentList.ScrollToBottom();
            }
        }
    }
}