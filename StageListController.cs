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
    [SerializeField] protected int bufferCount = 10; // �̸� �ε��� ������
    [SerializeField] bool autoSelectOnEnable = true;
    [SerializeField] protected float spacing; // ������ ���� ����
    [SerializeField] UnityEngine.UI.Button[] areaButtons; // ���� ��ư

    [Header("VerticalScrollView Option")]
    [SerializeField] protected int itemPerRow = 1; // �� �ٿ� ������ ������ ��
    [SerializeField] protected float topOffset;
    [SerializeField] protected float bottomOffset;
    [SerializeField] protected float hotizontalOffset;

    [Header("DB")]
    [SerializeField] private string databaseFileName = "aeonforge.db";

    protected LinkedList<RecyclableScrollSlot<StageDto>> slotList = new LinkedList<RecyclableScrollSlot<StageDto>>();
    protected List<StageDto> dataList = new List<StageDto>();
    protected float itemHeight; // ���� ����
    protected float itemWidth; // ���� �ʺ�
    protected int poolSize; // ������ ���� ��
    protected int tmpfirstVisibleIndex; // ���� ù��°�� ���̴� �������� �ε���
    protected int contentVisibleSlotCount; // ���� ȭ�鿡 ���̴� ���� ����

    [SerializeField] private int slotCount;

    void Start()
    {
        //if (bootstrap == null) bootstrap = FindObjectOfType<GameBootstrap>();

        List<StageDto> dataList = new List<StageDto>();
        if (bootstrap != null && bootstrap.StageRepo != null)
            dataList = bootstrap.StageRepo.GetAllStages();
        else
            dataList = new List<StageDto>(); // ���� ����

        Init(dataList);
    }
    /// <summary>
    /// �ʱ� ����
    /// </summary>
    /// <param name="dataList"></param>
    public virtual void Init(List<StageDto> dataList)
    {
        this.dataList = dataList;

        RectTransform scrollRectTransform = this.scrollRect.GetComponent<RectTransform>();
        // ���� ũ��
        itemHeight = itemPrefab.Height;
        itemWidth = itemPrefab.Width;

        // ��ü ���� ���
        int totalRows = Mathf.CeilToInt((float)dataList.Count / itemPerRow);
        float contentHeight = itemHeight * totalRows + ((totalRows - 1) * spacing) + topOffset + bottomOffset;

        //Anchor ����
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.anchorMin = new Vector2(0f, 1f);

        // contentRect ���� ���
        contentVisibleSlotCount = (int)(scrollRectTransform.rect.height / itemHeight) * itemPerRow;
        contentRect.sizeDelta = new Vector2(contentRect.sizeDelta.x, contentHeight);

        // ���� ���� �� ����Ʈ�� �߰�
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
        // ���� Index�� ��� ���� ���
        int row = 0 <= index ? index / itemPerRow : (index - 1) / itemPerRow;
        int column  = Mathf.Abs(index) % itemPerRow;

        // X�� Y�� ��ġ ���(���� ���� �߾� ����)
        Vector2 pivot = item.RectTransform.pivot;
        float totalWidth = (itemPerRow * (itemWidth + spacing)) - spacing;
        float contentWidth = contentRect.rect.width;
        float offsetX = (contentWidth - totalWidth) / 2f;
        float adjustedY = -(row * (itemHeight + spacing)) - itemHeight * (1 - pivot.y);
        float adjustedX = column * (itemWidth + spacing) + itemWidth * pivot.x;
        adjustedX += offsetX + hotizontalOffset;
        adjustedY -= topOffset;
        item.RectTransform.localPosition = new Vector3(adjustedX, adjustedY, 0);

        // index�� �Էµ� dataList�� ũ�⸦ �Ѿ�ų� 0�̸��̸� ������ ���� update���� �� ��
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

        // ���� �ε��� ��ġ ���
        int firstVisibleRowIndex = Mathf.Max(0, Mathf.FloorToInt(contentY / (itemHeight + spacing)));
        int firstVisibleIndex = firstVisibleRowIndex * itemPerRow;

        // ���� ���� ��ġ�� ���� ��ġ�� �޶����ٸ� ���� ���ġ
        if(tmpfirstVisibleIndex != firstVisibleIndex)
        {
            int diffIndex = (tmpfirstVisibleIndex - firstVisibleIndex) / itemPerRow;

            // ���� �ε����� �� Ŭ ���
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
            // ���� �ε����� �� Ŭ ���
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

        // ���� ���� ���
        int index = tmpfirstVisibleIndex - bufferCount * itemPerRow;
        foreach(RecyclableScrollSlot<StageDto> item in slotList)
        {
            UpdateSlot(item, index);
            index++;
        }
    }
}
