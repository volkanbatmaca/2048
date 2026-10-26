using UnityEngine;

[CreateAssetMenu(menuName = "2048/Native Leaderboard Settings")]
public sealed class NativeLeaderboardSettings : ScriptableObject
{
    public string gameCenterLeaderboardId;
    public string playGamesLeaderboardId;
}
