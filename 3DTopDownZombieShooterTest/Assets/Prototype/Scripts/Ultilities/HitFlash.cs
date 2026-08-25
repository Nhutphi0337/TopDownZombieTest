using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    [SerializeField] private float duration = 0.08f;

    private SkinnedMeshRenderer[] renderers;
    private MaterialPropertyBlock propertyBlock;

    private Coroutine flashCoroutine;

    private static readonly int HitFlashProperty =
        Shader.PropertyToID("_HitFlash");

    private void Awake()
    {
        renderers =
            GetComponentsInChildren<SkinnedMeshRenderer>();

        propertyBlock =
            new MaterialPropertyBlock();

        SetFlash(0f);
    }

    public void Play()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
        }

        flashCoroutine =
            StartCoroutine(FlashCoroutine());
    }

    public void ResetFlash()
    {
        if (flashCoroutine != null)
        {
            StopCoroutine(flashCoroutine);
            flashCoroutine = null;
        }

        SetFlash(0f);
    }

    private IEnumerator FlashCoroutine()
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;

            float t = elapsed / duration;

            SetFlash(1f - t);

            yield return null;
        }

        SetFlash(0f);

        flashCoroutine = null;
    }

    private void SetFlash(float value)
    {
        foreach (SkinnedMeshRenderer renderer in renderers)
        {
            renderer.GetPropertyBlock(propertyBlock);

            propertyBlock.SetFloat(
                HitFlashProperty,
                value);

            renderer.SetPropertyBlock(propertyBlock);
        }
    }
}