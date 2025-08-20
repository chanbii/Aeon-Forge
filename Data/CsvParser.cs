using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

public static class CsvParser
{
    /// <summary>
    /// �־��� CSV �ؽ�Ʈ�� �Ľ��Ͽ� DTO ����Ʈ�� ��ȯ�մϴ�.
    /// </summary>
    /// <typeparam name="T">CSV ���ڵ带 ������ DTO Ÿ��</typeparam>
    /// <param name="text">CSV ���� ���ڿ�</param>
    /// <returns>DTO �ν��Ͻ����� ����Ʈ</returns>
    public static List<T> Parse<T>(TextAsset asset) where T : new()
        => asset == null ? new List<T>() : Parse<T>(asset.text);

    // CSV -> DTO
    public static List<T> Parse<T>(string text) where T : new()
    {
        var list = new List<T>();
        var rows = ParseRaw(text);
        var t = typeof(T);
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.Public);

        foreach (var dict in rows)
        {
            var inst = new T();
            foreach (var f in fields)
            {
                if (!dict.TryGetValue(f.Name, out var val) || string.IsNullOrEmpty(val))
                    continue;

                try
                {
                    object boxed;
                    var ft = f.FieldType;

                    if (ft == typeof(string)) boxed = val;
                    else if (ft == typeof(int)) boxed = int.Parse(val);
                    else if (ft == typeof(float)) boxed = float.Parse(val);
                    else if (ft == typeof(double)) boxed = double.Parse(val);
                    else if (ft == typeof(bool)) boxed = ParseBool(val);
                    else if (ft.IsEnum) boxed = Enum.Parse(ft, val, ignoreCase: true);
                    else boxed = Convert.ChangeType(val, ft);

                    f.SetValue(inst, boxed);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[Csv] Parse fail: field='{f.Name}', value='{val}' �� {e.Message}");
                }
            }
            list.Add(inst);
        }
        return list;
    }

    // ���� �����͸� ��ųʸ� ����Ʈ�� ��ȯ
    // header�� �������� �� ���� ���� ��Ī
    public static List<Dictionary<string, string>> ParseRaw(string text)
    {
        var list = new List<Dictionary<string, string>>();
        if (string.IsNullOrWhiteSpace(text))
            return list;

        var lines = text.Replace("\r\n", "\n").Replace("\r", "\n").Split('\n');
        if (lines.Length == 0)
            return list;
        if (lines[0].Length > 0 && lines[0][0] == '\uFEFF')
            lines[0] = lines[0].Substring(1);

        int i = 0;
        while (i < lines.Length && string.IsNullOrWhiteSpace(lines[i]))
            i++;
        if(i >= lines.Length)
            return list;

        var header = SplitLine(lines[i++]);
        for(; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            var cols = SplitLine(line);
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for(int c = 0; c < header.Count; c++)
            {
                var key = header[c].Trim();
                var val = (c < cols.Count) ? cols[c] : string.Empty;
                dict[key] = val?.Trim();
            }
            list.Add(dict);
        }
        return list;
    }

    // CSV �� ���� �÷� ������ ����
    public static List<string> SplitLine(string line)
    {
        var res = new List<string>();
        bool q = false;
        var sb = new System.Text.StringBuilder();
        for (int i = 0; i < line.Length; i++)
        {
            char ch = line[i];
            if (ch == '\"')
            {
                if (q && i + 1 < line.Length && line[i + 1] == '\"')
                {
                    sb.Append('\"');
                    i++;
                }
                else
                {
                    q = !q;
                }
            }
            else if (ch == ',' && !q)
            {
                res.Add(sb.ToString());
                sb.Length = 0;
            }
            else
            {
                sb.Append(ch);
            }
        }
        res.Add(sb.ToString());
        return res;
    }

    static bool ParseBool(string v)
    {
        v = v.Trim().ToLowerInvariant();
        return v is "1" or "true" or "yes" or "y" or "on";
    }

   

    
}
