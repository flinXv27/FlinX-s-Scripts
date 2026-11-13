using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

[Serializable]
public class SaveData
{
    public string scene;
    public bool hasCheckpoint;          // false = fresh game, player spawns where placed in the scene
    public float x, y;
    public int health;
    public int maxHealth;
    public float dashCharge = 1f;
    public List<string> collected = new List<string>();   // IDs of pickups already taken
    public string savedAt;
    public float playSeconds;
}

public static class SaveSystem
{
    public const int SlotCount = 3;

    static string PathFor(int slot) =>
        System.IO.Path.Combine(Application.persistentDataPath, $"save_{slot}.json");

    public static bool Exists(int slot) => File.Exists(PathFor(slot));

    public static bool AnySave()
    {
        for (int i = 0; i < SlotCount; i++) if (Exists(i)) return true;
        return false;
    }

    public static void Save(int slot, SaveData data)
    {
        try
        {
            string path = PathFor(slot);
            string tmp = path + ".tmp";
            File.WriteAllText(tmp, JsonUtility.ToJson(data, true));
            File.Copy(tmp, path, true);   // write-then-copy so a crash can't leave a half-written save
            File.Delete(tmp);
        }
        catch (Exception e) { Debug.LogError("Save failed: " + e.Message); }
    }

    public static SaveData Load(int slot)
    {
        if (!Exists(slot)) return null;
        try { return JsonUtility.FromJson<SaveData>(File.ReadAllText(PathFor(slot))); }
        catch (Exception e) { Debug.LogError("Load failed: " + e.Message); return null; }
    }

    public static void Delete(int slot)
    {
        if (Exists(slot)) File.Delete(PathFor(slot));
    }
}
// full of joy and whimsy - Pradhyot.N 4/11/26 (dont remove this comment, its the signature)
// Visual signal, fire! Dance
// Until you give me an answer, eternal infinity loop
// Signals echoing in the void with nowhere to go
// Always offline, any reply will never come
// A scream without an echo, for you
// Is it a parallel line? Am I alone? This never-ending wait
// Hey (Hello?), where are you now? Try waving your hand
// Um, you’re alive, right? Just one more time - translated lyrics of "Signalling" by AnythingBecomeMoe
