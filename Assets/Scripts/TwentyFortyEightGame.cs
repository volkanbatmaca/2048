using System.Collections.Generic;
using System.Drawing;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms.Impl;
using Color = UnityEngine.Color;

public interface IRewardedAdProvider
{
    void ShowRewardedAd(System.Action<bool> onCompleted);
}

public interface IPrivacyOptionsProvider
{
    bool IsPrivacyOptionsRequired { get; }
    void ShowPrivacyOptions();
}

public interface IAdBannerProvider
{
    float ReservedBannerHeightPixels { get; }
    void SetBannerVisible(bool visible);
}

public interface INativeLeaderboardProvider
{
    bool IsConfigured { get; }
    void SubmitBestScore(long score);
    void ShowLeaderboard(System.Action<bool> onCompleted);
}

public sealed class TwentyFortyEightGame : MonoBehaviour
{
private const int Size = 4;
private const int WinTile = 2048;
private const int FreeUndoLimit = 3;
private const int LeaderboardTimeBaseSeconds = 86400;
private const float SwipeThreshold = 50f;
private const string BestScoreKey = "2048_best_score";
private const string BestScoreTimeKey = "2048_best_score_time";
private const string LanguageKey = "2048_language";
private const string ThemeKey = "2048_theme";

private static readonly string[] Languages = { "Türkçe", "English", "Español", "Français", "Deutsch" };
private static readonly string[] ThemeKeys = { "sage", "yellow", "red", "blue", "orange" };
private static readonly string[][] ThemeColors =
{
    new[] { "F2F5F0", "B7C8BE", "DDE6DF", "648574", "4B6256" },
    new[] { "F5F2E8", "D6CDAF", "E9E2C7", "A39468", "615A43" },
    new[] { "F5EFED", "D8BDB8", "EADAD6", "B37C73", "6C514D" },
    new[] { "EEF3F6", "B9CAD5", "DCE7ED", "6D91A6", "455C6B" },
    new[] { "F7F1EB", "D9C1AA", "EDE0D2", "B9845F", "684F3E" }
};
private static readonly string[][] TilePalettes =
{
    new[] { "E3ECE5", "CDDDD1", "B5CEBD", "9CBFA9", "86AE96", "709A82", "5F876F", "527860", "456A52", "395B46", "2E4B3A" },
    new[] { "F0EAD3", "E5DDB9", "D9CE9F", "CCBD86", "BFAE70", "AD9B5E", "9D8A4D", "8C793F", "7A6833", "695829", "58491F" },
    new[] { "F1E2DE", "E8CCC5", "DDB6AD", "D2A198", "C58C82", "B87970", "A96562", "995653", "874747", "763C3C", "653232" },
    new[] { "E0EAF0", "C9DCE6", "B2CFDE", "9BBFD2", "83ADC4", "6C9BB6", "5D8499", "4F748A", "42647A", "36556A", "2B465A" },
    new[] { "F1E4D8", "E8D2BE", "DDBFA3", "D2AD8D", "C69C78", "B98B67", "A77856", "966847", "855839", "744B2E", "633E24" }
};
private static readonly string[][] TileTextPalettes =
{
    new[] { "49675C", "3F6B5A", "315E51", "285647", "23483F", "1D4035", "F2F5F0", "F2F5F0", "F2F5F0", "F2F5F0", "F2F5F0" },
    new[] { "6B6247", "625A3F", "595237", "504A31", "47432B", "3E3B27", "F5F2E8", "F5F2E8", "F5F2E8", "F5F2E8", "F5F2E8" },
    new[] { "714F4A", "684640", "5F3E39", "563733", "4D302D", "442A27", "F5EFED", "F5EFED", "F5EFED", "F5EFED", "F5EFED" },
    new[] { "4C6370", "435D6B", "3A5766", "325160", "2A4B5A", "234554", "EEF3F6", "EEF3F6", "EEF3F6", "EEF3F6", "EEF3F6" },
    new[] { "705A48", "67513F", "5E4937", "55412F", "4C3928", "433222", "F7F1EB", "F7F1EB", "F7F1EB", "F7F1EB", "F7F1EB" }
};
private static readonly Dictionary<string, string[]> LocalizedText = new Dictionary<string, string[]>
{
    { "menu", new[] { "Menü", "Menu", "Menú", "Menu", "Menü" } },
    { "resume", new[] { "Oyuna dön", "Return to game", "Volver al juego", "Reprendre la partie", "Zurück zum Spiel" } },
    { "newGame", new[] { "Yeni oyun", "New game", "Nueva partida", "Nouvelle partie", "Neues Spiel" } },
    { "leaderboard", new[] { "Liderlik tablosu", "Leaderboard", "Clasificación", "Classement", "Bestenliste" } },
    { "leaderboardShort", new[] { "Skorlar", "Scores", "Puntos", "Scores", "Punkte" } },
    { "settings", new[] { "Ayarlar", "Settings", "Ajustes", "Paramètres", "Einstellungen" } },
    { "howToPlay", new[] { "Nasıl oynanır?", "How to play", "Cómo jugar", "Comment jouer", "So wird gespielt" } },
    { "back", new[] { "Geri", "Back", "Volver", "Retour", "Zurück" } },
    { "language", new[] { "Dil", "Language", "Idioma", "Langue", "Sprache" } },
    { "theme", new[] { "Tema", "Theme", "Tema", "Thème", "Thema" } },
    { "sage", new[] { "Adaçayı", "Sage", "Salvia", "Sauge", "Salbei" } },
    { "yellow", new[] { "Toz sarı", "Dusty yellow", "Amarillo suave", "Jaune doux", "Sanftes Gelb" } },
    { "red", new[] { "Kiremit kırmızısı", "Terracotta red", "Rojo teja", "Rouge terre cuite", "Terrakotta-Rot" } },
    { "blue", new[] { "Yumuşak mavi", "Soft blue", "Azul suave", "Bleu doux", "Sanftes Blau" } },
    { "orange", new[] { "Yumuşak turuncu", "Soft orange", "Naranja suave", "Orange doux", "Sanftes Orange" } },
    { "score", new[] { "SKOR", "SCORE", "PUNTOS", "SCORE", "PUNKTE" } },
    { "best", new[] { "EN İYİ", "BEST", "RÉCORD", "RECORD", "BESTE" } },
    { "mergeHelp", new[] { "Birleştir, 2048'e ulaş!", "Merge tiles. Reach 2048!", "¡Combina y llega a 2048!", "Fusionne et atteins 2048 !", "Verbinde Steine. Erreiche 2048!" } },
    { "swipeHelp", new[] { "Kaydırarak kutuları birleştir", "Swipe to merge tiles", "Desliza para unir fichas", "Fais glisser pour fusionner", "Wische, um Steine zu verbinden" } },
    { "undo", new[] { "Geri al", "Undo", "Deshacer", "Annuler", "Rückgängig" } },
    { "watchUndo", new[] { "Geri Al (İzle +1)", "Undo (Watch +1)", "Deshacer (Ver +1)", "Annuler (Voir +1)", "Rückgängig (Ansehen +1)" } },
    { "privacy", new[] { "Gizlilik ayarları", "Privacy options", "Opciones de privacidad", "Options de confidentialité", "Datenschutzoptionen" } },
    { "adMissing", new[] { "Reklam sağlayıcısı bağlı değil.", "Ad provider is not configured.", "El proveedor de anuncios no está configurado.", "Le fournisseur de publicité n'est pas configuré.", "Der Werbeanbieter ist nicht eingerichtet." } },
    { "adIncomplete", new[] { "Reklam tamamlanmadı. Geri alma kullanılmadı.", "Ad not completed. Undo was not used.", "Anuncio no completado. No se deshizo la jugada.", "Publicité non terminée. Annulation non utilisée.", "Anzeige nicht abgeschlossen. Zug nicht rückgängig gemacht." } },
    { "noMoves", new[] { "Hamle kalmadı!", "No moves left!", "¡No quedan movimientos!", "Plus de mouvements !", "Keine Züge mehr!" } },
    { "continue", new[] { "Devam et", "Continue", "Continuar", "Continuer", "Weiter" } },
    { "win", new[] { "Harika! 2048'e ulaştın.", "Great! You reached 2048.", "¡Genial! Llegaste a 2048.", "Bravo ! Tu as atteint 2048.", "Super! Du hast 2048 erreicht." } },
    { "leaderboardInfo", new[] { "En yüksek skorları görmek için platform hesabınla giriş yap.", "Sign in with your platform account to view top scores.", "Inicia sesión con tu cuenta de plataforma para ver las mejores puntuaciones.", "Connecte-toi avec ton compte de plateforme pour voir les meilleurs scores.", "Melde dich mit deinem Plattformkonto an, um die Bestwerte zu sehen." } },
    { "openLeaderboard", new[] { "Skorları göster", "Show scores", "Ver puntuaciones", "Afficher les scores", "Bestenliste öffnen" } },
    { "leaderboardSetup", new[] { "Liderlik tablosu henüz bu platformda yapılandırılmadı.", "Leaderboard is not configured for this platform yet.", "La clasificación aún no está configurada para esta plataforma.", "Le classement n'est pas encore configuré pour cette plateforme.", "Die Bestenliste ist für diese Plattform noch nicht eingerichtet." } },
    { "leaderboardUnavailable", new[] { "Liderlik tablosuna bağlanılamadı. Hesap ve platform ayarlarını kontrol et.", "Could not connect to the leaderboard. Check account and platform setup.", "No se pudo conectar. Comprueba la cuenta y la configuración.", "Connexion impossible. Vérifie le compte et la configuration.", "Verbindung fehlgeschlagen. Prüfe Konto und Plattform-Einstellungen." } },
    { "leaderboardPlatform", new[] { "iOS'ta Game Center, Android'de Google Play Oyunlar kullanılır.", "Game Center is used on iOS; Google Play Games on Android.", "Se usa Game Center en iOS y Google Play Juegos en Android.", "Game Center est utilisé sur iOS et Google Play Jeux sur Android.", "Auf iOS wird Game Center, auf Android Google Play Games verwendet." } },
    { "okay", new[] { "Tamam", "OK", "Aceptar", "OK", "OK" } },
    { "tutorial", new[] { "Kaydırarak tüm taşları bir yöne hareket ettir. Aynı sayılar birleşir ve skor kazandırır. 2048'e ulaş!", "Swipe to move all tiles in one direction. Matching numbers merge and add to your score. Reach 2048!", "Desliza para mover las fichas. Los números iguales se unen y suman puntos. ¡Llega a 2048!", "Fais glisser pour déplacer les tuiles. Les nombres identiques fusionnent et rapportent des points. Atteins 2048 !", "Wische, um alle Steine zu bewegen. Gleiche Zahlen verschmelzen und erhöhen deinen Punktestand. Erreiche 2048!" } },
    { "languageName", new[] { "Türkçe", "English", "Español", "Français", "Deutsch" } }
};

private readonly int[,] board = new int[Size, Size];
private readonly List<GameSnapshot> undoHistory = new List<GameSnapshot>();
private readonly List<GamePage> pageHistory = new List<GamePage>();
private int score;
private int bestScore;
private float elapsedSeconds;
private float bestScoreTime = -1f;
private int languageIndex;
private int themeIndex;
private int freeUndosUsed;
private GamePage currentPage = GamePage.Game;
private bool gameOver;
private bool hasWon;
private bool continuedAfterWin;
private bool isShowingRewardedAd;
private string adNotice;
private string leaderboardStatus;
private Vector2 pointerStart;
private bool trackingPointer;
private IAdBannerProvider bannerProvider;

private Texture2D whiteTexture;
private GUIStyle labelStyle;
private GUIStyle titleStyle;
private GUIStyle headerStyle;
private GUIStyle overlayStyle;
private GUIStyle tileLabelStyle;
private GUIStyle scoreHeadingStyle;
private GUIStyle gameHintStyle;
private GUIStyle buttonStyle;

private static float UiScale
{
    get
    {
        var scale = Screen.dpi > 0f ? Screen.dpi / 160f : Screen.width / 360f;
        return Mathf.Clamp(scale, 1f, 4f);
    }
}

private static float UiScreenWidth => Screen.width / UiScale;
private static float UiScreenHeight => Screen.height / UiScale;
private float ReservedBannerHeight => bannerProvider != null
    ? bannerProvider.ReservedBannerHeightPixels / UiScale
    : 50f;

[RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
private static void CreateGameIfMissing()
{
    if (FindFirstObjectByType<TwentyFortyEightGame>() != null)
        return;

    var gameObject = new GameObject("2048 Game");
    gameObject.AddComponent<AdMobRewardedAdProvider>();
    gameObject.AddComponent<NativeLeaderboardProvider>();
    gameObject.AddComponent<TwentyFortyEightGame>();
}

private void Awake()
{
    bestScore = PlayerPrefs.GetInt(BestScoreKey, 0);
    bestScoreTime = PlayerPrefs.HasKey(BestScoreTimeKey)
        ? PlayerPrefs.GetFloat(BestScoreTimeKey, -1f)
        : -1f;
    languageIndex = Mathf.Clamp(PlayerPrefs.GetInt(LanguageKey, 0), 0, Languages.Length - 1);
    themeIndex = Mathf.Clamp(PlayerPrefs.GetInt(ThemeKey, 0), 0, ThemeKeys.Length - 1);
    bannerProvider = FindAdBannerProvider();
    whiteTexture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
    whiteTexture.SetPixel(0, 0, Color.white);
    whiteTexture.Apply();
    NewGame();
    FindLeaderboardProvider()?.SubmitBestScore(CreateLeaderboardScore(bestScore, bestScoreTime));
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
    if (currentPage == GamePage.Game && !gameOver && (!hasWon || continuedAfterWin) && !isShowingRewardedAd)
        elapsedSeconds += Time.unscaledDeltaTime;
    bannerProvider?.SetBannerVisible(true);
}

private void ReadKeyboardInput()
{
    var keyboard = Keyboard.current;
    var backPressed = keyboard != null && keyboard.escapeKey.wasPressedThisFrame;


#if ENABLE_LEGACY_INPUT_MANAGER
backPressed |= Input.GetKeyDown(KeyCode.Escape);
#endif

    if (backPressed)
    {
        NavigateBack();
        return;
    }

    if (keyboard == null)
        return;

    if (currentPage != GamePage.Game)
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
    if (currentPage != GamePage.Game)
    {
        trackingPointer = false;
        return;
    }

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
    elapsedSeconds = 0f;
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

    var previousBestScore = bestScore;
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
    var bestRecordImproved = UpdateBestScoreTime();
    if (bestRecordImproved || bestScore > previousBestScore)
        FindLeaderboardProvider()?.SubmitBestScore(CreateLeaderboardScore(bestScore, bestScoreTime));

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
        adNotice = Text("adMissing");
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
        else
            adNotice = Text("adIncomplete");
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

private static IPrivacyOptionsProvider FindPrivacyOptionsProvider()
{
    var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
    for (var i = 0; i < behaviours.Length; i++)
    {
        if (behaviours[i] is IPrivacyOptionsProvider provider)
            return provider;
    }

    return null;
}

private static INativeLeaderboardProvider FindLeaderboardProvider()
{
    var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
    for (var i = 0; i < behaviours.Length; i++)
    {
        if (behaviours[i] is INativeLeaderboardProvider provider)
            return provider;
    }

    return null;
}

private static IAdBannerProvider FindAdBannerProvider()
{
    var behaviours = FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
    for (var i = 0; i < behaviours.Length; i++)
    {
        if (behaviours[i] is IAdBannerProvider provider)
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

private bool UpdateBestScoreTime()
{
    if (score <= 0 || score != bestScore)
        return false;

    var elapsedWholeSeconds = Mathf.FloorToInt(elapsedSeconds);
    if (bestScoreTime >= 0f && elapsedWholeSeconds >= bestScoreTime)
        return false;

    bestScoreTime = elapsedWholeSeconds;
    PlayerPrefs.SetFloat(BestScoreTimeKey, bestScoreTime);
    PlayerPrefs.Save();
    return true;
}

private static long CreateLeaderboardScore(int points, float timeSeconds)
{
    var seconds = timeSeconds < 0f
        ? LeaderboardTimeBaseSeconds - 1
        : Mathf.Clamp(Mathf.FloorToInt(timeSeconds), 0, LeaderboardTimeBaseSeconds - 1);
    var timeBonus = LeaderboardTimeBaseSeconds - 1 - seconds;
    return (long)points * LeaderboardTimeBaseSeconds + timeBonus;
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
    var previousMatrix = GUI.matrix;
    GUI.matrix = Matrix4x4.Scale(Vector3.one * UiScale);
    try
    {
        EnsureStyles();
        DrawSolidRect(new Rect(0, 0, UiScreenWidth, UiScreenHeight), ThemeBackground());
        ApplyThemeToStyles();

        switch (currentPage)
        {
            case GamePage.Game:
                DrawGamePage();
                break;
            case GamePage.Menu:
                DrawMenuPage();
                break;
            case GamePage.Settings:
                DrawSettingsPage();
                break;
            case GamePage.Leaderboard:
                DrawLeaderboardPage();
                break;
            case GamePage.HowToPlay:
                DrawHowToPlayPage();
                break;
        }
    }
    finally
    {
        GUI.matrix = previousMatrix;
    }
}

private void DrawGamePage()
{
    var privacyOptionsProvider = FindPrivacyOptionsProvider();
    var showPrivacyOptions = privacyOptionsProvider != null && privacyOptionsProvider.IsPrivacyOptionsRequired;

    // Banner reklam alanı rezervasyonu (Alt Kısım)
    var safeArea = Screen.safeArea;
    safeArea.x /= UiScale;
    safeArea.y /= UiScale;
    safeArea.width /= UiScale;
    safeArea.height /= UiScale;
    var availableHeight = Mathf.Max(0f, UiScreenHeight - ReservedBannerHeight - safeArea.yMin);

    // Tahtanın boyutu (Mümkün olduğunca büyük dikey kare)
    var maxBoardWidth = UiScreenWidth - 32f;
    var boardSize = maxBoardWidth;

    // Dinamik Yükseklik Oranları
    var spacing = boardSize * 0.02f;
    var titleBadgeWidth = boardSize * 0.32f;
    var scoreBoxWidth = (boardSize - titleBadgeWidth - spacing * 2f) / 2f;

    var scoreBoxHeight = scoreBoxWidth * 0.75f;
    var headerButtonHeight = 32f;
    var scoreButtonGap = 6f;
    var headerHeight = scoreBoxHeight + scoreButtonGap + headerButtonHeight;

    var helpTextHeight = 24f;
    var controlHeight = headerButtonHeight;
    var controlGap = 8f;
    var privacyHeight = showPrivacyOptions ? 36f : 0f;

    // Tahta boyutunu dikey alana sığacak şekilde optimize etme
    var fixedElementsHeight = headerHeight + helpTextHeight + (controlHeight * 2f) + controlGap + privacyHeight + 60f;
    if (boardSize + fixedElementsHeight > availableHeight)
    {
        boardSize = Mathf.Max(0f, availableHeight - fixedElementsHeight);
        spacing = boardSize * 0.02f;
        titleBadgeWidth = boardSize * 0.32f;
        scoreBoxWidth = (boardSize - titleBadgeWidth - spacing * 2f) / 2f;
        scoreBoxHeight = scoreBoxWidth * 0.75f;
        headerHeight = scoreBoxHeight + scoreButtonGap + headerButtonHeight;
    }

    var topMargin = Mathf.Max(safeArea.y, (availableHeight - (boardSize + fixedElementsHeight)) / 2f + 10f) + 14f;
    var boardLeft = (UiScreenWidth - boardSize) / 2f;

    // 1. ÜST HEADER ALANI (2048 Logosu, Skor, En İyi)
    var titleRect = new Rect(boardLeft, topMargin, titleBadgeWidth, scoreBoxHeight);
    var scoreRect = new Rect(titleRect.xMax + spacing, topMargin, scoreBoxWidth, scoreBoxHeight);
    var bestRect = new Rect(scoreRect.xMax + spacing, topMargin, scoreBoxWidth, scoreBoxHeight);

    var titleBadgeStyle = new GUIStyle(headerStyle)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = Mathf.Max(22, Mathf.Clamp(Mathf.RoundToInt(scoreBoxHeight * 0.5f), 24, 42) - 3),
        fontStyle = FontStyle.Bold,
        wordWrap = false,
        normal = { textColor = Color.white }
    };
    DrawSolidRect(titleRect, ThemeButton());
    GUI.Label(titleRect, "2048", titleBadgeStyle);

    DrawScorePanel(scoreRect, Text("score"), score);
    DrawScorePanel(bestRect, Text("best"), bestScore);

    // Üst Menü / Skorlar Butonları
    var headerButtonY = topMargin + scoreBoxHeight + scoreButtonGap;
    var originalButtonFontSize = buttonStyle.fontSize;
    buttonStyle.fontSize = 13;

    if (DrawStyledButton(new Rect(scoreRect.x, headerButtonY, scoreBoxWidth, headerButtonHeight), Text("menu")))
        NavigateTo(GamePage.Menu);
    if (DrawStyledButton(new Rect(bestRect.x, headerButtonY, scoreBoxWidth, headerButtonHeight), Text("leaderboardShort")))
    {
        leaderboardStatus = null;
        NavigateTo(GamePage.Leaderboard);
    }
    var timerStyle = new GUIStyle(scoreHeadingStyle)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = Mathf.Clamp(Mathf.RoundToInt(headerButtonHeight * 0.42f), 11, 13)
    };
    GUI.Label(new Rect(boardLeft, headerButtonY, titleBadgeWidth, headerButtonHeight), FormatElapsedTime(), timerStyle);
    buttonStyle.fontSize = originalButtonFontSize;

    // 2. İPUCU METNİ
    var helpTextTop = headerButtonY + headerButtonHeight + 8f;
    gameHintStyle.fontSize = 14;
    var hintRect = new Rect(boardLeft, helpTextTop, boardSize, helpTextHeight);
    while (gameHintStyle.fontSize > 10 && gameHintStyle.CalcSize(new GUIContent(Text("mergeHelp"))).x > hintRect.width)
        gameHintStyle.fontSize--;
    GUI.Label(hintRect, Text("mergeHelp"), gameHintStyle);

    // 3. 4x4 OYUN TAHTASI (EKRANA TAM OTURAN)
    var boardY = helpTextTop + helpTextHeight + 6f;
    var boardRect = new Rect(boardLeft, boardY, boardSize, boardSize);
    DrawBoard(boardRect);

    // 4. ALT KONTROL BUTONLARI (Geri Al & Yeni Oyun)
    var controlsWidth = boardSize;
    var undoRect = new Rect(boardLeft, boardRect.yMax + 14f, controlsWidth, controlHeight);
    var undoLabel = UndoButtonLabel();

    if (DrawStyledButton(undoRect, undoLabel))
        RequestUndo();

    var newGameRect = new Rect(boardLeft, undoRect.yMax + controlGap, controlsWidth, controlHeight);
    if (DrawStyledButton(newGameRect, Text("newGame")))
        NewGame();

    if (showPrivacyOptions)
    {
        var privacyRect = new Rect(boardLeft, newGameRect.yMax + controlGap, controlsWidth, privacyHeight);
        if (GUI.Button(privacyRect, GUIContent.none, labelStyle))
            privacyOptionsProvider.ShowPrivacyOptions();
    }
}

private void DrawMenuPage()
{
    var width = Mathf.Min(UiScreenWidth - 40f, 420f);
    var x = (UiScreenWidth - width) / 2f;
    var titleY = Mathf.Max(36f, UiScreenHeight * 0.13f);
    titleStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(width * 0.19f), 48, 78);
    GUI.Label(new Rect(x, titleY, width, 96f), "2048", titleStyle);
    var buttonWidth = StandardActionButtonWidth();
    var buttonX = (UiScreenWidth - buttonWidth) / 2f;
    var buttonHeight = StandardActionButtonHeight();
    var menuLabel = Text("menu");
    var menuButtonRect = new Rect(buttonX, titleY + 88f, buttonWidth, buttonHeight);
    if (DrawStyledButton(menuButtonRect, menuLabel))
        NavigateTo(GamePage.Game);

    var buttonY = titleY + 150f;
    var gap = 10f;
    if (DrawStyledButton(new Rect(buttonX, buttonY, buttonWidth, buttonHeight), Text("resume")))
        NavigateTo(GamePage.Game);
    if (DrawStyledButton(new Rect(buttonX, buttonY + buttonHeight + gap, buttonWidth, buttonHeight), Text("newGame")))
    {
        NewGame();
        NavigateTo(GamePage.Game);
    }
    if (DrawStyledButton(new Rect(buttonX, buttonY + (buttonHeight + gap) * 2f, buttonWidth, buttonHeight), Text("leaderboard")))
    {
        leaderboardStatus = null;
        NavigateTo(GamePage.Leaderboard);
    }
    if (DrawStyledButton(new Rect(buttonX, buttonY + (buttonHeight + gap) * 3f, buttonWidth, buttonHeight), Text("settings")))
        NavigateTo(GamePage.Settings);
    if (DrawStyledButton(new Rect(buttonX, buttonY + (buttonHeight + gap) * 4f, buttonWidth, buttonHeight), Text("howToPlay")))
        NavigateTo(GamePage.HowToPlay);

}

private void DrawSettingsPage()
{
    DrawPageHeader(Text("settings"));
    var width = Mathf.Min(UiScreenWidth - 40f, 440f);
    var x = (UiScreenWidth - width) / 2f;
    var rowY = Mathf.Max(UiScreenHeight * 0.29f, 150f);
    var rowHeight = 88f;
    DrawSelectorRow(new Rect(x, rowY, width, rowHeight), Text("language"), Languages[languageIndex],
        () => ChangeLanguage(-1), () => ChangeLanguage(1));
    DrawSelectorRow(new Rect(x, rowY + rowHeight + 16f, width, rowHeight), Text("theme"), Text(ThemeKeys[themeIndex]),
        () => ChangeTheme(-1), () => ChangeTheme(1));
    DrawBackButton();
}

private void DrawLeaderboardPage()
{
    DrawPageHeader(Text("leaderboard"));
    var width = Mathf.Min(UiScreenWidth - 44f, 440f);
    var x = (UiScreenWidth - width) / 2f;
    var infoY = UiScreenHeight * 0.31f;
    GUI.Label(new Rect(x, infoY, width, 90f), Text("leaderboardInfo"), labelStyle);
    GUI.Label(new Rect(x, infoY + 90f, width, 64f), Text("leaderboardPlatform"), labelStyle);

    var provider = FindLeaderboardProvider();
    var configured = provider != null && provider.IsConfigured;
    var statusText = leaderboardStatus ?? (configured ? string.Empty : Text("leaderboardSetup"));
    if (!string.IsNullOrEmpty(statusText))
        GUI.Label(new Rect(x, infoY + 164f, width, 56f), statusText, labelStyle);

    var actionButtonWidth = StandardActionButtonWidth();
    var actionButtonHeight = StandardActionButtonHeight();
    var actionButtonX = (UiScreenWidth - actionButtonWidth) / 2f;
    var safeAreaBottom = UiScreenHeight - Screen.safeArea.yMin / UiScale - ReservedBannerHeight;
    const float bottomMargin = 12f;
    const float buttonGap = 8f;
    var backButtonY = safeAreaBottom - bottomMargin - actionButtonHeight;
    var actionButtonY = backButtonY - buttonGap - actionButtonHeight;
    if (DrawStyledButton(new Rect(actionButtonX, actionButtonY, actionButtonWidth, actionButtonHeight), Text("openLeaderboard")))
    {
        if (!configured)
            leaderboardStatus = Text("leaderboardSetup");
        else
        {
            leaderboardStatus = null;
            provider.ShowLeaderboard(success =>
            {
                if (this)
                    leaderboardStatus = success ? null : Text("leaderboardUnavailable");
            });
        }
    }

    if (DrawStyledButton(
        new Rect(actionButtonX, backButtonY, actionButtonWidth, actionButtonHeight),
        Text("back")))
        NavigateBack();
}

private void DrawHowToPlayPage()
{
    DrawPageHeader(Text("howToPlay"));
    var width = Mathf.Min(UiScreenWidth - 48f, 440f);
    var x = (UiScreenWidth - width) / 2f;
    var textY = UiScreenHeight * 0.32f;
    GUI.Label(new Rect(x, textY, width, 180f), Text("tutorial"), labelStyle);
    DrawBackButton();
}

private void DrawPageHeader(string title)
{
    titleStyle.fontSize = Mathf.Clamp((int)UiScreenWidth / 16, 28, 44);
    GUI.Label(new Rect(24f, 38f, UiScreenWidth - 48f, 64f), title, titleStyle);
}

private void DrawBackButton()
{
    var safeArea = Screen.safeArea;
    safeArea.y /= UiScale;
    safeArea.height /= UiScale;
    var safeAreaTop = UiScreenHeight - (safeArea.y + safeArea.height);
    var safeAreaBottom = UiScreenHeight - safeArea.y - ReservedBannerHeight;
    var buttonWidth = StandardActionButtonWidth();
    var buttonHeight = StandardActionButtonHeight();
    var bottomMargin = Mathf.Clamp(UiScreenHeight * 0.04f, 20f, 36f);
    var preferredY = safeAreaTop + safeArea.height * 0.82f;
    var lastVisibleY = safeAreaBottom - bottomMargin - buttonHeight;
    var rect = new Rect(
        (UiScreenWidth - buttonWidth) / 2f,
        Mathf.Clamp(Mathf.Min(preferredY, lastVisibleY), safeAreaTop, UiScreenHeight - buttonHeight),
        buttonWidth,
        buttonHeight);
    if (DrawStyledButton(rect, Text("back")))
        NavigateBack();
}

private void NavigateTo(GamePage page)
{
    if (currentPage == page)
        return;

    pageHistory.Add(currentPage);
    currentPage = page;
}

private void NavigateBack()
{
    if (pageHistory.Count > 0)
    {
        var lastIndex = pageHistory.Count - 1;
        currentPage = pageHistory[lastIndex];
        pageHistory.RemoveAt(lastIndex);
    }
    else if (currentPage != GamePage.Menu)
    {
        currentPage = GamePage.Menu;
    }
}

private void DrawSelectorRow(Rect rect, string label, string value, System.Action previous, System.Action next)
{
    DrawSolidRect(rect, ThemePanel());
    GUI.Label(new Rect(rect.x + 12f, rect.y + 4f, rect.width - 24f, 24f), label, scoreHeadingStyle);
    var arrowSize = StandardActionButtonHeight();
    var arrowY = rect.y + rect.height - arrowSize - 8f;
    var arrowWidth = Mathf.Min(48f, rect.width * 0.18f);
    var previousRect = new Rect(rect.x + 8f, arrowY, arrowWidth, arrowSize);
    var nextRect = new Rect(rect.xMax - arrowWidth - 8f, arrowY, arrowWidth, arrowSize);
    if (DrawStyledButton(previousRect, "<"))
        previous();
    if (DrawStyledButton(nextRect, ">"))
        next();
    var valueRect = new Rect(previousRect.xMax + 8f, arrowY, nextRect.xMin - previousRect.xMax - 16f, arrowSize);
    DrawSolidRect(valueRect, ThemeEmptyTile());
    GUI.Label(valueRect, value, labelStyle);
}

private float StandardActionButtonWidth()
{
    return Mathf.Min(UiScreenWidth - 48f, 300f);
}

private static float StandardActionButtonHeight()
{
    return 32f;
}

private void ChangeLanguage(int direction)
{
    languageIndex = (languageIndex + direction + Languages.Length) % Languages.Length;
    PlayerPrefs.SetInt(LanguageKey, languageIndex);
    PlayerPrefs.Save();
}

private void ChangeTheme(int direction)
{
    themeIndex = (themeIndex + direction + ThemeKeys.Length) % ThemeKeys.Length;
    PlayerPrefs.SetInt(ThemeKey, themeIndex);
    PlayerPrefs.Save();
}

private string Text(string key)
{
    return LocalizedText.TryGetValue(key, out var translations)
        ? translations[Mathf.Clamp(languageIndex, 0, translations.Length - 1)]
        : key;
}

private void DrawBoard(Rect boardRect)
{
    DrawSolidRect(boardRect, ThemePanel());

    var spacing = boardRect.width * 0.02f;
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
            DrawSolidRect(tileRect, value == 0 ? ThemeEmptyTile() : TileColor(value));
            if (value != 0)
            {
                tileLabelStyle.fontSize = Mathf.RoundToInt(tileSize * (value >= 1024 ? 0.32f : value >= 128 ? 0.38f : 0.46f));
                tileLabelStyle.normal.textColor = TileTextColor(value);
                GUI.Label(tileRect, value.ToString(), tileLabelStyle);
            }
        }
    }

    if (!string.IsNullOrEmpty(adNotice))
        DrawOverlay(boardRect, adNotice, Text("okay"));
    else if (gameOver)
        DrawOverlay(boardRect, Text("noMoves"), UndoButtonLabel());
    else if (hasWon && !continuedAfterWin)
        DrawOverlay(boardRect, Text("win"), Text("continue"));
}

private void DrawOverlay(Rect rect, string message, string buttonText)
{
    var overlayColor = ThemeButton();
    overlayColor.a = 0.88f;
    DrawSolidRect(rect, overlayColor);
    GUI.Label(new Rect(rect.x + 12f, rect.center.y - 52f, rect.width - 24f, 48f), message, overlayStyle);
    if (DrawStyledButton(new Rect(rect.center.x - 90f, rect.center.y + 8f, 180f, 48f), buttonText))
    {
        if (!string.IsNullOrEmpty(adNotice))
            adNotice = null;
        else if (gameOver)
        {
            if (undoHistory.Count > 0)
                RequestUndo();
            else
                NewGame();
        }
        else
            continuedAfterWin = true;
    }
}

private string UndoButtonLabel()
{
    return freeUndosUsed < FreeUndoLimit
        ? Text("undo") + " (" + (FreeUndoLimit - freeUndosUsed) + ")"
        : Text("watchUndo");
}

private void DrawScorePanel(Rect rect, string heading, int value)
{
    DrawSolidRect(rect, ThemePanel());
    scoreHeadingStyle.fontSize = Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.22f), 10, 14);
    GUI.Label(new Rect(rect.x + 2f, rect.y + 3f, rect.width - 4f, rect.height * 0.36f), heading, scoreHeadingStyle);
    var valueStyle = new GUIStyle(tileLabelStyle)
    {
        fontSize = Mathf.Max(8, Mathf.Clamp(Mathf.RoundToInt(rect.height * 0.3f), 10, 20) - 3)
    };
    var valueWidth = rect.width - 8f;
    while (valueStyle.fontSize > 8 && valueStyle.CalcSize(new GUIContent("99999")).x > valueWidth)
        valueStyle.fontSize--;
    valueStyle.normal.textColor = ThemeText();
    GUI.Label(new Rect(rect.x + 4f, rect.y + rect.height * 0.34f, valueWidth, rect.height * 0.62f), value.ToString(), valueStyle);
}

private string FormatElapsedTime()
{
    var totalSeconds = Mathf.FloorToInt(elapsedSeconds);
    return string.Format(
        "{0:00}:{1:00}:{2:00}",
        totalSeconds / 3600,
        (totalSeconds / 60) % 60,
        totalSeconds % 60);
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
        fontSize = Mathf.Clamp((int)UiScreenWidth / 32, 12, 18),
        normal = { textColor = ThemeText() }
    };
    titleStyle = new GUIStyle(labelStyle)
    {
        fontSize = 48,
        fontStyle = FontStyle.Bold,
        normal = { textColor = ThemeText() }
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
        normal = { textColor = ThemeText() }
    };
    scoreHeadingStyle = new GUIStyle(labelStyle)
    {
        fontSize = Mathf.Clamp((int)UiScreenWidth / 38, 11, 16),
        fontStyle = FontStyle.Bold,
        normal = { textColor = ThemeText() }
    };
    gameHintStyle = new GUIStyle(labelStyle)
    {
        fontSize = Mathf.Clamp((int)UiScreenWidth / 28, 13, 17),
        fontStyle = FontStyle.Bold,
        wordWrap = false,
        normal = { textColor = ThemeText() }
    };
    buttonStyle = new GUIStyle(GUI.skin.button)
    {
        alignment = TextAnchor.MiddleCenter,
        fontSize = Mathf.Clamp((int)UiScreenWidth / 28, 14, 18),
        fontStyle = FontStyle.Bold,
        normal = { textColor = Color.white }
    };
    buttonStyle.normal.background = whiteTexture;
    buttonStyle.hover.background = whiteTexture;
    buttonStyle.active.background = whiteTexture;
    buttonStyle.focused.background = whiteTexture;
}

private Color TileColor(int value)
{
    var paletteIndex = value <= 2 ? 0 : Mathf.Clamp(Mathf.RoundToInt(Mathf.Log(value, 2f)) - 1, 0, 10);
    return HexColor(TilePalettes[themeIndex][paletteIndex]);
}

private Color TileTextColor(int value)
{
    var paletteIndex = value <= 2 ? 0 : Mathf.Clamp(Mathf.RoundToInt(Mathf.Log(value, 2f)) - 1, 0, 10);
    return HexColor(TileTextPalettes[themeIndex][paletteIndex]);
}

private bool DrawStyledButton(Rect rect, string text)
{
    GUI.backgroundColor = ThemeButton();
    var clicked = GUI.Button(rect, text, buttonStyle);
    GUI.backgroundColor = Color.white;
    return clicked;
}

private void ApplyThemeToStyles()
{
    if (labelStyle == null)
        return;

    labelStyle.normal.textColor = ThemeText();
    titleStyle.normal.textColor = ThemeText();
    headerStyle.normal.textColor = ThemeText();
    tileLabelStyle.normal.textColor = ThemeText();
    scoreHeadingStyle.normal.textColor = ThemeText();
    gameHintStyle.normal.textColor = ThemeText();
}

private Color ThemeBackground() => ThemeColor(0);
private Color ThemePanel() => ThemeColor(1);
private Color ThemeEmptyTile() => ThemeColor(2);
private Color ThemeButton() => ThemeColor(3);
private Color ThemeText() => ThemeColor(4);

private Color ThemeColor(int colorIndex)
{
    return HexColor(ThemeColors[themeIndex][colorIndex]);
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

private enum GamePage
{
    Menu,
    Game,
    Settings,
    Leaderboard,
    HowToPlay
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
