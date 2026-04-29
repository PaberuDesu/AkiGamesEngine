using System;
using System.Collections.Generic;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class FieldController : GameComponent
    {
        private bool _yourMove = true;
        private int _yourSign = 1;
        private int _computerSign => 1 - _yourSign;

        private List<CellController> _cells = [];

        private Random _random = new();

        private EndGame gameFinishScreen = null;

        public override void Awake()
        {
            foreach (GameObject child in gameObject.Children)
            {
                CellController cell = child.GetComponent<CellController>();
                _cells.Add(cell);
                cell.fieldController = this;
            }
            gameFinishScreen = gameObject.Parent.Children[1].GetComponent<EndGame>();
        }
        
        public void ClickOn(CellController cell)
        {
            if (_yourMove && cell.IsEmpty)
            {
                _yourMove = false;
                cell.Sign = _yourSign;
                if (!CheckGameEnd())
                {
                    ComputerMove();
                }
            }
        }

        private void ComputerMove()
        {
            // Получаем список свободных клеток
            List<int> emptyCells = [];
            for (int i = 0; i < _cells.Count; i++)
            {
                if (_cells[i].IsEmpty) emptyCells.Add(i);
            }

            // Выбираем случайную свободную клетку
            if (emptyCells.Count > 0)
            {
                int randomIndex = emptyCells[_random.Next(emptyCells.Count)];
                _cells[randomIndex].Sign = _computerSign;
                
                if (CheckGameEnd()) return;
            }
            _yourMove = true;
        }

        private bool CheckGameEnd()
        {
            // Проверяем победу
            int winner = CheckWinner();
            if (winner >= 0)
            {
                EndGame(winner);
                return true;
            }

            // Проверяем ничью
            if (IsBoardFull())
            {
                EndGame(-1);
                return true;
            }

            return false;
        }

        private int CheckWinner()
        {
            // Проверяем все выигрышные комбинации
            int[,] winConditions = new int[,]
            {
                {0, 1, 2}, {3, 4, 5}, {6, 7, 8}, // Горизонтали
                {0, 3, 6}, {1, 4, 7}, {2, 5, 8}, // Вертикали
                {0, 4, 8}, {2, 4, 6}             // Диагонали
            };

            for (int i = 0; i < winConditions.GetLength(0); i++)
            {
                int a = winConditions[i, 0];
                int b = winConditions[i, 1];
                int c = winConditions[i, 2];

                CellController cellA = _cells[a];
                CellController cellB = _cells[b];
                CellController cellC = _cells[c];

                int? cellASign = cellA?.Sign;

                if (cellASign != null && cellASign == cellB.Sign && cellASign == cellC.Sign)
                {
                    return (int)cellASign;
                }
            }

            return -1; // Нет победителя
        }

        private bool IsBoardFull()
        {
            foreach (CellController cell in _cells)
            {
                if (cell.IsEmpty) return false;
            }
            return true;
        }

        private void EndGame(int winner)
        {
            _yourMove = false;
            gameFinishScreen.ShowEndScreen(winner, _yourSign);
        }

        public void Restart()
        {
            gameFinishScreen.Close();

            _yourSign = 1 - _yourSign;
            _yourMove = _yourSign == 1;
            foreach (CellController cell in _cells)
            {
                cell.Reset();
            }

            if (!_yourMove) ComputerMove();
        }
    }
}