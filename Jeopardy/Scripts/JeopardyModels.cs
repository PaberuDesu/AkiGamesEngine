using System.Collections.Generic;

namespace AkiGames.Scripts
{
    public class JeopardyQuestionSet
    {
        public List<JeopardyTheme> Themes { get; set; } = [];
    }

    public class JeopardyTheme
    {
        public string Title { get; set; } = "";
        public List<JeopardyQuestion> Questions { get; set; } = [];
    }

    public class JeopardyQuestion
    {
        public int Points { get; set; }
        public string Text { get; set; } = "";
        public string Answer { get; set; } = "";
        public string Image { get; set; } = "";
        public List<string> Options { get; set; } = [];
        public bool IsUsed { get; set; }
    }

    public class JeopardyTeam
    {
        public string Name { get; set; } = "";
        public string Logo { get; set; } = "";
        public string LogoImage { get; set; } = "";
        public int Score { get; set; }
    }
}
