using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(AudioLowPassFilter))]
public sealed class VivoxVoiceOcclusion : MonoBehaviour
{
    private const float ClearCutoff = 22000f;
    private const float OccludedCutoff = 1800f;
    private const float OccludedVolume = 0.25f;
    private const float UpdateInterval = 0.1f;

    private readonly RaycastHit[] hits = new RaycastHit[16];

    private Transform listener;
    private Transform speaker;
    private AudioSource audioSource;
    private AudioLowPassFilter lowPassFilter;
    private LayerMask occlusionMask;
    private float nextUpdate;

    public void Configure(
        Transform listenerTransform,
        Transform speakerTransform,
        AudioSource source,
        LayerMask mask)
    {
        listener = listenerTransform;
        speaker = speakerTransform;
        audioSource = source;
        occlusionMask = mask;
        lowPassFilter = GetComponent<AudioLowPassFilter>();
        ApplyOcclusion(false);
    }

    private void Update()
    {
        if (listener == null || speaker == null || audioSource == null || Time.unscaledTime < nextUpdate)
        {
            return;
        }

        nextUpdate = Time.unscaledTime + UpdateInterval;
        Vector3 origin = listener.position;
        Vector3 direction = speaker.position - origin;
        float distance = direction.magnitude;

        if (distance <= Mathf.Epsilon)
        {
            ApplyOcclusion(false);
            return;
        }

        int hitCount = Physics.RaycastNonAlloc(
            origin,
            direction / distance,
            hits,
            distance,
            occlusionMask,
            QueryTriggerInteraction.Ignore);

        bool isOccluded = false;
        for (int index = 0; index < hitCount; index++)
        {
            Transform hitTransform = hits[index].transform;
            if (hitTransform == null || hitTransform.IsChildOf(listener) || hitTransform.IsChildOf(speaker))
            {
                continue;
            }

            isOccluded = true;
            break;
        }

        ApplyOcclusion(isOccluded);
    }

    private void ApplyOcclusion(bool isOccluded)
    {
        audioSource.volume = isOccluded ? OccludedVolume : 1f;
        lowPassFilter.cutoffFrequency = isOccluded ? OccludedCutoff : ClearCutoff;
    }
}
