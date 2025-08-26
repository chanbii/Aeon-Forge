using System;
using System.Collections.Generic;
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

    protected LinkedList<RecyclableScrollSlot<StageDto>> slotList = new LinkedList<RecyclableScrollSlot<StageDto>>();
    protected List<StageDto> dataList = new List<StageDto>();
    protected float itemHeight; // 슬롯 높이
    protected float itemWidth; // 슬롯 너비
    protected int poolSize; // 재사용할 슬롯 수
    protected int tmpfirstVisibleIndex; // 현재 첫번째로 보이는 아이템의 인덱스
    protected int contentVisibleSlotCount; // 현재 화면에 보이는 슬롯 개수

    [SerializeField] private int slotCount;

    void Start()
    {
        //if (bootstrap == null) bootstrap = FindObjectOfType<GameBootstrap>();

        List<StageDto> dataList = new List<StageDto>();
        if (bootstrap != null && bootstrap.StageRepo != null)
            dataList = bootstrap.StageRepo.GetAllStages();
        else
            dataList = new List<StageDto>(); // 안전 가드

        Init(dataList);
    }
    /// <summary>
    /// 초기 설정
    /// </summary>
    /// <param name="dataList"></param>
    public virtual void Init(List<StageDto> dataList)
    {
        this.dataList = dataList;

        RectTransform scrollRectTransform = this.scrollRect.GetComponent<RectTransform>();
        // 슬롯 크기
        itemHeight = itemPrefab.Height;
        itemWidth = itemPrefab.Width;

        // 전체 높이 계산
        int totalRows = Mathf.CeilToInt((float)dataList.Count / itemPerRow);
        float contentHeight = itemHeight * totalRows + ((totalRows - 1) * spacing) + topOffset + bottomOffset;

        //Anchor 고정
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.anchorMin = new Vector2(0f, 1f);

        // contentRect 높이 계산
        contentVisibleSlotCount = (int)(scrollRectTransform.rect.height / itemHeight) * itemPerRow;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

        // 슬롯 생성 및 리스트에 추가
        poolSize = contentVisibleSlotCount + (bufferCount * 2 * itemPerRow);
        int index = bufferCount * itemPerRow;
        for(int i = 0; i < poolSize; i++)
        {
            RecyclableScrollSlot<StageDto> item = Instantiate(itemPrefab, contentRect);
            slotList.AddLast(item);
            item.Init();
            UpdateSlot(item, index++);
        }
        scrollRect.onValueChanged.AddListener(OnScroll);
    }
    
    private void UpdateSlot(RecyclableScrollSlot<StageDto> item, int index)
    {
        // 현재 Index의 행과 열을 계산
        int row = 0 <= index ? index / itemPerRow : (index - 1) / itemPerRow;
        int column  = Mathf.Abs(index) % itemPerRow;

        // X축 Y축 위치 계산(가로 기준 중앙 정렬)
        Vector2 pivot = item.RectTransform.pivot;
        float totalWidth = (itemPerRow * (itemWidth + spacing)) - spacing;
        float contentWidth = contentRect.rect.width;
        float offsetX = (contentWidth - totalWidth) / 2f;
        float adjustedY = -(row * (itemHeight + spacing)) - itemHeight * (1 - pivot.y);
        float adjustedX = column * (itemWidth + spacing) + itemWidth * pivot.x;
        adjustedX += offsetX + hotizontalOffset;
        adjustedY -= topOffset;
        item.RectTransform.localPosition = new Vector3(adjustedX, adjustedY, 0);

        // index가 입력된 dataList의 크기를 넘어가거나 0미만이면 슬롯을 끄고 update진행 안 함
        if(index < 0 || index >= dataList.Count)
        {
            item.gameObject.SetActive(false);
            return;
        }
        else
        {
            item.UpdateSlot(dataList[index]);
            item.gameObject.SetActive(true);
        }
    }

    private void OnScroll(Vector2 scrollPosition)
    {
        float contentY = contentRect.anchoredPosition.y;

        // 현재 인덱스 위치 계산
        int firstVisibleRowIndex = Mathf.Max(0, Mathf.FloorToInt(contentY / (itemHeight + spacing)));
        int firstVisibleIndex = firstVisibleRowIndex * itemPerRow;

        // 만약 이전 위치와 현재 위치가 달라졌다면 슬롯 재배치
        if(tmpfirstVisibleIndex != firstVisibleIndex)
        {
            int diffIndex = (tmpfirstVisibleIndex - firstVisibleIndex) / itemPerRow;

            // 현재 인덱스가 더 클 경우
            if(diffIndex < 0)
            {
                int lastVisibleIndex = tmpfirstVisibleIndex + contentVisibleSlotCount;
                for(int i = 0, cnt = Mathf.Abs(diffIndex) * itemPerRow; i < cnt; i++)
                {
                    RecyclableScrollSlot<StageDto> item = slotList.First.Value;
                    slotList.RemoveFirst();
                    slotList.AddLast(item);

                    int newIndex = lastVisibleIndex + (bufferCount * itemPerRow) + i;
                    UpdateSlot(item, newIndex);
                }
            }
            // 이전 인덱스가 더 클 경우
            else if(diffIndex > 0)
            {
                for (int i = 0, cnt = Mathf.Abs(diffIndex) * itemPerRow; i < cnt; i++){
                    RecyclableScrollSlot<StageDto> item = slotList.Last.Value;
                    slotList.RemoveLast();
                    slotList.AddFirst(item);

                    int newIndex = tmpfirstVisibleIndex - (bufferCount * itemPerRow) - i;
                    UpdateSlot(item, newIndex);
                }
            }

            tmpfirstVisibleIndex = firstVisibleIndex;
        }
    }

    public void UpdateData(List<StageDto> dataList)
    {
        this.dataList = dataList;

        // 예비 슬롯 고려
        int index = tmpfirstVisibleIndex - bufferCount * itemPerRow;
        foreach(RecyclableScrollSlot<StageDto> item in slotList)
        {
            UpdateSlot(item, index);
            index++;
        }
    }
}
