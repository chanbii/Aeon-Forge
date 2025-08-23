using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class StageListController : MonoBehaviour
{
    [SerializeField] private Tables tables;
    [SerializeField] private Transform content;
    [SerializeField] private StageItemView itemPrefab;   // ��Ÿ ����
    [SerializeField] private int maxItems = 10;

    private readonly List<StageItemView> pool = new();
    private StageItemView currentOpen;

    // ���� Ŭ��
    public void ShowArea(string areaName)
    {
        if (tables == null || content == null || itemPrefab == null)
        {
            Debug.LogError("[StageList] Inspector �Ҵ� ����(tables/content/itemPrefab).");
            return;
        }

        // maxItems ����
        var stages = tables.GetStagesByArea(areaName, maxItems);
        Rebind(stages);
    }

    private void Rebind(List<StageDto> stages)
    {
        if (stages == null) stages = new List<StageDto>();

        // Ǯ Ȯ��
        while (pool.Count < stages.Count)
        {
            var v = Instantiate(itemPrefab, content);
            v.OnExpandRequest += HandleExpandRequest;
            pool.Add(v);
        }

        // ���ε� & ���ü�
        for (int i = 0; i < pool.Count; i++)
        {
            bool active = i < stages.Count;
            var view = pool[i];
            view.gameObject.SetActive(active);
            if (!active) continue;

            var s = stages[i];

            // ����/��� 1ȸ ��ȸ
            var rewards = tables.GetRewards(s.StageID);
            var costs = tables.GetCosts(s.StageID);

            view.Bind(s, rewards, costs);

            // ��� ���� ���·� ���� (��� ���̾ƿ� �ݿ�)
            view.SetExpanded(false, immediate: true);
        }

        // ���� �����ִ� �׸� �ʱ�ȭ
        currentOpen = null;

        // ���̾ƿ� ����ȭ
        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)content);
    }

    private void HandleExpandRequest(StageItemView requester)
    {
        if (requester == null) return;

        // ���� ������ �ٽ� ������ �� '����'�� ���ϸ� �Ʒ� ��� ���
        if (currentOpen == requester)
        {
            requester.SetExpanded(false);
            currentOpen = null;
            return;
        }

        // �ٸ��� ���� ���� �� �ݰ�, ���� ����
        if (currentOpen != null)
            currentOpen.SetExpanded(false);

        requester.SetExpanded(true);
        currentOpen = requester;
    }

    // (����) �ı� �� �̺�Ʈ ����
    private void OnDestroy()
    {
        foreach (var v in pool)
        {
            if (v != null) v.OnExpandRequest -= HandleExpandRequest;
        }
    }

}
