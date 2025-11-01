using UnityEngine;
using UnityEngine.UI;

public class ImagePanelController : MonoBehaviour
{
    [SerializeField] private GameObject imagePanel; // 띄울 이미지 패널
    [SerializeField] private Button openButton;     // 여는 버튼
    [SerializeField] private Button closeButton;    // 닫는 버튼

    private void Start()
    {
        // 초기 상태: 이미지 창 비활성화
        imagePanel.SetActive(false);

        // 버튼 클릭 이벤트 등록
        openButton.onClick.AddListener(OpenPanel);
        closeButton.onClick.AddListener(ClosePanel);
    }

    private void OpenPanel()
    {
        imagePanel.SetActive(true);
    }

    private void ClosePanel()
    {
        imagePanel.SetActive(false);
    }
}
