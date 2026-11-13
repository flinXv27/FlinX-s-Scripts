using System;
using System.Collections.Generic;
using UnityEngine;

public static class GameSession
{
    public static int CurrentSlot = -1;          
    public static SaveData Current;
    public static HashSet<string> Collected = new HashSet<string>();
    public static float PlaySeconds;          
    public static bool Playing;                 

    public static void NewGame(int slot, string firstScene)
    {
        SaveData d = new SaveData { scene = firstScene, savedAt = Now() };
        Begin(slot, d);
        SaveSystem.Save(slot, d);   
    }

    public static bool LoadGame(int slot)
    {
        SaveData d = SaveSystem.Load(slot);
        if (d == null) return false;
        Begin(slot, d);
        return true;
    }

    static void Begin(int slot, SaveData d)
    {
        CurrentSlot = slot;
        Current = d;
        Collected = new HashSet<string>(d.collected);
        PlaySeconds = d.playSeconds;
        Playing = true;
    }

    public static void Clear()
    {
        CurrentSlot = -1;
        Current = null;
        Collected = new HashSet<string>();
        PlaySeconds = 0f;
        Playing = false;
    }
    public static void AdvanceToScene(string scene)
    {
        if (Current == null) Current = new SaveData();
        Current.scene = scene;
        Current.hasCheckpoint = false;
        Current.collected = new List<string>(Collected);
        Current.playSeconds = PlaySeconds;
        Current.savedAt = Now();
        if (CurrentSlot >= 0) SaveSystem.Save(CurrentSlot, Current);
    }

    public static void ResetToLastSave()
    {
        Collected = Current != null ? new HashSet<string>(Current.collected) : new HashSet<string>();
    }

    public static void SaveCheckpoint(string scene, Vector2 pos, int health, int maxHealth, float dashCharge)
    {
        if (Current == null) Current = new SaveData();
        Current.scene = scene;
        Current.hasCheckpoint = true;
        Current.x = pos.x;
        Current.y = pos.y;
        Current.health = health;
        Current.maxHealth = maxHealth;
        Current.dashCharge = dashCharge;
        Current.collected = new List<string>(Collected);
        Current.playSeconds = PlaySeconds;
        Current.savedAt = Now();
        if (CurrentSlot >= 0) SaveSystem.Save(CurrentSlot, Current);
    }

    static string Now() => DateTime.Now.ToString("yyyy-MM-dd HH:mm");
}
// this is pretty cool stuff yk? - pradhyot. N 