using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using AkiGames.Core;
using AkiGames.Core.Serialization;
using AkiGames.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.Scripts
{
    public class JeopardyGame : GameComponent
    {
        private const float QuestionDurationSeconds = 60f;
        private const string QuestionsFileName = "questions.json";
        private const string MainMenuScene = "Content/Scenes/MainMenu";
        private const string TeamMenuScene = "Content/Scenes/TeamMenu";
        private const string GameBoardScene = "Content/Scenes/GameBoard";
        private const string QuestionScene = "Content/Scenes/QuestionScreen";
        private const string AwardScene = "Content/Scenes/AwardScreen";

        private readonly List<JeopardyTeam> _teams = new();
        private readonly List<GameObject> _teamRows = new();
        private readonly List<GameObject> _awardTeamButtons = new();
        private readonly List<GameObject> _scoreboardItems = new();
        private readonly List<QuestionCellBinding> _questionCells = new();
        private readonly Dictionary<string, GameObject> _objectsByName = new(StringComparer.Ordinal);

        private readonly LogoOption[] _logos =
        {
            new("Sahur", "LogoSahur", "Content/TeamLogos/Sahur.png", new Color(170, 90, 45)),
            new("Lirili", "LogoLirili", "Content/TeamLogos/Lirili.png", new Color(45, 90, 190)),
            new("Crocodilo", "LogoCrocodilo", "Content/TeamLogos/Crocodilo.png", new Color(45, 150, 80)),
            new("Patapim", "LogoPatapim", "Content/TeamLogos/Patapim.png", new Color(205, 165, 45))
        };

        private JeopardyQuestionSet _questionSet;
        private GameObject _currentScene;
        private GameObject _teamList;
        private GameObject _noTeamsText;
        private GameObject _awardButtonsRoot;
        private GameObject _awardNoOneButton;
        private GameObject _logoPreviewImageObject;
        private GameObject _scoreboardRoot;
        private GameObject _scoreboardEmptyText;
        private Text _questionText;
        private Image _questionImage;
        private UITransform _timerFillTransform;
        private Text _timerText;
        private Text _answerText;
        private JeopardyTextInput _teamNameInput;
        private JeopardyQuestion _currentQuestion;
        private JeopardyTheme _currentTheme;
        private float _remainingQuestionTime;
        private int _selectedLogoIndex;
        private bool _isQuestionActive;

        public override void Awake()
        {
            _questionSet = LoadQuestions();
            OpenMainMenu();
        }

        public override void Update()
        {
            if (!_isQuestionActive) return;

            _remainingQuestionTime -= (float)gameTime.ElapsedGameTime.TotalSeconds;
            if (_remainingQuestionTime <= 0)
            {
                _remainingQuestionTime = 0;
                _isQuestionActive = false;
                OpenAwardScreen();
            }

            UpdateTimerView();
        }

        private void OpenMainMenu()
        {
            if (!ShowScene(MainMenuScene)) return;

            BindButton("StartButton", StartGame);
            BindButton("AddTeamButton", OpenTeamMenu);
            BindButton("ExitButton", Game1.ExitGame);
        }

        private void OpenTeamMenu()
        {
            if (!ShowScene(TeamMenuScene)) return;

            _teamNameInput = RequireComponent<JeopardyTextInput>(FindRequired("TeamNameInput"));
            _logoPreviewImageObject = FindRequired("LogoPreviewImage");
            _teamList = FindRequired("TeamList");
            _noTeamsText = FindRequired("NoTeams");

            BindButton("AddTeamConfirm", AddTeam);
            BindButton("BackToMain", OpenMainMenu);

            for (int i = 0; i < _logos.Length; i++)
            {
                int logoIndex = i;
                BindButton(_logos[i].ButtonObjectName, () => SelectLogo(logoIndex));
            }

            SelectLogo(_selectedLogoIndex);
            RefreshTeamList();
        }

        private void StartGame()
        {
            foreach (JeopardyTheme theme in _questionSet.Themes)
            {
                foreach (JeopardyQuestion question in theme.Questions)
                    question.IsUsed = false;
            }

            foreach (JeopardyTeam team in _teams)
                team.Score = 0;

            OpenBoard();
        }

        private void OpenBoard()
        {
            if (!ShowScene(GameBoardScene)) return;

            _scoreboardRoot = FindRequired("Scoreboard");
            _scoreboardEmptyText = FindRequired("ScoreboardEmpty");
            BindButton("BackToMenu", OpenMainMenu);
            ConfigureBoard();
            UpdateBoard();
            UpdateScores();
        }

        private void ConfigureBoard()
        {
            _questionCells.Clear();

            for (int themeIndex = 0; themeIndex < 6; themeIndex++)
            {
                JeopardyTheme theme = themeIndex < _questionSet.Themes.Count
                    ? _questionSet.Themes[themeIndex]
                    : null;

                GameObject themeObject = FindRequired($"Theme{themeIndex}");
                themeObject.IsActive = theme != null;
                SetText(themeObject, theme?.Title ?? "");

                for (int questionIndex = 0; questionIndex < 5; questionIndex++)
                {
                    GameObject questionObject = FindRequired($"Question{themeIndex}_{questionIndex}");
                    JeopardyQuestion question = theme != null && questionIndex < theme.Questions.Count
                        ? theme.Questions[questionIndex]
                        : null;

                    questionObject.IsActive = question != null;
                    SetText(questionObject, question != null ? question.Points.ToString() : "");

                    if (question == null) continue;

                    JeopardyButton button = RequireComponent<JeopardyButton>(questionObject);
                    JeopardyTheme capturedTheme = theme;
                    JeopardyQuestion capturedQuestion = question;
                    button.Clicked = () => OpenQuestion(capturedTheme, capturedQuestion);

                    _questionCells.Add(new QuestionCellBinding(
                        question,
                        questionObject,
                        RequireComponent<Text>(questionObject),
                        RequireComponent<Image>(questionObject)
                    ));
                }
            }
        }

        private void SelectLogo(int logoIndex)
        {
            _selectedLogoIndex = Math.Clamp(logoIndex, 0, _logos.Length - 1);
            SetObjectImage(
                _logoPreviewImageObject,
                _logos[_selectedLogoIndex].TextureLink,
                _logos[_selectedLogoIndex].FallbackColor
            );
        }

        private void AddTeam()
        {
            string teamName = _teamNameInput?.Value?.Trim() ?? "";
            if (string.IsNullOrWhiteSpace(teamName))
                teamName = $"Team {_teams.Count + 1}";

            LogoOption logo = _logos[_selectedLogoIndex];
            _teams.Add(new JeopardyTeam
            {
                Name = teamName,
                Logo = logo.Name,
                LogoImage = logo.TextureLink,
                Score = 0
            });

            _teamNameInput?.Clear();
            RefreshTeamList();
            UpdateScores();
        }

        private void RefreshTeamList()
        {
            if (_teamList == null || _noTeamsText == null) return;

            RemoveDynamicObjects(_teamRows);
            _noTeamsText.IsActive = _teams.Count == 0;

            for (int i = 0; i < _teams.Count; i++)
            {
                JeopardyTeam team = _teams[i];
                GameObject row = InstantiatePrefab("TeamListRow", _teamList, $"TeamRow{i}", TopLeft(0, i * 54, 470, 44));
                if (row == null) continue;

                SetChildText(row, "TeamText", $"{team.Name}     {team.Score}");
                SetObjectImage(FindChildByName(row, "Logo"), team.LogoImage, GetLogoFallbackColor(team.Logo));
                _teamRows.Add(row);
            }
        }

        private void OpenQuestion(JeopardyTheme theme, JeopardyQuestion question)
        {
            if (question.IsUsed || !ShowScene(QuestionScene)) return;

            _currentTheme = theme;
            _currentQuestion = question;
            _remainingQuestionTime = QuestionDurationSeconds;
            _isQuestionActive = true;

            _questionText = RequireComponent<Text>(FindRequired("QuestionText"));
            _questionImage = RequireComponent<Image>(FindRequired("QuestionImage"));
            _timerFillTransform = FindRequired("TimerFill").uiTransform;
            _timerText = RequireComponent<Text>(FindRequired("Timer"));

            BindButton("EscapeQuestion", ReturnQuestionToBoard);
            BindButton("SkipTimer", OpenAwardScreen);

            SetText(_questionText, question.Text);
            SetImageTexture(_questionImage, question.Image, Color.Black);
            UpdateTimerView();
        }

        private void ReturnQuestionToBoard()
        {
            _isQuestionActive = false;
            _currentQuestion = null;
            _currentTheme = null;
            OpenBoard();
        }

        private void OpenAwardScreen()
        {
            _isQuestionActive = false;
            if (!ShowScene(AwardScene)) return;

            _answerText = RequireComponent<Text>(FindRequired("AnswerText"));
            _awardButtonsRoot = FindRequired("AwardButtons");
            _awardNoOneButton = FindRequired("AwardNoOne");

            BindButton("ShowAnswer", ShowAnswer);
            BindButton("AwardNoOne", () => AwardToTeam(-1));

            SetText(_answerText, "");
            RebuildAwardButtons();
        }

        private void ShowAnswer()
        {
            SetText(_answerText, _currentQuestion?.Answer ?? "");
        }

        private void AwardToTeam(int teamIndex)
        {
            if (_currentQuestion != null)
            {
                if (teamIndex >= 0 && teamIndex < _teams.Count)
                    _teams[teamIndex].Score += _currentQuestion.Points;

                _currentQuestion.IsUsed = true;
            }

            _currentQuestion = null;
            _currentTheme = null;
            OpenBoard();
        }

        private void RebuildAwardButtons()
        {
            if (_awardButtonsRoot == null || _awardNoOneButton == null) return;

            RemoveDynamicObjects(_awardTeamButtons);

            for (int i = 0; i < _teams.Count; i++)
            {
                int teamIndex = i;
                JeopardyTeam team = _teams[i];
                GameObject buttonObject = InstantiatePrefab("AwardTeamButton", _awardButtonsRoot, $"AwardTeam{i}", TopLeft(90, i * 78, 520, 62));
                if (buttonObject == null) continue;

                SetText(buttonObject, team.Name);
                SetObjectImage(FindChildByName(buttonObject, "Logo"), team.LogoImage, GetLogoFallbackColor(team.Logo));
                RequireComponent<JeopardyButton>(buttonObject).Clicked = () => AwardToTeam(teamIndex);
                _awardTeamButtons.Add(buttonObject);
            }

            ApplyTransform(_awardNoOneButton.uiTransform, TopLeft(90, _teams.Count * 78 + 18, 520, 62));
            _awardNoOneButton.RefreshBounds(_awardButtonsRoot.uiTransform);
        }

        private void UpdateBoard()
        {
            foreach (QuestionCellBinding cell in _questionCells)
            {
                cell.ButtonObject.IsMouseTargetable = !cell.Question.IsUsed;
                cell.Image.fillColor = cell.Question.IsUsed ? new Color(26, 26, 32) : new Color(30, 66, 155);
                SetText(cell.Text, cell.Question.IsUsed ? "" : cell.Question.Points.ToString());
            }
        }

        private void UpdateScores()
        {
            if (_scoreboardRoot == null || _scoreboardEmptyText == null) return;

            RemoveDynamicObjects(_scoreboardItems);
            _scoreboardEmptyText.IsActive = _teams.Count == 0;

            if (_teams.Count == 0)
            {
                SetText(_scoreboardEmptyText, "No teams yet");
                return;
            }

            const int itemWidth = 240;
            const int itemHeight = 48;
            const int gap = 18;

            int totalWidth = _teams.Count * itemWidth + Math.Max(0, _teams.Count - 1) * gap;
            float startX = Math.Max(0, (_scoreboardRoot.uiTransform.Bounds.Width - totalWidth) / 2f);

            for (int i = 0; i < _teams.Count; i++)
            {
                JeopardyTeam team = _teams[i];
                GameObject item = InstantiatePrefab(
                    "ScoreboardTeam",
                    _scoreboardRoot,
                    $"ScoreboardTeam{i}",
                    TopLeft(startX + i * (itemWidth + gap), 0, itemWidth, itemHeight)
                );
                if (item == null) continue;

                SetChildText(item, "TeamText", $"{team.Name}: {team.Score}");
                SetObjectImage(FindChildByName(item, "Logo"), team.LogoImage, GetLogoFallbackColor(team.Logo));
                _scoreboardItems.Add(item);
            }
        }

        private void UpdateTimerView()
        {
            if (_timerFillTransform == null || _timerText == null) return;

            float progress = MathHelper.Clamp(_remainingQuestionTime / QuestionDurationSeconds, 0, 1);
            _timerFillTransform.OffsetMax = new Vector2(720 * (1 - progress), 0);
            _timerFillTransform.RefreshBounds();
            SetText(_timerText, $"{Math.Ceiling(_remainingQuestionTime)} seconds");
        }

        private bool ShowScene(string sceneLink)
        {
            ClearCurrentScene();

            _currentScene = LoadLinkedScene(sceneLink);
            if (_currentScene == null)
            {
                Console.WriteLine($"Jeopardy scene '{sceneLink}' could not be loaded.");
                return false;
            }

            gameObject.AddChild(_currentScene);
            _currentScene.RefreshBounds(gameObject.uiTransform);
            BindCurrentSceneObjects();
            return true;
        }

        private void ClearCurrentScene()
        {
            _isQuestionActive = false;
            ResetSceneBindings();

            foreach (GameObject child in gameObject.Children.ToArray())
                child.Dispose();

            _currentScene = null;
        }

        private void ResetSceneBindings()
        {
            _objectsByName.Clear();
            _questionCells.Clear();
            _teamRows.Clear();
            _awardTeamButtons.Clear();
            _scoreboardItems.Clear();
            _teamList = null;
            _noTeamsText = null;
            _awardButtonsRoot = null;
            _awardNoOneButton = null;
            _logoPreviewImageObject = null;
            _scoreboardRoot = null;
            _scoreboardEmptyText = null;
            _questionText = null;
            _questionImage = null;
            _timerFillTransform = null;
            _timerText = null;
            _answerText = null;
            _teamNameInput = null;
        }

        private GameObject LoadLinkedScene(string sceneLink)
        {
            string wrapperJson = $$"""
            {
              "ObjectName": "SceneLinkLoader",
              "Children": [
                { "Link": {{JsonSerializer.Serialize(sceneLink)}} }
              ]
            }
            """;

            using JsonDocument document = JsonDocument.Parse(wrapperJson);
            GameObject wrapper = JsonProjectSerializer.LoadFromJson(document.RootElement);
            GameObject scene = wrapper.Children.FirstOrDefault();
            if (scene == null)
            {
                wrapper.Dispose();
                return null;
            }

            wrapper.RemoveChild(scene);
            wrapper.Dispose();
            return scene;
        }

        private void BindCurrentSceneObjects()
        {
            _objectsByName.Clear();
            if (_currentScene != null)
                IndexObjects(_currentScene);
        }

        private JeopardyQuestionSet LoadQuestions()
        {
            string path = ResolveQuestionsPath();
            if (!File.Exists(path))
                return CreateFallbackQuestionSet();

            string json = File.ReadAllText(path);
            JeopardyQuestionSet questionSet = JsonSerializer.Deserialize<JeopardyQuestionSet>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );

            return questionSet ?? CreateFallbackQuestionSet();
        }

        private static string ResolveQuestionsPath()
        {
            foreach (string path in GetQuestionPathCandidates())
            {
                if (File.Exists(path))
                    return path;
            }

            return "";
        }

        private static IEnumerable<string> GetQuestionPathCandidates()
        {
            foreach (string contentRoot in GetContentRoots())
            {
                if (string.IsNullOrWhiteSpace(contentRoot)) continue;

                yield return Path.Combine(contentRoot, QuestionsFileName);
                yield return Path.Combine(contentRoot, "Content", QuestionsFileName);
            }

            yield return Path.Combine(AppContext.BaseDirectory, QuestionsFileName);
            yield return Path.Combine(AppContext.BaseDirectory, "Content", QuestionsFileName);
            yield return Path.Combine(Directory.GetCurrentDirectory(), QuestionsFileName);
            yield return Path.Combine(Directory.GetCurrentDirectory(), "Content", QuestionsFileName);
        }

        private static IEnumerable<string> GetContentRoots()
        {
            yield return GetGame1StringProperty("GameContentRoot");
            yield return GetGame1StringProperty("ContentRoot");
            yield return GetGame1StringProperty("EditorContentRoot");
        }

        private static string GetGame1StringProperty(string propertyName) =>
            typeof(Game1)
                .GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)?
                .GetValue(null) as string ?? "";

        private static JeopardyQuestionSet CreateFallbackQuestionSet()
        {
            JeopardyQuestionSet fallback = new();
            for (int themeIndex = 0; themeIndex < 6; themeIndex++)
            {
                JeopardyTheme theme = new() { Title = $"Theme {themeIndex + 1}" };
                for (int questionIndex = 0; questionIndex < 5; questionIndex++)
                {
                    int points = (questionIndex + 1) * 100;
                    theme.Questions.Add(new JeopardyQuestion
                    {
                        Points = points,
                        Text = $"Fallback question {themeIndex + 1}-{questionIndex + 1}",
                        Answer = "Fallback answer",
                        Image = "Content/QuestionImages/placeholder"
                    });
                }

                fallback.Themes.Add(theme);
            }

            return fallback;
        }

        private void IndexObjects(GameObject current)
        {
            if (!string.IsNullOrWhiteSpace(current.ObjectName) && !_objectsByName.ContainsKey(current.ObjectName))
                _objectsByName.Add(current.ObjectName, current);

            foreach (GameObject child in current.Children)
                IndexObjects(child);
        }

        private GameObject FindRequired(string objectName)
        {
            if (_objectsByName.TryGetValue(objectName, out GameObject found))
                return found;

            throw new InvalidOperationException($"Jeopardy scene is missing required object '{objectName}'.");
        }

        private static GameObject FindChildByName(GameObject root, string objectName)
        {
            if (root == null) return null;
            if (root.ObjectName == objectName) return root;

            foreach (GameObject child in root.Children)
            {
                GameObject found = FindChildByName(child, objectName);
                if (found != null) return found;
            }

            return null;
        }

        private JeopardyButton BindButton(string objectName, Action action)
        {
            JeopardyButton button = RequireComponent<JeopardyButton>(FindRequired(objectName));
            button.Clicked = action;
            return button;
        }

        private static T RequireComponent<T>(GameObject target) where T : GameComponent
        {
            T component = target?.GetComponent<T>();
            if (component == null)
                throw new InvalidOperationException($"Jeopardy object '{target?.ObjectName ?? "null"}' is missing component {typeof(T).Name}.");

            return component;
        }

        private GameObject InstantiatePrefab(string prefabName, GameObject parent, string objectName, UITransform transform)
        {
            if (!Game1.Prefabs.TryGetValue(prefabName, out GameObject prefab))
            {
                Console.WriteLine($"Jeopardy prefab '{prefabName}' wasn't found.");
                return null;
            }

            GameObject copy = prefab.Copy();
            copy.ObjectName = objectName;
            ApplyTransform(copy.uiTransform, transform);
            parent.AddChild(copy);
            copy.RefreshBounds(parent.uiTransform);
            return copy;
        }

        private static void RemoveDynamicObjects(List<GameObject> objects)
        {
            foreach (GameObject item in objects.ToArray())
                item.Dispose();

            objects.Clear();
        }

        private static UITransform TopLeft(float x, float y, float width, float height) =>
            new()
            {
                OffsetMin = new Vector2(x, y),
                OffsetMax = Vector2.Zero,
                Width = (int)width,
                Height = (int)height,
                HorizontalAlignment = UITransform.AlignmentH.Left,
                VerticalAlignment = UITransform.AlignmentV.Top,
                anchorLeftTop = Vector2.Zero,
                anchorRightBottom = Vector2.Zero,
                origin = Vector2.Zero,
                LocalRotation = 0,
                Enabled = true
            };

        private static void ApplyTransform(UITransform target, UITransform source)
        {
            target.OffsetMin = source.OffsetMin;
            target.OffsetMax = source.OffsetMax;
            target.Width = source.Width;
            target.Height = source.Height;
            target.HorizontalAlignment = source.HorizontalAlignment;
            target.VerticalAlignment = source.VerticalAlignment;
            target.anchorLeftTop = source.anchorLeftTop;
            target.anchorRightBottom = source.anchorRightBottom;
            target.origin = source.origin;
            target.LocalRotation = source.LocalRotation;
            target.Enabled = source.Enabled;
        }

        private static void SetChildText(GameObject root, string childName, string value)
        {
            GameObject child = FindChildByName(root, childName);
            if (child != null)
                SetText(child, value);
        }

        private static void SetText(GameObject target, string value) =>
            SetText(target?.GetComponent<Text>(), value);

        private static void SetText(Text text, string value)
        {
            if (text != null)
                text.text = value ?? "";
        }

        private static void SetObjectImage(GameObject target, string textureLink, Color fallbackColor)
        {
            Image image = target?.GetComponent<Image>();
            SetImageTexture(image, textureLink, fallbackColor);
        }

        private static void SetImageTexture(Image image, string textureLink, Color fallbackColor)
        {
            if (image == null) return;

            Texture2D texture = Game1.LoadGameTexture(textureLink);
            image.texture = texture;
            image.fillColor = texture != null ? Color.White : fallbackColor;
        }

        private Color GetLogoFallbackColor(string logoName) =>
            _logos.FirstOrDefault(logo => logo.Name == logoName)?.FallbackColor ?? Color.White;

        private sealed record LogoOption(string Name, string ButtonObjectName, string TextureLink, Color FallbackColor);

        private sealed record QuestionCellBinding(JeopardyQuestion Question, GameObject ButtonObject, Text Text, Image Image);
    }
}
