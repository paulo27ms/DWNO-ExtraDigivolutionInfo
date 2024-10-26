using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HarmonyLib;
using Il2CppSystem.Reflection;


namespace ExtraDigivolutionInfo;

[BepInPlugin(GUID, PluginName, PluginVersion)]
[BepInProcess("Digimon World Next Order.exe")]
public class Plugin : BasePlugin
{
    public static ManualLogSource Logger;
    internal const string GUID = "ExtraDigivolutionInfo";
    internal const string PluginName = "ExtraDigivolutionInfo";
    internal const string PluginVersion = "1.0.0";

    public static ConfigEntry<int> infos;
    public static ConfigEntry<int> infos_known;
    public static ConfigEntry<int> infos_unknown;
    public static ConfigEntry<int> infos_per_unknown;

    public override void Load()
    {
        infos = Config.Bind("#Dojo", "ExtraInfosDojoItem", 4, "How many extra digivolution info rolls you should get from giving an item at the dojo.");
        infos_known = Config.Bind("#Interaction", "ExtraInfosPartner", 5, "How many extra digivolution info rolls you should get from interacting with your partner.");
        infos_unknown = Config.Bind("#Interaction", "ExtraInfosRandomCount", 10, "How many random digimon will be selected to perform digivolution info rolls if you're Mega or above.");
        infos_per_unknown = Config.Bind("#Interaction", "ExtraInfosPerRandom", 1, "How many digivolution info rolls will be done per random digimon picked above.");
        Logger = Log;
        Harmony.CreateAndPatchAll(typeof(Plugin));
    }

    [HarmonyPatch(typeof(EvolutionDojo), "ReleaseEvolutionConditionFlag", new Type[] {typeof(uint), typeof(ItemData)})]
    [HarmonyPostfix]
    public static void ReleaseEvolutionConditionFlag_PostFix(uint digimonID, ItemData item, ref string __result)
    {
        MainGameManager instance = MainGameManager.m_instance;
        EvolutionDojo evolutionDojo = instance.evolutionDojo;
        string message = evolutionDojo.ReleaseEvolutionConditionFlag(digimonID, infos.Value);

        __result += "\n" + message;
    }

    [HarmonyPatch(typeof(PlayerData), "OpenRandomEvolutionCondition", new Type[] {typeof(PartnerCtrl)})]
    [HarmonyPostfix]
    public static void OpenRandomEvolutionCondition_PostFix(PartnerCtrl partnerCtrl, ref string __result)
    {
        MainGameManager instance = MainGameManager.m_instance;
        EvolutionDojo evolutionDojo = instance.evolutionDojo;
        ParameterDigimonData defaultData = partnerCtrl.m_data.m_defaultData;
        if (defaultData.m_growth <= 4)
        {
            uint id = defaultData.m_id;
            string message = evolutionDojo.ReleaseEvolutionConditionFlag(id, infos_known.Value);

            __result += "\n" + message;
        }
        else
        {
            List<uint> Digimons = new List<uint>();
            foreach (ParameterDigimonData @params in AppMainScript.Ref.m_parameters.m_csvbDigimonData.m_params)
            {
                if (@params.m_growth <= 4)
                {
                    Digimons.Add(@params.m_id);
                }
            }
            Random random = new Random();
            int digimonCount = Digimons.Count;
            for (int i = digimonCount - 1; i > 1; i--)
            {
                int j = random.Next(i + 1);
                uint temp = Digimons[i];
                Digimons[i] = Digimons[j];
                Digimons[j] = temp;
            }
            int success_counter = 0;
            int global_counter = 0;
            __result = __result.Trim();
            while (success_counter < infos_unknown.Value)
            {
                if (global_counter == Digimons.Count) break;
                uint id = Digimons[global_counter];
                string message = evolutionDojo.ReleaseEvolutionConditionFlag(id, infos_per_unknown.Value);
                if (!string.IsNullOrEmpty(message))
                {
                    __result += "\n" + message;
                    success_counter++;
                }
                global_counter++;
            }

            __result = __result.Trim();
        }
    }
}