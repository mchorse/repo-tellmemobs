using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace TellMeMobs;

public enum MobLabelVisibility
{
	Invisible = 0,
	MapVisible,
	Visible
}

[BepInPlugin("McHorse.TellMeMobs", "TellMeMobs", "1.0.1")]
public class TellMeMobs : BaseUnityPlugin
{
    internal static TellMeMobs Instance { get; private set; } = null!;
    internal static ConfigEntry<MobLabelVisibility> LabelVisibility { get; private set; } = null!;
    internal new static ManualLogSource Logger => Instance._logger;
    private ManualLogSource _logger => base.Logger;
    internal Harmony? Harmony { get; set; }

    private bool _postedMessage;
    private bool _wasntTyping;

    public static string GetMobInformation()
    {
        Dictionary<string, int> messageMap = new Dictionary<string, int>();
        EnemyParent[] allEnemies = FindObjectsOfType<EnemyParent>();

        foreach (EnemyParent enemy in allEnemies)
        {
            string? key = enemy.enemyName;
            int value = messageMap.GetValueOrDefault(key, 0);

            messageMap[key] = value + 1;
        }

        List<string> messages = new List<string>();
            
        foreach (var pair in messageMap)
        {
            string pairKey = pair.Key.ToUpper();
                
            messages.Add(pair.Value == 1 ? pairKey : $"{pairKey}x{pair.Value}");
        }

        if (messages.Count > 0)
        {
            return string.Join(", ", messages);
        }

        return "";
    }
    
    private void Awake()
    {
        Instance = this;
        LabelVisibility = Config.Bind(
            "General",
            "Label visibility",
            MobLabelVisibility.Invisible,
            "When to show the mob list on the HUD: never visible, visible when map opened, and always visible"
        );

        // Prevent the plugin from being deleted
        this.gameObject.transform.parent = null;
        this.gameObject.hideFlags = HideFlags.HideAndDontSave;

        Patch();

        Logger.LogInfo($"{Info.Metadata.GUID} v{Info.Metadata.Version} has loaded!");
    }

    internal void Patch()
    {
        Harmony ??= new Harmony(Info.Metadata.GUID);
        Harmony.PatchAll();
    }

    internal void Unpatch()
    {
        Harmony?.UnpatchSelf();
    }

    private void Update()
    {
        if (!SemiFunc.LevelGenDone())
        {
            _postedMessage = false;
            _wasntTyping = false;
            
            return;
        }
        
        if (_postedMessage || Time.timeSinceLevelLoad < 10f) return;
        
        TruckScreenText truckScreenText = TruckScreenText.instance;

        if (truckScreenText)
        {
            if (!_wasntTyping)
            {
                if (truckScreenText.isTyping) _wasntTyping = true;
                else return;
            }

            if (truckScreenText.isTyping) return;

            string message = GetMobInformation();

            if (message.Length > 0)
            {
                truckScreenText.textMesh.text += "\n\n<color=#4d0000><b>TAXMAN:</b></color>\n<sprite name=heart> " + message;
            }

            _postedMessage = true;
        }
    }
}