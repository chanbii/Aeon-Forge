using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class StageListController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameBootstrap bootstrap;
    [SerializeField] private Tables tables;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private RectTransform contentRect;
    [SerializeField] protected RecyclableScrollSlot<StageDto> itemPrefab;

    [Header("Options")]
    [SerializeField] protected int bufferCount = 10; // 미리 로드할 데이터
    [SerializeField] bool autoSelectOnEnable = true;
    [SerializeField] protected float spacing; // 아이템 간의 간격
    [SerializeField] UnityEngine.UI.Button[] areaButtons; // 구역 버튼

    [Header("VerticalScrollView Option")]
    [SerializeField] protected int itemPerRow = 1; // 한 줄에 보여줄 아이템 수
    [SerializeField] protected float topOffset;
    [SerializeField] protected float bottomOffset;
    [SerializeField] protected float hotizontalOffset;

    [Header("DB")]
    [SerializeField] private string databaseFileName = "aeonforge.db";

    [SerializeField] private int slotCount;
    private List<float> heights = new();
    private List<float> offsets = new();
    private int expandedIndex = -1;
    private float collapsedHeight;

    protected LinkedList<RecyclableScrollSlot<StageDto>> slotList = new LinkedList<RecyclableScrollSlot<StageDto>>();
    protected List<StageDto> dataList = new List<StageDto>();
    protected float itemHeight; // 슬롯 높이
    protected float itemWidth; // 슬롯 너비
    protected int poolSize; // 재사용할 슬롯 수
    protected int tmpfirstVisibleIndex; // 현재 첫번째로 보이는 아이템의 인덱스
    protected int contentVisibleSlotCount; // 현재 화면에 보이는 슬롯 개수
    private Dictionary<string, StageProgressDto> progressMap = new();
    private Coroutine animCo;

    [SerializeField] float collapsedRowHeight = 150f;

    void Start()
    {
        if (bootstrap == null) bootstrap = FindObjectOfType<GameBootstrap>();
        var repo = bootstrap?.StageRepo;
        if (repo == null) { Init(new List<StageDto>()); return; }

        var areas = repo.GetAreasInTableOrder();
        SetupAreaButtons(areas);

        repo.EnsureInitialUnlocks();       
        progressMap = repo.GetProgressMap(); 

        var initial = repo.GetAllStages();  
        Init(initial);
    }
    private StageItemView FindViewForIndex(int idx)
    {
        foreach (var it in slotList)
            if (it is StageItemView v && v.enabled && v.gameObject.activeInHierarchy && v.BoundIndex == idx)
                return v;
        return null;
    }

    private void SetupAreaButtons(List<string> areas)
    {
        if (areaButtons == null) return;

        for (int i = 0; i < areaButtons.Length; i++)
        {
            if (i < areas.Count)
            {
                var area = areas[i];
                var btn = areaButtons[i];

                var tmp = btn.GetComponentInChildren<TMP_Text>(true);
                if (tmp) tmp.SetText(area);

                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() =>
                {
                    var list = bootstrap.StageRepo.GetStagesByArea(area);
                    ShowArea(list);
                });

                btn.gameObject.SetActive(true);
            }
            else areaButtons[i].gameObject.SetActive(false);
        }
    }

    private void ShowArea(List<StageDto> list)
    {
        if (bootstrap?.StageRepo != null)
            progressMap = bootstrap.StageRepo.GetProgressMap();

        // 펼친 항목 초기화 후 데이터 교체
        expandedIndex = -1;         
        UpdateData(list);         

        tmpfirstVisibleIndex = 0;
        contentRect.anchoredPosition = Vector2.zero;

        scrollRect.StopMovement();
        Canvas.ForceUpdateCanvases();
        ClampScroll();
    }

    /// <summary>
    /// 초기 설정
    /// </summary>
    /// <param name="dataList"></param>
    /// 
    public virtual void Init(List<StageDto> dataList)
    {
        if (scrollRect == null) { Debug.LogError("[StageList] scrollRect 없음", this); return; }
        if (contentRect == null || !contentRect.gameObject.scene.IsValid())
            contentRect = scrollRect.content;
        if (itemPrefab == null) { Debug.LogError("[StageList] itemPrefab 없음", this); return; }

        this.dataList = dataList ?? new List<StageDto>();

        var scrollRectTransform = scrollRect.GetComponent<RectTransform>();

        // 레이아웃 계산
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRectTransform);

        var viewportRT = scrollRect.viewport ? scrollRect.viewport
                                             : scrollRect.GetComponent<RectTransform>();

        itemWidth = Mathf.Max(1f, viewportRT.rect.width - Mathf.Abs(hotizontalOffset) * 2f);

        float MeasureCollapsed()
        {
            var probe = Instantiate(itemPrefab, contentRect);
            probe.Init();

            float h = 60f; 
            if (probe is StageItemView pv)
            {
                pv.SetExpanded(false, silent: true);          
                Canvas.ForceUpdateCanvases();
                LayoutRebuilder.ForceRebuildLayoutImmediate(pv.RectTransform);
                h = Mathf.Max(h, LayoutUtility.GetPreferredHeight(pv.RectTransform));
                if (h <= 0f) h = pv.RectTransform.rect.height; 
            }

#if UNITY_EDITOR
            DestroyImmediate(probe.gameObject);
#else
    Destroy(probe.gameObject);
#endif

            return Mathf.Clamp(h, 40f, viewportRT.rect.height);
        }

        float measured = MeasureCollapsed();          
        collapsedHeight = (collapsedRowHeight > 0f) ? collapsedRowHeight : measured;
        collapsedHeight = Mathf.Clamp(collapsedHeight, 40f, 200f);
        expandedIndex = -1;

        // 가변 높이용 테이블 초기화 (모두 접힘)
        heights = new List<float>(this.dataList.Count);
        offsets = new List<float>(this.dataList.Count);
        float acc = 0f;
        for (int i = 0; i < this.dataList.Count; i++)
        {
            heights.Add(collapsedHeight);
            offsets.Add(acc);
            acc += collapsedHeight + spacing;
        }
        float contentHeight = Mathf.Max(0, acc - spacing) + bottomOffset;

        // 앵커/피벗/초기 위치 
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);
        contentRect.anchoredPosition = Vector2.zero;

        // 슬롯 수 계산
        scrollRectTransform = scrollRect.GetComponent<RectTransform>();
        int visibleRows = Mathf.CeilToInt((scrollRectTransform.rect.height + spacing) / (collapsedHeight + spacing));
        contentVisibleSlotCount = Mathf.Max(1, visibleRows) * itemPerRow;

        // 풀 정리/생성
        scrollRect.onValueChanged.RemoveListener(OnScroll);
        foreach (var s in slotList) if (s) Destroy(s.gameObject);
        slotList.Clear();

        poolSize = contentVisibleSlotCount + (bufferCount * 2 * itemPerRow);
        tmpfirstVisibleIndex = 0;

        int index = 0;
        for (int i = 0; i < poolSize; i++)
        {
            var item = Instantiate(itemPrefab, contentRect);
            slotList.AddLast(item);
            item.Init();
            UpdateSlot(item, index++);
        }

        scrollRect.onValueChanged.AddListener(OnScroll);
        scrollRect.StopMovement();
        Canvas.ForceUpdateCanvases();
        ClampScroll();
    }

    private void UpdateSlot(RecyclableScrollSlot<StageDto> item, int index)
    {

        if (index < 0 || index >= dataList.Count)
        {
            item.gameObject.SetActive(false);
            return;
        }

        var rt = item.RectTransform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.localScale = Vector3.one;

        // 1열 기준 중앙 정렬
        float offsetX = (contentRect.rect.width - itemWidth) * 0.5f;
        float x = offsetX + hotizontalOffset;
        float y = topOffset + offsets[index];

        rt.anchoredPosition = new Vector2(x, -y);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, itemWidth);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, heights[index]);

        item.gameObject.SetActive(true);
        item.UpdateSlot(dataList[index]);

        var data = dataList[index];
        if (item is StageItemView view)
        {
            bool isExpanded = (expandedIndex == index);
            string sid = data.StageID; 
            bool unlocked = progressMap.TryGetValue(sid, out var prog) && prog.IsUnlocked;
            view.SetUnlockState(unlocked);
            view.Bind(index, isExpanded, OnRequestExpand);
        }
    }

    private void OnRequestExpand(int newIndex, StageItemView sender)
    {
        if (animCo != null) StopCoroutine(animCo);
        animCo = StartCoroutine(AnimateExpandSequence(newIndex, sender));
    }
    private IEnumerator AnimateExpandSequence(int newIndex, StageItemView newView)
    {
        int old = expandedIndex;
        StageItemView oldView = (old >= 0) ? FindViewForIndex(old) : null;

        if (old >= 0 && old != newIndex)
        {
            float from = heights[old];
            float to = collapsedHeight;
            if (oldView) oldView.EnsureDetailsVisibleForAnim();
            yield return StartCoroutine(AnimateHeight(oldView, old, from, to, expanding: false));
            if (oldView) oldView.HideDetailsAfterAnim();
            heights[old] = collapsedHeight;
        }

        expandedIndex = newIndex;

        EnsureExpandedFullyVisible(newIndex, 18f);

        newView.EnsureDetailsVisibleForAnim();
        float target = newView.MeasurePreferredHeight();
        float fromH = heights[newIndex];
        float toH = Mathf.Max(collapsedHeight + 1f, target);

        yield return StartCoroutine(AnimateHeight(newView, newIndex, fromH, toH, expanding: true));
    }

    private void EnsureExpandedFullyVisible(int index, float pad = 12f)
    {
        if (index < 0 || index >= offsets.Count) return;

        float viewY = contentRect.anchoredPosition.y;                 // 0=맨위, +일수록 아래로 스크롤
        float viewH = scrollRect.viewport.rect.height;
        float maxY = Mathf.Max(0f, contentRect.rect.height - viewH);

        float itemTop = topOffset + offsets[index];
        float itemBottom = itemTop + heights[index];

        float newY = viewY;

        if (itemTop < viewY + pad)
            newY = itemTop - pad;

        if (itemBottom > viewY + viewH - pad)
            newY = itemBottom - viewH + pad;

        contentRect.anchoredPosition = new Vector2(
            contentRect.anchoredPosition.x,
            Mathf.Clamp(newY, 0f, maxY)
        );
    }

    private IEnumerator AnimateHeight(StageItemView view, int idx, float from, float to, bool expanding)
    {
        const float duration = 0.22f;
        float t = 0f;

        while (t < 1f)
        {
            float u = 1f - Mathf.Pow(1f - t, 3f);
            float h = Mathf.Lerp(from, to, u);

            heights[idx] = h;

            if (view)
            {
                float a = Mathf.InverseLerp(collapsedHeight, to, h);
                view.SetDetailsAlpha(a);
            }

            RebuildOffsetsFrom(0);
            RelayoutVisible();

            yield return null;
            t += Time.unscaledDeltaTime / duration;
        }

        heights[idx] = to;
        RebuildOffsetsFrom(0);
        RelayoutVisible();
        ClampScroll();
        EnsureExpandedFullyVisible(idx, 12f);   

        if (!expanding && view) view.HideDetailsAfterAnim();
    }
    private void RebuildOffsetsFrom(int _)
    {
        if (heights.Count == 0) return;

        float acc = 0f;
        for (int i = 0; i < heights.Count; i++)
        {
            offsets[i] = acc;
            acc += heights[i] + spacing;
        }

        float contentHeight = Mathf.Max(0, acc - spacing) + bottomOffset;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

        ClampScroll();
    }

    private void RelayoutVisible()
    {
        int first = FindFirstVisibleIndex();
        tmpfirstVisibleIndex = first;

        int start = Mathf.Max(0, first - bufferCount);
        int idx = start;

        foreach (var it in slotList)
            UpdateSlot(it, idx++);
    }

    private int FindFirstVisibleIndex()
    {
        if (offsets == null || offsets.Count == 0) return 0;

        float y = Mathf.Max(0f, contentRect.anchoredPosition.y - topOffset);

        int lo = 0, hi = offsets.Count - 1, ans = 0;
        while (lo <= hi)
        {
            int mid = (lo + hi) >> 1;
            float end = offsets[mid] + heights[mid];
            if (end > y) { ans = mid; hi = mid - 1; }
            else { lo = mid + 1; }
        }
        return ans;
    }

    private void OnScroll(Vector2 _)
    {
        if (dataList == null || dataList.Count == 0) return;

        int first = FindFirstVisibleIndex();
        if (first == tmpfirstVisibleIndex) return;
        tmpfirstVisibleIndex = first;

        int index = first - bufferCount;
        var node = slotList.First;
        while (node != null)
        {
            UpdateSlot(node.Value, index++);
            node = node.Next;
        }
    }


    private void ClampScroll()
    {
        var viewportH = scrollRect.viewport.rect.height;
        var contentH = contentRect.rect.height;

        bool canScroll = contentH > viewportH + 0.5f;
        scrollRect.vertical = canScroll;

        float maxY = Mathf.Max(0f, contentH - viewportH);
        var pos = contentRect.anchoredPosition;
        pos.y = Mathf.Clamp(pos.y, 0f, maxY);
        contentRect.anchoredPosition = pos;
    }

    public void UpdateData(List<StageDto> newData)
    {
        this.dataList = newData ?? new List<StageDto>();
        expandedIndex = -1;

        heights.Clear(); offsets.Clear();
        float acc = 0f;
        for (int i = 0; i < this.dataList.Count; i++)
        {
            heights.Add(collapsedHeight);
            offsets.Add(acc);
            acc += collapsedHeight + spacing;
        }
        float contentHeight = Mathf.Max(0, acc - spacing) + bottomOffset;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

        contentRect.anchoredPosition = Vector2.zero;
        tmpfirstVisibleIndex = 0;

        int idx = 0; 
        foreach (var it in slotList) 
            UpdateSlot(it, idx++);

        scrollRect.StopMovement();
        Canvas.ForceUpdateCanvases();
        ClampScroll();
    }
}
