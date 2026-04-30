using System.Collections.Concurrent;
using Microsoft.Xna.Framework;
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
        private static readonly ConcurrentQueue<string> _pendingLogs = new();
        
        private ScrollableListController _contentList;

        private static int _lines = 0;
        private const int _maxLines = 70;

        public override void Awake()
        {
            _contentList = ResolveScrollableContent();
            GameObject output = GetOrCreateOutputObject();
            _textComponent = output.GetComponent<Text>();
            base.Awake();
        }

        private GameObject GetOrCreateOutputObject()
        {
            if (_contentList.gameObject.Children.Count > 0)
                return _contentList.gameObject.Children[0];

            GameObject output = new("ConsoleOutput")
            {
                IsMouseTargetable = false
            };

            output.AddComponent(new UITransform
            {
                OffsetMin = new Vector2(5, 5),
                OffsetMax = new Vector2(5, 0),
                Width = 0,
                Height = 0,
                HorizontalAlignment = UITransform.AlignmentH.Stretch,
                VerticalAlignment = UITransform.AlignmentV.Stretch
            });

            output.AddComponent(new Text
            {
                HorizontalWrap = Text.WrapModeH.NewLineControlsHeigth,
                HorizontalAlignment = Text.AlignmentH.Left,
                VerticalAlignment = Text.AlignmentV.Top,
                TextColor = Color.White
            });

            _contentList.gameObject.AddChild(output);
            return output;
        }

        public static void Log(object line)
        {
            _pendingLogs.Enqueue($"{line}\n");
        }

        private static void AppendLog(string newOutput)
        {
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
            while (_pendingLogs.TryDequeue(out string logLine))
            {
                AppendLog(logLine);
            }

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
