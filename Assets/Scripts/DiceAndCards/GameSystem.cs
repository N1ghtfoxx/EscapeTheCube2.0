//using UnityEngine;
//using UnityEngine.Events;
//using System;
//using System.Collections.Generic;
///// <summary>
///// This script holds game-related data. Levels, settings, global stats, and structs used across multiple systems.
///// It builds a registry of all spacecrafts on Startup of the game
///// </summary>
///// 
//public class GameSystem : MonoBehaviour
//{
//    public static GameSystem Instance { get; private set; }

//    private readonly Dictionary<int, SpacecraftData> _spacecraftById = new();
//    private readonly Dictionary<int, LevelData> _leveldictById = new();

//    public readonly List<LevelData> LevelDatas = new();

//    public readonly List<SpacecraftData> PlayerSpacecrafts = new();
//    public readonly List<SpacecraftData> EnemySpacecrafts = new();
//    public readonly List<SpacecraftData> BossSpacecrafts = new();

//    public PlayerProfile ActiveProfile { get; private set; }
//    private void Awake()
//    {
//        if (Instance != null && Instance != this)
//        {
//            Destroy(this);
//            return;
//        }
//        Instance = this;
//        DontDestroyOnLoad(this);
//        BuildSpacecraftRegistry();
//        RegisterLevels("Levels", LevelDatas);
//    }
//    /// <summary>
//    /// This method builds an entire registry of all spacecrafts and levels to provide references for other components
//    /// </summary>
//    public void BuildSpacecraftRegistry()
//    {
//        _spacecraftById.Clear();
//        PlayerSpacecrafts.Clear();
//        EnemySpacecrafts.Clear();
//        BossSpacecrafts.Clear();

//        RegisterCategory("Spacecrafts/PlayerSC", PlayerSpacecrafts);
//        RegisterCategory("Spacecrafts/EnemySC", EnemySpacecrafts);
//        RegisterCategory("Spacecrafts/BossSC", BossSpacecrafts);
//        //Debug.Log($"Registry complete! Total ships in Dictionary: {_spacecraftById.Count}");
//    }

//    private void RegisterCategory(string path, List<SpacecraftData> categoryList)
//    {
//        SpacecraftData[] assets = Resources.LoadAll<SpacecraftData>(path);

//        foreach (var data in assets)
//        {
//            if (data == null) continue;
//            categoryList.Add(data);
//            if (!_spacecraftById.TryAdd(data.SpacecraftID, data)) continue;
//        }
//    }
//    private void RegisterLevels(string path, List<LevelData> levelDatas)
//    {
//        LevelData[] assets = Resources.LoadAll<LevelData>(path);

//        foreach (var data in assets)
//        {
//            if (data == null) continue;
//            levelDatas.Add(data);
//            if (!_leveldictById.TryAdd(data.LevelID, data)) continue;
//        }
//    }
//    public SpacecraftData GetSpacecraftDataById(int id)
//    {
//        _spacecraftById.TryGetValue(id, out var data);
//        return data;
//    }
//    public int GetNextPlayerShipId(int currentId)
//    {
//        int index = PlayerSpacecrafts.FindIndex(s => s.SpacecraftID == currentId);
//        index = (index + 1) % PlayerSpacecrafts.Count;
//        return PlayerSpacecrafts[index].SpacecraftID;
//    }

//    public int GetPrevPlayerShipId(int currentId)
//    {
//        int index = PlayerSpacecrafts.FindIndex(s => s.SpacecraftID == currentId);
//        index--;
//        if (index < 0) index = PlayerSpacecrafts.Count - 1;
//        return PlayerSpacecrafts[index].SpacecraftID;
//    }
//    public void SetActiveProfile(string playerName)
//    {
//        if (string.IsNullOrEmpty(playerName)) playerName = "NewPilot";
//        playerName.ToString();
//        PlayerProfile profile = SaveManager.Instance.LoadPlayerProfile(playerName);
//        profile.PlayerName = playerName;
//        SaveManager.Instance.SavePlayerProfile(profile);
//        ActiveProfile = profile;
//    }
//}
//#region Scriptable Objects

//[CreateAssetMenu(fileName = "LevelData", menuName = "Game Data/Level Data")]
//public class LevelData : ScriptableObject
//{
//    [SerializeField] private int _levelID;
//    [SerializeField] private int _difficultyRating;
//    [SerializeField] private EnemyData[] _enemies;
//    [SerializeField] private Transform[] _spawnPoints;

//    public int LevelID => _levelID;
//    public int DifficultyRating => _difficultyRating;
//    public EnemyData[] Enemies => _enemies;
//    public Transform[] SpawnPoints => _spawnPoints;
//}
//#endregion
//#region Structs

//#endregion
//#region Enums
//public enum AssociationType
//{
//    Player,
//    Enemy,
//    Boss,
//    Neutral
//}
//public enum GameState
//{
//    MainMenu,
//    Lobby,
//    InGame,
//    PostGame,
//}

//#endregion
//#region Events
//[Serializable]
//public static class GameEvents
//{
//    public static UnityEvent<GameState> OnGameStateChanged = new();
//    public static UnityEvent<PlayerSessionData> OnPlayerStatusChanged = new();
//    public static UnityEvent<Spacecraft> OnEntitySpawn = new();
//    public static UnityEvent OnPlayerDestroyed = new();
//    public static UnityEvent OnEnemyDestroyed = new();
//    public static UnityEvent OnLevelChanged = new();
//    public static UnityEvent<PlayerSession, SpacecraftStats> OnPlayerStatsChanged = new();
//    public static UnityEvent<SpacecraftStats> OnBossStatChanged = new();
//    public static UnityEvent<bool> OnPostGame = new();
//    public static void ChangeGameState(GameState newState)
//    {
//        try
//        {
//            OnGameStateChanged?.Invoke(newState);
//        }
//        catch
//        {
//        }
//    }
//}
//#endregion