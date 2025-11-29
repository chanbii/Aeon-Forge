using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProgressSave
{
    public int dataVersion = 1;

    public Dictionary<string, int> clearedMaxByArea = new(StringComparer.OrdinalIgnoreCase);

    public int GetClearedMax(string area)
        => clearedMaxByArea.TryGetValue(area ?? "", out var v) ? v : 0;

    public void MarkCleared(string area, int stageNum)
    {
        var key = area ?? "";
        if (!clearedMaxByArea.TryGetValue(key, out var cur) || stageNum > cur)
            clearedMaxByArea[key] = stageNum;
    }

    /// <summary>
    /// 언락 규칙: (clearedMax + 1) 까지 열림
    /// 아무 것도 안 깼으면 clearedMax=0 → 1번만 언락
    /// </summary>
    public bool IsUnlocked(string area, int stageNum)
    {
        int clearedMax = GetClearedMax(area);
        return stageNum <= clearedMax + 1;
    }
}
