using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class MenuButtonHover : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI buttonText;
    [SerializeField] private TextMeshProUGUI indicatorText; // Kropka "•" obok tekstu

    [Header("Colors")]
    [SerializeField] private Color normalColor = new Color(0.7f, 0.7f, 0.7f, 1f); // Jasnoszary
    [SerializeField] private Color hoverColor = Color.white;

    [Header("Audio")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioSource audioSource;

    private void Start()
    {
        if (buttonText == null) buttonText = GetComponent<TextMeshProUGUI>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        
        SetNormalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        buttonText.color = hoverColor;
        if (indicatorText != null)
        {
            indicatorText.enabled = true;
        }

        if (hoverSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(hoverSound);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormalState();
    }

    private void SetNormalState()
    {
        buttonText.color = normalColor;
        if (indicatorText != null)
        {
            indicatorText.enabled = false;
        }
    }
}
