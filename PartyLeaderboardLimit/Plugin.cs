using HarmonyLib;
using IPA;
using IPA.Loader;
using IpaLogger = IPA.Logging.Logger;
using System.Reflection;
using System.Collections.Generic;

namespace PartyLeaderboardLimit;

[Plugin(RuntimeOptions.DynamicInit)]
[HarmonyPatch(typeof(LocalLeaderboardsModel), "AddScore", 
    new[] {typeof(string), typeof(LocalLeaderboardsModel.LeaderboardType), typeof(string), typeof(int), typeof(bool)})]
public class LocalLeaderboardAddScorePatch
{
    internal static IpaLogger Log { get; private set; } = null!;
    private Harmony _harmony;
    private Assembly _executingAssembly = Assembly.GetExecutingAssembly();
    public static int ScoreLimit = 2;
    
    [Init]
    public LocalLeaderboardAddScorePatch(IpaLogger ipaLogger, PluginMetadata pluginMetadata)
    {
        Log = ipaLogger;
        Log.Info($"{pluginMetadata.Name} {pluginMetadata.HVersion} initialized.");
        _harmony = new Harmony(pluginMetadata.Id);
    }

    [OnStart]
    public void OnApplicationStart()
    {
        //Log.Info("TEST: appstart");
        Log.Debug("OnApplicationStart");
        _harmony.PatchAll(_executingAssembly);
    }

    [OnExit]
    public void OnApplicationQuit()
    {
        //Log.Info("TEST: appquit");
        Log.Debug("OnApplicationQuit");
        _harmony.UnpatchSelf();
    }

    [HarmonyPrefix]
    public static bool Prefix(LocalLeaderboardsModel __instance, string leaderboardId, LocalLeaderboardsModel.LeaderboardType leaderboardType, string playerName, int score, bool fullCombo)
    {
        //Log.Info("TEST: Prefix called");
            if (leaderboardType == LocalLeaderboardsModel.LeaderboardType.Daily ) //  skip the daily leaderboard
            { 
                // Run the original implementation
                return true;
            }
            
            LocalLeaderboardsModel.LeaderboardData leaderboardData = __instance.GetLeaderboardData(leaderboardId, leaderboardType);
            List<int> nameArr = [];
            if (leaderboardData != null)
            {
                List<LocalLeaderboardsModel.ScoreData> scores = leaderboardData._scores;
                
                // finds all other scores made by the player and stores their list indexes
                for (int i = 0; i < scores.Count; i++)
                {
                    //Log.Info(scores[i]._score.ToString() + ":" + scores[i]._playerName);
                    if (scores[i]._playerName.Equals(playerName))
                    {
                        //Log.Info("namearr add");
                        if (nameArr != null) nameArr.Add(i);
                    }
                }

                if (nameArr != null)
                {
                    while (nameArr.Count > ScoreLimit) // remove all excess scores
                    {
                        //Log.Info("remove excess run");
                        int nameIndex = nameArr[nameArr.Count - 1];

                        List<LocalLeaderboardsModel.ScoreData> scores2 = leaderboardData._scores;

                        scores2.RemoveAt(nameIndex);
                        nameArr.RemoveAt(nameIndex);
                    }
                    
                    if (nameArr.Count == ScoreLimit) // deletes lowest score to make space for the new higher one
                    {
                        List<LocalLeaderboardsModel.ScoreData> scores2 = leaderboardData._scores;
                        int nameIndex = nameArr[nameArr.Count - 1];
                        if (scores2[nameIndex]._score < score)
                        {
                            scores2.RemoveAt(nameIndex);
                            nameArr.RemoveAt(nameIndex);
                            //Log.Info("remove run");
                        }
                        else // skips if new score is the lowest
                        {
                            //Log.Info("skip score run");
                            return false;
                        }
                    }
                }
            }
            // Run the original implementation
            //Log.Info("original run");
            return true;
        }
    }
    