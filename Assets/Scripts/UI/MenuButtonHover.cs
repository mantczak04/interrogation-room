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
    [Range(0f, 1f)]
    [SerializeField] private float hoverVolume = 0.18f;
    [Min(0f)]
    [SerializeField] private float hoverSoundCooldown = 0.06f;

    private static float lastHoverSoundTime = float.NegativeInfinity;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetRuntimeState()
    {
        lastHoverSoundTime = float.NegativeInfinity;
    }

    private void Start()
    {
        if (buttonText == null) buttonText = GetComponent<TextMeshProUGUI>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (indicatorText != null)
        {
            indicatorText.raycastTarget = false;
        }

        if (audioSource != null)
        {
            audioSource.playOnAwake = false;
            audioSource.spatialBlend = 0f;
            audioSource.volume = 1f;
        }
        
        SetNormalState();
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (buttonText != null)
        {
            buttonText.color = hoverColor;
        }
        if (indicatorText != null)
        {
            indicatorText.enabled = true;
        }

        float realtimeNow = Time.realtimeSinceStartup;
        float minimumInterval = hoverSound != null
            ? Mathf.Max(hoverSoundCooldown, hoverSound.length)
            : hoverSoundCooldown;
        if (hoverSound != null &&
            audioSource != null &&
            realtimeNow >= lastHoverSoundTime + minimumInterval)
        {
            audioSource.PlayOneShot(hoverSound, hoverVolume);
            lastHoverSoundTime = realtimeNow;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        SetNormalState();
    }

    private void SetNormalState()
    {
        if (buttonText != null)
        {
            buttonText.color = normalColor;
        }
        if (indicatorText != null)
        {
            indicatorText.enabled = false;
        }
    }
}
