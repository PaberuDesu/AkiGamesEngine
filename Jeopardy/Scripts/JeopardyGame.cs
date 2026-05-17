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
        private const int MaxTeams = 4;
        private const float QuestionDurationSeconds = 60f;
        private const string QuestionsFileName = "questions.json";
        private const string QuestionsRound2FileName = "questions_1.json";
        private const string QuestionsRound3FileName = "questions_2.json";
        private const string MainMenuScene = "Content/Scenes/MainMenu";
        private const string TeamMenuScene = "Content/Scenes/TeamMenu";
        private const string GameBoardScene = "Content/Scenes/GameBoard";
        private const string QuestionScene = "Content/Scenes/QuestionScreen";
        private const string AwardScene = "Content/Scenes/AwardScreen";
        private const string AwardOptionsScene = "Content/Scenes/AwardOptionsScreen";
        private const string WinnerScene = "Content/Scenes/WinnerScreen";

        private readonly List<JeopardyTeam> _teams = new();
        private readonly List<GameObject> _teamRows = new();
        private readonly List<GameObject> _awardTeamButtons = new();
        private readonly List<GameObject> _scoreboardItems = new();
        private readonly List<QuestionCellBinding> _questionCells = new();
        private readonly List<GameObject> _optionObjects = new();
        private readonly Dictionary<string, GameObject> _objectsByName = new(StringComparer.Ordinal);
        private readonly OptionVisualState[] _awardOptionStates = new OptionVisualState[4];

        private readonly LogoOption[] _logos =
        {
            new("Sahur", "LogoSahur", "Content/TeamLogos/Sahur.png", new Color(170, 90, 45)),
            new("Lirili", "LogoLirili", "Content/TeamLogos/Lirili.png", new Color(45, 90, 190)),
            new("Crocodilo", "LogoCrocodilo", "Content/TeamLogos/Crocodilo.png", new Color(45, 150, 80)),
            new("Patapim", "LogoPatapim", "Content/TeamLogos/Patapim.png", new Color(205, 165, 45))
        };

        private readonly List<JeopardyQuestionSet> _questionRounds = new();
        private GameObject _currentScene;
        private GameObject _teamList;
        private GameObject _noTeamsText;
        private GameObject _addTeamConfirmButton;
        private GameObject _awardButtonsRoot;
        private GameObject _awardNoOneButton;
        private GameObject _awardTitleObject;
        private GameObject _logoPreviewImageObject;
        private GameObject _scoreboardRoot;
        private GameObject _scoreboardEmptyText;
        private GameObject _winnerLogoObject;
        private GameObject _winnerScoresRoot;
        private Text _questionText;
        private Image _questionImage;
        private UITransform _timerFillTransform;
        private Text _timerText;
        private Text _answerText;
        private Text _awardTitleText;
        private Text _winnerTitleText;
        private Text _winnerSummaryText;
        private JeopardyTextInput _teamNameInput;
        private JeopardyQuestion _currentQuestion;
        private JeopardyTheme _currentTheme;
        private float _remainingQuestionTime;
        private int _selectedLogoIndex;
        private int _currentRoundIndex;
        private int _pendingOptionSelectionIndex = -1;
        private bool _isQuestionActive;
        private bool _isMultipleChoiceQuestion;
        private bool _isMultipleChoiceAnswerConfirmed;

        public override void Awake()
        {
            _questionRounds.AddRange(LoadQuestions());
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
                BeginAnswerPhase();
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
            _addTeamConfirmButton = FindRequired("AddTeamConfirm");

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
            foreach (JeopardyQuestionSet round in _questionRounds)
            {
                foreach (JeopardyTheme theme in round.Themes)
                {
                    foreach (JeopardyQuestion question in theme.Questions)
                        question.IsUsed = false;
                }
            }

            foreach (JeopardyTeam team in _teams)
                team.Score = 0;

            _currentRoundIndex = 0;
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
                JeopardyQuestionSet currentRound = GetCurrentRound();
                JeopardyTheme theme = currentRound != null && themeIndex < currentRound.Themes.Count
                    ? currentRound.Themes[themeIndex]
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
            if (_teams.Count >= MaxTeams)
            {
                RefreshTeamList();
                return;
            }

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
            if (_addTeamConfirmButton != null)
                _addTeamConfirmButton.IsMouseTargetable = _teams.Count < MaxTeams;

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
            _pendingOptionSelectionIndex = -1;
            _isMultipleChoiceQuestion = question.Options?.Count == 4;
            _isMultipleChoiceAnswerConfirmed = false;
            ResetAwardOptionStates();

            _questionText = RequireComponent<Text>(FindRequired("QuestionText"));
            _questionImage = RequireComponent<Image>(FindRequired("QuestionImage"));
            _timerFillTransform = FindRequired("TimerFill").uiTransform;
            _timerText = RequireComponent<Text>(FindRequired("Timer"));
            ConfigureQuestionOptions();

            BindButton("EscapeQuestion", ReturnQuestionToBoard);
            GameObject skipButton = FindRequired("SkipTimer");
            skipButton.IsActive = !_isMultipleChoiceQuestion;
            if (!_isMultipleChoiceQuestion)
                RequireComponent<JeopardyButton>(skipButton).Clicked = BeginAnswerPhase;

            SetText(_questionText, question.Text);
            SetImageTexture(_questionImage, question.Image, Color.Black);
            UpdateTimerView();
        }

        private void ReturnQuestionToBoard()
        {
            _isQuestionActive = false;
            _currentQuestion = null;
            _currentTheme = null;
            _pendingOptionSelectionIndex = -1;
            _isMultipleChoiceQuestion = false;
            _isMultipleChoiceAnswerConfirmed = false;
            ResetAwardOptionStates();
            OpenBoard();
        }

        private void OpenAwardScreen()
        {
            _isQuestionActive = false;
            bool isMultipleChoiceQuestion = _currentQuestion?.Options?.Count == 4;
            string sceneLink = isMultipleChoiceQuestion ? AwardOptionsScene : AwardScene;
            if (!ShowScene(sceneLink)) return;
            _isMultipleChoiceQuestion = isMultipleChoiceQuestion;

            _awardTitleObject = FindRequired("AwardTitle");
            _awardTitleText = RequireComponent<Text>(_awardTitleObject);
            _answerText = RequireComponent<Text>(FindRequired("AnswerText"));
            _awardButtonsRoot = FindRequired("AwardButtons");
            _awardNoOneButton = FindRequired("AwardNoOne");

            BindButton("AwardNoOne", () => AwardToTeam(-1));

            if (isMultipleChoiceQuestion)
            {
                SetText(_awardTitleText, "Выбор победителя");
                SetText(_awardNoOneButton, "Никто");
                SetText(_answerText, "Нажмите правильный вариант ответа");
                ConfigureAwardOptions();
            }
            else
            {
                SetText(_awardTitleText, "Выбор победителя");
                SetText(_awardNoOneButton, "Никто");
                BindButton("ShowAnswer", ShowAnswer);
                SetText(_answerText, "");
            }

            RebuildAwardButtons();
            if (isMultipleChoiceQuestion)
                SetAwardSelectionEnabled(_isMultipleChoiceAnswerConfirmed);

            if (isMultipleChoiceQuestion && _pendingOptionSelectionIndex >= 0)
            {
                PrimeAwardOptionsFromPendingSelection();
                _pendingOptionSelectionIndex = -1;
            }
        }

        private void OpenWinnerScene()
        {
            if (!ShowScene(WinnerScene)) return;

            _winnerTitleText = RequireComponent<Text>(FindRequired("WinnerTitle"));
            _winnerSummaryText = RequireComponent<Text>(FindRequired("WinnerSummary"));
            _winnerLogoObject = FindRequired("WinnerLogo");
            _winnerScoresRoot = FindRequired("WinnerScores");

            BindButton("WinnerBackToMenu", OpenMainMenu);
            PopulateWinnerScene();
        }

        private void ShowAnswer()
        {
            SetText(_answerText, _currentQuestion?.Answer ?? "");
        }

        private void BeginAnswerPhase()
        {
            _isQuestionActive = false;
            OpenAwardScreen();
        }

        private void BeginAnswerPhase(int selectedOptionIndex)
        {
            _pendingOptionSelectionIndex = selectedOptionIndex;
            BeginAnswerPhase();
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
            _pendingOptionSelectionIndex = -1;
            _isMultipleChoiceQuestion = false;
            _isMultipleChoiceAnswerConfirmed = false;
            ResetAwardOptionStates();

            if (IsCurrentRoundComplete())
            {
                AdvanceToNextRoundOrFinish();
                return;
            }

            OpenBoard();
        }

        private void RebuildAwardButtons()
        {
            if (_awardButtonsRoot == null || _awardNoOneButton == null) return;

            RemoveDynamicObjects(_awardTeamButtons);
            const int buttonHeight = 62;
            const int gap = 16;
            int totalButtonCount = _teams.Count + 1;
            float contentHeight = totalButtonCount * buttonHeight + Math.Max(0, totalButtonCount - 1) * gap;
            float startY = Math.Max(18, (_awardButtonsRoot.uiTransform.Height - contentHeight) / 2f);

            for (int i = 0; i < _teams.Count; i++)
            {
                int teamIndex = i;
                JeopardyTeam team = _teams[i];
                GameObject buttonObject = InstantiatePrefab("AwardTeamButton", _awardButtonsRoot, $"AwardTeam{i}", TopLeft(90, startY + i * (buttonHeight + gap), 520, buttonHeight));
                if (buttonObject == null) continue;

                SetText(buttonObject, team.Name);
                SetObjectImage(FindChildByName(buttonObject, "Logo"), team.LogoImage, GetLogoFallbackColor(team.Logo));
                RequireComponent<JeopardyButton>(buttonObject).Clicked = () => AwardToTeam(teamIndex);
                _awardTeamButtons.Add(buttonObject);
            }

            ApplyTransform(_awardNoOneButton.uiTransform, TopLeft(90, startY + _teams.Count * (buttonHeight + gap), 520, buttonHeight));
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
            _addTeamConfirmButton = null;
            _awardButtonsRoot = null;
            _awardNoOneButton = null;
            _awardTitleObject = null;
            _logoPreviewImageObject = null;
            _scoreboardRoot = null;
            _scoreboardEmptyText = null;
            _winnerLogoObject = null;
            _winnerScoresRoot = null;
            _questionText = null;
            _questionImage = null;
            _timerFillTransform = null;
            _timerText = null;
            _answerText = null;
            _awardTitleText = null;
            _winnerTitleText = null;
            _winnerSummaryText = null;
            _teamNameInput = null;
            _optionObjects.Clear();
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

        private List<JeopardyQuestionSet> LoadQuestions()
        {
            List<JeopardyQuestionSet> rounds = [];

            JeopardyQuestionSet firstRound = LoadQuestionRound(QuestionsFileName);
            if (firstRound != null)
                rounds.Add(firstRound);

            JeopardyQuestionSet secondRound = LoadQuestionRound(QuestionsRound2FileName);
            if (secondRound != null)
                rounds.Add(secondRound);

            if (secondRound != null)
            {
                JeopardyQuestionSet thirdRound = LoadQuestionRound(QuestionsRound3FileName);
                if (thirdRound != null)
                    rounds.Add(thirdRound);
            }

            if (rounds.Count == 0)
                rounds.Add(CreateFallbackQuestionSet());

            return rounds;
        }

        private static JeopardyQuestionSet LoadQuestionRound(string fileName)
        {
            string path = ResolveQuestionsPath(fileName);
            if (!File.Exists(path))
                return null;

            string json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<JeopardyQuestionSet>(
                json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            );
        }

        private static string ResolveQuestionsPath(string fileName)
        {
            foreach (string path in GetQuestionPathCandidates(fileName))
            {
                if (File.Exists(path))
                    return path;
            }

            return "";
        }

        private static IEnumerable<string> GetQuestionPathCandidates(string fileName)
        {
            foreach (string contentRoot in GetContentRoots())
            {
                if (string.IsNullOrWhiteSpace(contentRoot)) continue;

                yield return Path.Combine(contentRoot, fileName);
                yield return Path.Combine(contentRoot, "Content", fileName);
            }

            yield return Path.Combine(AppContext.BaseDirectory, fileName);
            yield return Path.Combine(AppContext.BaseDirectory, "Content", fileName);
            yield return Path.Combine(Directory.GetCurrentDirectory(), fileName);
            yield return Path.Combine(Directory.GetCurrentDirectory(), "Content", fileName);
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

        private JeopardyQuestionSet GetCurrentRound() =>
            _currentRoundIndex >= 0 && _currentRoundIndex < _questionRounds.Count ?
                _questionRounds[_currentRoundIndex] :
                null;

        private bool IsCurrentRoundComplete()
        {
            JeopardyQuestionSet currentRound = GetCurrentRound();
            if (currentRound == null) return true;

            foreach (JeopardyTheme theme in currentRound.Themes)
            {
                foreach (JeopardyQuestion question in theme.Questions)
                {
                    if (!question.IsUsed)
                        return false;
                }
            }

            return true;
        }

        private void AdvanceToNextRoundOrFinish()
        {
            if (_currentRoundIndex + 1 < _questionRounds.Count)
            {
                _currentRoundIndex++;
                OpenBoard();
                return;
            }

            OpenWinnerScene();
        }

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

        private void ConfigureQuestionOptions()
        {
            _optionObjects.Clear();

            for (int i = 0; i < 4; i++)
            {
                GameObject optionObject = FindChildByName(_currentScene, $"Option{i}");
                if (optionObject == null) continue;

                _optionObjects.Add(optionObject);
                optionObject.IsActive = _isMultipleChoiceQuestion;
                optionObject.IsMouseTargetable = _isMultipleChoiceQuestion;

                JeopardyButton button = optionObject.GetComponent<JeopardyButton>();
                if (button != null)
                {
                    if (_isMultipleChoiceQuestion)
                    {
                        int optionIndex = i;
                        button.Clicked = () => BeginAnswerPhase(optionIndex);
                    }
                    else
                    {
                        button.Clicked = null;
                    }
                }

                Image optionImage = optionObject.GetComponent<Image>();
                if (optionImage != null)
                    optionImage.fillColor = new Color(30, 66, 155);

                Text optionText = optionObject.GetComponent<Text>();
                if (optionText != null)
                {
                    string value = _isMultipleChoiceQuestion ? _currentQuestion.Options[i] : "";
                    optionText.text = value;
                }
            }
        }

        private void ConfigureAwardOptions()
        {
            _optionObjects.Clear();

            for (int i = 0; i < 4; i++)
            {
                GameObject optionObject = FindRequired($"Option{i}");
                SetText(optionObject, _currentQuestion?.Options?[i] ?? "");
                optionObject.IsActive = true;
                optionObject.IsMouseTargetable = true;

                JeopardyButton button = optionObject.GetComponent<JeopardyButton>();
                if (button != null)
                {
                    int optionIndex = i;
                    button.Clicked = () => HandleAwardOptionClicked(optionIndex);
                }

                ApplyOptionVisualState(optionObject, _awardOptionStates[i]);
                _optionObjects.Add(optionObject);
            }
        }

        private void HandleAwardOptionClicked(int selectedIndex)
        {
            int correctIndex = GetCorrectOptionIndex();
            if (selectedIndex < 0 || selectedIndex >= _optionObjects.Count)
                return;

            GameObject optionObject = _optionObjects[selectedIndex];
            if (selectedIndex == correctIndex)
            {
                _awardOptionStates[selectedIndex] = OptionVisualState.Correct;
                ApplyOptionVisualState(optionObject, _awardOptionStates[selectedIndex]);
                _isMultipleChoiceAnswerConfirmed = true;
                SetText(_answerText, BuildMultipleChoiceAnswerText());
                SetAwardSelectionEnabled(true);
            }
            else
            {
                _awardOptionStates[selectedIndex] = OptionVisualState.Wrong;
                ApplyOptionVisualState(optionObject, _awardOptionStates[selectedIndex]);
            }
        }

        private int GetCorrectOptionIndex()
        {
            if (_currentQuestion?.Options == null)
                return -1;

            string answer = _currentQuestion.Answer?.Trim() ?? "";
            for (int i = 0; i < _currentQuestion.Options.Count; i++)
            {
                if (string.Equals(_currentQuestion.Options[i]?.Trim(), answer, StringComparison.OrdinalIgnoreCase))
                    return i;
            }

            return -1;
        }

        private string BuildMultipleChoiceAnswerText()
        {
            int correctIndex = GetCorrectOptionIndex();
            if (correctIndex >= 0 && correctIndex < _currentQuestion.Options.Count)
                return $"Правильный ответ: {_currentQuestion.Options[correctIndex]}";

            return $"Правильный ответ: {_currentQuestion?.Answer ?? ""}";
        }

        private void SetAwardSelectionEnabled(bool isEnabled)
        {
            if (_awardTitleObject != null)
                _awardTitleObject.IsActive = isEnabled;

            if (_awardButtonsRoot != null)
                _awardButtonsRoot.IsActive = isEnabled;

            if (_awardNoOneButton != null)
                _awardNoOneButton.IsMouseTargetable = isEnabled;

            foreach (GameObject buttonObject in _awardTeamButtons)
                buttonObject.IsMouseTargetable = isEnabled;
        }

        private void ResetAwardOptionStates()
        {
            for (int i = 0; i < _awardOptionStates.Length; i++)
                _awardOptionStates[i] = OptionVisualState.Neutral;
        }

        private void PrimeAwardOptionsFromPendingSelection()
        {
            int pendingIndex = _pendingOptionSelectionIndex;
            if (pendingIndex < 0 || pendingIndex >= _awardOptionStates.Length)
                return;

            int correctIndex = GetCorrectOptionIndex();
            _awardOptionStates[pendingIndex] = pendingIndex == correctIndex
                ? OptionVisualState.Correct
                : OptionVisualState.Wrong;

            ConfigureAwardOptions();

            if (pendingIndex == correctIndex)
            {
                _isMultipleChoiceAnswerConfirmed = true;
                SetText(_answerText, BuildMultipleChoiceAnswerText());
                SetAwardSelectionEnabled(true);
            }
        }

        private static void ApplyOptionVisualState(GameObject optionObject, OptionVisualState state)
        {
            Image image = optionObject?.GetComponent<Image>();
            JeopardyButton button = optionObject?.GetComponent<JeopardyButton>();
            if (image == null || button == null) return;

            Color color = state switch
            {
                OptionVisualState.Wrong => new Color(176, 56, 56),
                OptionVisualState.Correct => new Color(44, 150, 84),
                _ => new Color(30, 66, 155)
            };

            bool isLocked = state != OptionVisualState.Neutral;
            button.LockVisualState = isLocked;
            button.ApplyColors(
                color,
                isLocked ? color : new Color(45, 94, 205),
                isLocked ? color : new Color(18, 43, 105)
            );
            image.fillColor = color;
        }

        private static void ApplyOptionVisualState(Image image, OptionVisualState state)
        {
            if (image == null) return;

            image.fillColor = state switch
            {
                OptionVisualState.Wrong => new Color(176, 56, 56),
                OptionVisualState.Correct => new Color(44, 150, 84),
                _ => new Color(30, 66, 155)
            };
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

        private void PopulateWinnerScene()
        {
            List<JeopardyTeam> rankedTeams = _teams
                .OrderByDescending(team => team.Score)
                .ThenBy(team => team.Name, StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (rankedTeams.Count == 0)
            {
                SetText(_winnerTitleText, "Game Over");
                SetText(_winnerSummaryText, "No teams participated.");
                if (_winnerLogoObject != null)
                    _winnerLogoObject.IsActive = false;
                RebuildWinnerScores(rankedTeams);
                return;
            }

            int bestScore = rankedTeams[0].Score;
            List<JeopardyTeam> winners = rankedTeams
                .Where(team => team.Score == bestScore)
                .ToList();

            if (winners.Count == 1)
            {
                JeopardyTeam winner = winners[0];
                SetText(_winnerTitleText, "Winner");
                SetText(_winnerSummaryText, $"{winner.Name} wins with {winner.Score} points!");
                if (_winnerLogoObject != null)
                {
                    _winnerLogoObject.IsActive = true;
                    SetObjectImage(_winnerLogoObject, winner.LogoImage, GetLogoFallbackColor(winner.Logo));
                }
            }
            else
            {
                string winnerNames = string.Join(", ", winners.Select(team => team.Name));
                SetText(_winnerTitleText, "Tie");
                SetText(_winnerSummaryText, $"{winnerNames} tie with {bestScore} points!");
                if (_winnerLogoObject != null)
                    _winnerLogoObject.IsActive = false;
            }

            RebuildWinnerScores(rankedTeams);
        }

        private void RebuildWinnerScores(List<JeopardyTeam> rankedTeams)
        {
            if (_winnerScoresRoot == null) return;

            RemoveDynamicObjects(_scoreboardItems);

            const int itemWidth = 420;
            const int itemHeight = 48;
            const int gap = 14;

            for (int i = 0; i < rankedTeams.Count; i++)
            {
                JeopardyTeam team = rankedTeams[i];
                GameObject item = InstantiatePrefab(
                    "ScoreboardTeam",
                    _winnerScoresRoot,
                    $"WinnerScore{i}",
                    TopLeft(0, i * (itemHeight + gap), itemWidth, itemHeight)
                );
                if (item == null) continue;

                SetChildText(item, "TeamText", $"{i + 1}. {team.Name} - {team.Score}");
                SetObjectImage(FindChildByName(item, "Logo"), team.LogoImage, GetLogoFallbackColor(team.Logo));
                _scoreboardItems.Add(item);
            }
        }

        private sealed record LogoOption(string Name, string ButtonObjectName, string TextureLink, Color FallbackColor);

        private sealed record QuestionCellBinding(JeopardyQuestion Question, GameObject ButtonObject, Text Text, Image Image);

        private enum OptionVisualState
        {
            Neutral,
            Wrong,
            Correct
        }
    }
}
