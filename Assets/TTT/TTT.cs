using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerOption
{
    NONE, //0
    X, // 1
    O // 2
}

public class TTT : MonoBehaviour
{
    public int Rows;
    public int Columns;
    [SerializeField] BoardView board;

    PlayerOption currentPlayer = PlayerOption.X;
    Cell[,] cells;

    // Start is called before the first frame update
    void Start()
    {
        cells = new Cell[Columns, Rows];

        board.InitializeBoard(Columns, Rows);

        for(int i = 0; i < Rows; i++)
        {
            for(int j = 0; j < Columns; j++)
            {
                cells[j, i] = new Cell();
                cells[j, i].current = PlayerOption.NONE;
            }
        }
    }

    public void MakeOptimalMove()
    {
        if (cells == null || Rows != 3 || Columns != 3 || GetWinner() != PlayerOption.NONE)
        {
            return;
        }

        int[] boardVals = new int[9];
        int[] bestOrder = { 4, 0, 2, 6, 8, 1, 3, 5, 7 };
        int[,] lines =
        {
            { 0, 1, 2 }, { 3, 4, 5 }, { 6, 7, 8 }, //rows
            { 0, 3, 6 }, { 1, 4, 7 }, { 2, 5, 8 }, //columns
            { 0, 4, 8 }, { 2, 4, 6 } //diagonals
        };

        for (int i = 0; i < 9; i++)
        {
            var squareOwner = cells[i % 3, i / 3].current;

            boardVals[i] = squareOwner == PlayerOption.NONE ? 0 : squareOwner == currentPlayer ? 1 : -1;
        }

        List<int> Wins(int p)
        {
            var result = new List<int>();

            for (int l = 0; l < 8; l++)
            {
                int sum = 0;
                int emptySquare = -1;

                for (int j = 0; j < 3; j++)
                {
                    int i = lines[l, j];
                    sum += boardVals[i];

                    if (boardVals[i] == 0)
                    {
                        emptySquare = i;
                    }
                }

                if (sum == 2 * p && emptySquare >= 0 && !result.Contains(emptySquare))
                {
                    result.Add(emptySquare);
                }
            }

            return result;
        }

        List<int> Forks(int p)
        {
            var result = new List<int>();

            for (int i = 0; i < 9; i++)
            {
                if (boardVals[i] != 0)
                {
                    continue;
                }

                boardVals[i] = p;

                if (Wins(p).Count > 1)
                {
                    result.Add(i);
                }

                boardVals[i] = 0;
            }

            return result;
        }

        int Pick()
        {

            foreach (int p in new[] { 1, -1 })
            {
                var wins = Wins(p);

                if (wins.Count > 0)
                {
                    return wins[0];
                }
            }

            var forks = Forks(1);

            if (forks.Count > 0)
            {
                return forks[0];
            }

            forks = Forks(-1);

            if (forks.Count == 1)
            {
                return forks[0];
            }

            if (forks.Count > 1)
            {
                foreach (int i in bestOrder)
                {
                    if (boardVals[i] != 0)
                    {
                        continue;
                    }

                    boardVals[i] = 1;
                    var threats = Wins(1);
                    bool safe;

                    if (threats.Count == 1)
                    {
                        int block = threats[0];
                        boardVals[block] = -1;

                        safe = Wins(-1).Count < 2;

                        boardVals[block] = 0;
                    }
                    else
                    {
                        safe = Forks(-1).Count == 0;
                    }


                    boardVals[i] = 0;

                    if (safe)
                    {
                        return i;
                    }
                }
            }

            if (boardVals[4] == 0)
            {
                return 4;
            }

            foreach (int i in new[] { 0, 2, 6, 8 })
            {
                if (boardVals[i] == -1 && boardVals[8 - i] == 0)
                {
                    return 8 - i;
                }
            }

            foreach (int i in bestOrder)
            {
                if (boardVals[i] == 0)
                {
                    return i;
                }
            }

            return -1;
        }

        bool open = false;

        for (int i = 0; i < 8; i++)
        {
            bool bot = false;
            bool player = false;

            for (int j = 0; j < 3; j++)
            {
                int l = lines[i, j];

                if (boardVals[l] == 1)
                {
                    bot = true;
                }

                if (boardVals[l] == -1)
                {
                    player = true;
                }
            }

            if (!bot || !player)
            {
                open = true;
                break;
            }
        }

        int move = -1;

        if (open)
        {
            move = Pick();
        }
        else
        {
            var empty = new List<int>();

            for (int i = 0; i < 9; i++)
            {
                if (boardVals[i] == 0)
                {
                    empty.Add(i);
                }
            }

            if (empty.Count > 0)
            {
                move = empty[UnityEngine.Random.Range(0, empty.Count)];
            }
        }

        if (move >= 0)
        {
            ChooseSpace(move % 3, move / 3);
        }
    }

    public void ChooseSpace(int column, int row)
    {
        // can't choose space if game is over
        if (GetWinner() != PlayerOption.NONE)
            return;

        // can't choose a space that's already taken
        if (cells[column, row].current != PlayerOption.NONE)
            return;

        // set the cell to the player's mark
        cells[column, row].current = currentPlayer;

        // update the visual to display X or O
        board.UpdateCellVisual(column, row, currentPlayer);

        // if there's no winner, keep playing, otherwise end the game
        if(GetWinner() == PlayerOption.NONE)
            EndTurn();
        else
        {
            Debug.Log("GAME OVER!");
        }
    }

    public void EndTurn()
    {
        // increment player, if it goes over player 2, loop back to player 1
        currentPlayer += 1;
        if ((int)currentPlayer > 2)
            currentPlayer = PlayerOption.X;
    }

    public PlayerOption GetWinner()
    {
        // sum each row/column based on what's in each cell X = 1, O = -1, blank = 0
        // we have a winner if the sum = 3 (X) or -3 (O)
        int sum = 0;

        // check rows
        for (int i = 0; i < Rows; i++)
        {
            sum = 0;
            for (int j = 0; j < Columns; j++)
            {
                var value = 0;
                if (cells[j, i].current == PlayerOption.X)
                    value = 1;
                else if (cells[j, i].current == PlayerOption.O)
                    value = -1;

                sum += value;
            }

            if (sum == 3)
                return PlayerOption.X;
            else if (sum == -3)
                return PlayerOption.O;

        }

        // check columns
        for (int j = 0; j < Columns; j++)
        {
            sum = 0;
            for (int i = 0; i < Rows; i++)
            {
                var value = 0;
                if (cells[j, i].current == PlayerOption.X)
                    value = 1;
                else if (cells[j, i].current == PlayerOption.O)
                    value = -1;

                sum += value;
            }

            if (sum == 3)
                return PlayerOption.X;
            else if (sum == -3)
                return PlayerOption.O;

        }

        // check diagonals
        // top left to bottom right
        sum = 0;
        for(int i = 0; i < Rows; i++)
        {
            int value = 0;
            if (cells[i, i].current == PlayerOption.X)
                value = 1;
            else if (cells[i, i].current == PlayerOption.O)
                value = -1;

            sum += value;
        }

        if (sum == 3)
            return PlayerOption.X;
        else if (sum == -3)
            return PlayerOption.O;

        // top right to bottom left
        sum = 0;
        for (int i = 0; i < Rows; i++)
        {
            int value = 0;

            if (cells[Columns - 1 - i, i].current == PlayerOption.X)
                value = 1;
            else if (cells[Columns - 1 - i, i].current == PlayerOption.O)
                value = -1;

            sum += value;
        }

        if (sum == 3)
            return PlayerOption.X;
        else if (sum == -3)
            return PlayerOption.O;

        return PlayerOption.NONE;
    }
}
