using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

[DefaultExecutionOrder(-100)]
public sealed class TwentyFortyEightGame : MonoBehaviour
{
    private const int Size = 4;
    private const int WinTile = 2048;
    private const string BestScoreKey = "TwentyFortyEightBestScore";
    private const float SwipeThreshold = 45f;
    private const int FreeUndoLimit = 3;

    private readonly int[,] board = new int[Size, Size];
    private readonly List<GameSnapshot> undoHistory = new List<GameSnapshot>();
    private int score;
    private int bestScore;
    private int freeUndosUsed;
    private bool gameOver;
    private bool hasWon;
    private bool continuedAfterWin;
    private bool isShowingRewardedAd;
    private string adNotice;
    private Vector2 pointerStart;
    private bool trackingPointer;

    private Texture2D whiteTexture;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private GUIStyle headerStyle;
    private GUIStyle overlayStyle;
    private GUIStyle tileLabelStyle;
    private GUIStyle buttonStyle;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CreateGameIfMissing()
    {
        if (FindFirstObjectByType<TwentyFortyEightGame>() != null)
            return;

        var gameObject = new GameObject("2048 Game");
        gameObject.AddComponent<TwentyFortyEightGame>();
    }

    private void Awake()
    {
        bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
        whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();
        NewGame();
    }

    private void OnDestroy()
    {
        if (whiteTexture != null)
            Destroy(whiteTexture);
    }

    private void Update()
    {
        ReadKeyboardInput();
        ReadPointerInput();
    }

    private void ReadKeyboardInput()
    {
        var keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (keyboard.upArrowKey.wasPressedThisFrame || keyboard.wKey.wasPressedThisFrame)
            Move(Direction.Up);
        else if (keyboard.downArrowKey.wasPressedThisFrame || keyboard.sKey.wasPressedThisFrame)
            Move(Direction.Down);
        else if (keyboard.leftArrowKey.wasPressedThisFrame || keyboard.aKey.wasPressedThisFrame)
            Move(Direction.Left);
        else if (keyboard.rightArrowKey.wasPressedThisFrame || keyboard.dKey.wasPressedThisFrame)
            Move(Direction.Right);

        if (keyboard.rKey.wasPressedThisFrame)
            NewGame();
    }

    private void ReadPointerInput()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;
            if (touch.press.wasPressedThisFrame)
            {
                pointerStart = touch.position.ReadValue();
                trackingPointer = true;
            }
            else if (touch.press.wasReleasedThisFrame && trackingPointer)
            {
                ProcessSwipe(touch.position.ReadValue());
                trackingPointer = false;
            }

            return;
        }

        var mouse = Mouse.current;
        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            pointerStart = mouse.position.ReadValue();
            trackingPointer = true;
        }
        else if (mouse.leftButton.wasReleasedThisFrame && trackingPointer)
        {
            ProcessSwipe(mouse.position.ReadValue());
            trackingPointer = false;
        }
    }

    private void ProcessSwipe(Vector2 pointerEnd)
    {
        var delta = pointerEnd - pointerStart;
        if (delta.sqrMagnitude < SwipeThreshold * SwipeThreshold)
            return;

        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
            Move(delta.x > 0 ? Direction.Right : Direction.Left);
        else
            Move(delta.y > 0 ? Direction.Up : Direction.Down);
    }

    private void NewGame()
    {
        if (isShowingRewardedAd)
            return;

        System.Array.Clear(board, 0, board.Length);
        undoHistory.Clear();
        score = 0;
        freeUndosUsed = 0;
        gameOver = false;
        hasWon = false;
        continuedAfterWin = false;
        adNotice = null;
        AddRandomTile(true);
        AddRandomTile(true);
    }

    private void Move(Direction direction)
    {
        if (isShowingRewardedAd || gameOver || (hasWon && !continuedAfterWin))
            return;

        var snapshot = CaptureSnapshot();
        var changed = false;
        for (var line = 0; line < Size; line++)
        {
            var values = new int[Size];
            for (var offset = 0; offset < Size; offset++)
            {
                var row = direction == Direction.Up || direction == Direction.Down
                    ? (direction == Direction.Up ? offset : Size - 1 - offset)
                    : line;
                var column = direction == Direction.Left || direction == Direction.Right
                    ? (direction == Direction.Left ? offset : Size - 1 - offset)
                    : line;
                values[offset] = board[row, column];
            }

            var mergedValues = MergeLine(values);
            for (var offset = 0; offset < Size; offset++)
            {
                var row = direction == Direction.Up || direction == Direction.Down
                    ? (direction == Direction.Up ? offset : Size - 1 - offset)
                    : line;
                var column = direction == Direction.Left || direction == Direction.Right
                    ? (direction == Direction.Left ? offset : Size - 1 - offset)
                    : line;

                if (board[row, column] != mergedValues[offset])
                    changed = true;
                board[row, column] = mergedValues[offset];
            }
        }

        if (!changed)
            return;

        undoHistory.Add(snapshot);
        AddRandomTile();
        if (HasNoMoves())
            gameOver = true;
    }

    private GameSnapshot CaptureSnapshot()
    {
        return new GameSnapshot
        {
            board = (int[,])board.Clone(),
            score = score,
            gameOver = gameOver,
            hasWon = hasWon,
            continuedAfterWin = continuedAfterWin
        };
    }

    private void RequestUndo()
    {
        if (isShowingRewardedAd || undoHistory.Count == 0)
            return;

        adNotice = null;
        if (freeUndosUsed < FreeUndoLimit)
        {
            freeUndosUsed++;
            ApplyUndo();
            return;
        }

        var provider = FindRewardedAdProvider();
        if (provider == null)
        {
            adNotice = "Reklam sağlayıcısı bağlı değil.";
            return;
        }

        isShowingRewardedAd = true;
        provider.ShowRewardedAd(rewardEarned =>
        {
            if (!this || !isShowingRewardedAd)
                return;

            isShowingRewardedAd = false;
            if (rewardEarned)
                ApplyUndo();
        });
    }

    private static IRewardedAdProvider FindRewardedAdProvider()
    {
        var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        for (var i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i] is IRewardedAdProvider provider)
                return provider;
        }

        return null;
    }

    private void ApplyUndo()
    {
        if (undoHistory.Count == 0)
            return;

        var snapshot = undoHistory[undoHistory.Count - 1];
        undoHistory.RemoveAt(undoHistory.Count - 1);
        System.Array.Copy(snapshot.board, board, board.Length);
        score = snapshot.score;
        gameOver = snapshot.gameOver;
        hasWon = snapshot.hasWon;
        continuedAfterWin = snapshot.continuedAfterWin;
        adNotice = null;
    }

    private int[] MergeLine(int[] values)
    {
        var compacted = new int[Size];
        var count = 0;
        for (var i = 0; i < values.Length; i++)
        {
            if (values[i] != 0)
                compacted[count++] = values[i];
        }

        var merged = new int[Size];
        var source = 0;
        var target = 0;
        while (source < count)
        {
            var value = compacted[source];
            if (source + 1 < count && compacted[source + 1] == value)
            {
                value *= 2;
                score += value;
                source += 2;
                if (value >= WinTile)
                    hasWon = true;
            }
            else
            {
                source++;
            }

            merged[target++] = value;
        }

        if (score > bestScore)
        {
            bestScore = score;
            PlayerPrefs.SetInt(BestScoreKey, bestScore);
        }

        return merged;
    }

    private void AddRandomTile(bool startingTile = false)
    {
        var emptyCells = new System.Collections.Generic.List<Vector2Int>();
        for (var row = 0; row < Size; row++)
        {
            for (var column = 0; column < Size; column++)
            {
                if (board[row, column] == 0)
                    emptyCells.Add(new Vector2Int(row, column));
            }
        }

        if (emptyCells.Count == 0)
            return;

        var cell = emptyCells[Random.Range(0, emptyCells.Count)];
        board[cell.x, cell.y] = startingTile || Random.value < 0.9f ? 2 : 4;
    }

    private bool HasNoMoves()
    {
        for (var row = 0; row < Size; row++)
        {
            for (var column = 0; column < Size; column++)
            {
                if (board[row, column] == 0)
                    return false;
                if (row + 1 < Size && board[row, column] == board[row + 1, column])
                    return false;
                if (column + 1 < Size && board[row, column] == board[row, column + 1])
                    return false;
            }
        }

        return true;
    }

    private void OnGUI()
    {
        EnsureStyles();
        DrawSolidRect(new Rect(0, 0, Screen.width, Screen.height), HexColor("F2F5F0"));

        var boardSize = Mathf.Min(Screen.width - 32f, Screen.height * 0.58f, 520f);
        boardSize = Mathf.Max(0, boardSize);
        titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(boardSize * 0.14f), 20, 58);
        headerStyle.fontSize = titleStyle.fontSize;
        overlayStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(boardSize / 16f), 14, 24);
        var contentHeight = 204f + boardSize;
        var top = Mathf.Max(12f, (Screen.height - contentHeight) / 2f);
        var boardRect = new Rect((Screen.width - boardSize) / 2f, top + 82f, boardSize, boardSize);

        var titleWidth = boardSize * 0.34f;
        var scoreWidth = (boardSize - titleWidth - 12f) / 2f;
        GUI.Label(new Rect(boardRect.x, top, titleWidth, 54f), "2048", headerStyle);

        DrawScorePanel(new Rect(boardRect.x + titleWidth + 4f, top + 2f, scoreWidth, 50f), "SKOR", score);
        DrawScorePanel(new Rect(boardRect.x + titleWidth + scoreWidth + 8f, top + 2f, scoreWidth, 50f), "EN İYİ", bestScore);

        GUI.Label(new Rect(boardRect.x, top + 53f, boardSize, 24f),
            "Aynı sayıları birleştir, 2048'e ulaş!", labelStyle);

        DrawBoard(boardRect);
        GUI.Label(new Rect(boardRect.x, boardRect.yMax + 10f, boardSize, 24f),
            "Kaydırarak kutuları birleştir", labelStyle);

        var undoLabel = freeUndosUsed < FreeUndoLimit
            ? "Geri al (" + (FreeUndoLimit - freeUndosUsed) + ")"
            : "Reklamla geri al";
        var undoRect = new Rect(boardRect.center.x - 72f, boardRect.yMax + 36f, 144f, 42f);
        if (DrawStyledButton(undoRect, undoLabel))
            RequestUndo();

        var newGameRect = new Rect(boardRect.center.x - 72f, boardRect.yMax + 82f, 144f, 42f);
        if (DrawStyledButton(newGameRect, "Yeni oyun"))
            NewGame();
    }

    private void DrawBoard(Rect boardRect)
    {
        DrawSolidRect(boardRect, HexColor("B7C8BE"));

        var spacing = boardRect.width * 0.018f;
        var tileSize = (boardRect.width - spacing * (Size + 1)) / Size;
        for (var row = 0; row < Size; row++)
        {
            for (var column = 0; column < Size; column++)
            {
                var tileRect = new Rect(
                    boardRect.x + spacing + column * (tileSize + spacing),
                    boardRect.y + spacing + row * (tileSize + spacing),
                    tileSize,
                    tileSize);

                var value = board[row, column];
                DrawSolidRect(tileRect, value == 0 ? HexColor("DDE6DF") : TileColor(value));
                if (value != 0)
                {
                    tileLabelStyle.fontSize = Mathf.RoundToInt(tileSize * (value >= 1024 ? 0.34f : value >= 128 ? 0.4f : 0.48f));
                    tileLabelStyle.normal.textColor = TileTextColor(value);
                    GUI.Label(tileRect, value.ToString(), tileLabelStyle);
                }
            }
        }

        if (!string.IsNullOrEmpty(adNotice))
            DrawOverlay(boardRect, adNotice, "Tamam");
        else if (gameOver)
            DrawOverlay(boardRect, "Hamle kalmadı!", "Yeniden başlat");
        else if (hasWon && !continuedAfterWin)
            DrawOverlay(boardRect, "Harika! 2048'e ulaştın.", "Devam et");
    }

    private void DrawOverlay(Rect rect, string message, string buttonText)
    {
        DrawSolidRect(rect, new Color(0.15f, 0.23f, 0.19f, 0.84f));
        GUI.Label(new Rect(rect.x + 12f, rect.center.y - 52f, rect.width - 24f, 48f), message, overlayStyle);
        if (DrawStyledButton(new Rect(rect.center.x - 90f, rect.center.y + 8f, 180f, 48f), buttonText))
        {
            if (!string.IsNullOrEmpty(adNotice))
                adNotice = null;
            else if (gameOver)
                NewGame();
            else
                continuedAfterWin = true;
        }
    }

    private void DrawScorePanel(Rect rect, string heading, int value)
    {
        DrawSolidRect(rect, HexColor("B7C8BE"));
        GUI.Label(new Rect(rect.x, rect.y + 5f, rect.width, 22f), heading, labelStyle);
        tileLabelStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.width / 5.5f), 12, 22);
        tileLabelStyle.normal.textColor = HexColor("4B6256");
        GUI.Label(new Rect(rect.x, rect.y + 26f, rect.width, 30f), value.ToString(), tileLabelStyle);
    }

    private void DrawSolidRect(Rect rect, Color color)
    {
        GUI.color = color;
        GUI.DrawTexture(rect, whiteTexture);
        GUI.color = Color.white;
    }

    private void EnsureStyles()
    {
        if (labelStyle != null)
            return;

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.width / 32, 12, 18),
            normal = { textColor = HexColor("4B6256") }
        };
        titleStyle = new GUIStyle(labelStyle)
        {
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            normal = { textColor = HexColor("4B6256") }
        };
        headerStyle = new GUIStyle(titleStyle)
        {
            alignment = TextAnchor.MiddleLeft
        };
        overlayStyle = new GUIStyle(labelStyle)
        {
            fontSize = 20,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        tileLabelStyle = new GUIStyle(labelStyle)
        {
            fontSize = 24,
            fontStyle = FontStyle.Bold,
            normal = { textColor = HexColor("4B6256") }
        };
        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.Clamp(Screen.width / 34, 13, 18),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white }
        };
        buttonStyle.normal.background = whiteTexture;
        buttonStyle.hover.background = whiteTexture;
        buttonStyle.active.background = whiteTexture;
        buttonStyle.focused.background = whiteTexture;
    }

    private static Color TileColor(int value)
    {
        switch (value)
        {
            case 2: return HexColor("E3ECE5");
            case 4: return HexColor("CDDDD1");
            case 8: return HexColor("B5CEBD");
            case 16: return HexColor("9CBFA9");
            case 32: return HexColor("86AE96");
            case 64: return HexColor("709A82");
            case 128: return HexColor("E4DDBD");
            case 256: return HexColor("D7CCA4");
            case 512: return HexColor("C9BA8E");
            case 1024: return HexColor("B9A777");
            default: return HexColor("A79563");
        }
    }

    private static Color TileTextColor(int value)
    {
        switch (value)
        {
            case 2: return HexColor("49675C");
            case 4: return HexColor("3F6B5A");
            case 8: return HexColor("315E51");
            case 16: return HexColor("285647");
            case 32: return HexColor("23483F");
            case 64: return HexColor("1D4035");
            case 128: return HexColor("6A5634");
            case 256: return HexColor("604B2E");
            case 512: return HexColor("55422C");
            case 1024: return HexColor("493D2E");
            default: return HexColor("40372A");
        }
    }

    private bool DrawStyledButton(Rect rect, string text)
    {
        GUI.backgroundColor = HexColor("648574");
        var clicked = GUI.Button(rect, text, buttonStyle);
        GUI.backgroundColor = Color.white;
        return clicked;
    }

    private static Color HexColor(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var color);
        return color;
    }

    private enum Direction
    {
        Up,
        Down,
        Left,
        Right
    }

    private sealed class GameSnapshot
    {
        public int[,] board;
        public int score;
        public bool gameOver;
        public bool hasWon;
        public bool continuedAfterWin;
    }
}

public interface IRewardedAdProvider
{
    void ShowRewardedAd(System.Action<bool> onCompleted);
}
