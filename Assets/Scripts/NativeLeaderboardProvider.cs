using System;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[DefaultExecutionOrder(-150)]
public sealed class NativeLeaderboardProvider : MonoBehaviour, INativeLeaderboardProvider
{
    private NativeLeaderboardSettings settings;
    private long pendingBestScore;
    private bool isAuthenticating;

    public bool IsConfigured => PlatformIsSupported && !string.IsNullOrWhiteSpace(LeaderboardId);

    private string LeaderboardId
    {
        get
        {
            if (settings == null)
                return null;

#if UNITY_IOS && !UNITY_EDITOR
            return settings.gameCenterLeaderboardId;
#elif UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_PLAY_GAMES
            return settings.playGamesLeaderboardId;
#else
            return null;
#endif
        }
    }

    private bool PlatformIsSupported
    {
        get
        {
#if UNITY_IOS && !UNITY_EDITOR
            return true;
#elif UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_PLAY_GAMES
            return true;
#else
            return false;
#endif
        }
    }

    private void Awake()
    {
        settings = Resources.Load<NativeLeaderboardSettings>("NativeLeaderboardSettings");
#if UNITY_ANDROID && !UNITY_EDITOR && GOOGLE_PLAY_GAMES
        GooglePlayGames.PlayGamesPlatform.Activate();
#endif
    }

    public void SubmitBestScore(long score)
    {
        if (!IsConfigured || score <= 0)
            return;

        pendingBestScore = Math.Max(pendingBestScore, score);
        if (Social.localUser.authenticated)
            ReportPendingBestScore();
    }

    public void ShowLeaderboard(Action<bool> onCompleted)
    {
        if (onCompleted == null)
            throw new ArgumentNullException(nameof(onCompleted));

        if (!IsConfigured)
        {
            onCompleted(false);
            return;
        }

        if (Social.localUser.authenticated)
        {
            ReportPendingBestScore();
            Social.ShowLeaderboardUI();
            onCompleted(true);
            return;
        }

        if (isAuthenticating)
        {
            onCompleted(false);
            return;
        }

        isAuthenticating = true;
        Social.localUser.Authenticate(success =>
        {
            isAuthenticating = false;
            if (!success)
            {
                onCompleted(false);
                return;
            }

            ReportPendingBestScore();
            Social.ShowLeaderboardUI();
            onCompleted(true);
        });
    }

    private void ReportPendingBestScore()
    {
        if (pendingBestScore <= 0 || string.IsNullOrWhiteSpace(LeaderboardId))
            return;

        var scoreToReport = pendingBestScore;
        Social.ReportScore(scoreToReport, LeaderboardId, success =>
        {
            if (success)
            {
                if (pendingBestScore == scoreToReport)
                    pendingBestScore = 0;
            }
            else
            {
                Debug.LogWarning("Liderlik tablosuna skor gönderilemedi.");
            }
        });
    }
}
