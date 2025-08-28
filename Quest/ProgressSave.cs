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
    /// ��� ��Ģ: (clearedMax + 1) ���� ����
    /// �ƹ� �͵� �� ������ clearedMax=0 �� 1���� ���
    /// </summary>
    public bool IsUnlocked(string area, int stageNum)
    {
        int clearedMax = GetClearedMax(area);
        return stageNum <= clearedMax + 1;
    }
}
