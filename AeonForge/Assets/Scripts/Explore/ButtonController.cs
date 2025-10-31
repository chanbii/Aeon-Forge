using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ButtonController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject dimBackground;
    [SerializeField] private GameObject cardPanel;

    [Header("Anim")]
    [SerializeField] float dimTargetAlpha = 0.5f;
    [SerializeField] float openDuration = 0.25f;
    [SerializeField] float closeDuration = 0.2f;
    [SerializeField] Vector3 startScale = new Vector3(0.9f, 0.9f, 1f);
    void Start()
    {
        Application.targetFrameRate = 60;
    }

    public void EventCardButtonDown()
    {
        if (dimBackground) dimBackground.SetActive(true);
        if (cardPanel) cardPanel.SetActive(true);
    }
}
