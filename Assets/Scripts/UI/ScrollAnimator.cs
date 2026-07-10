using System.Collections;
using UnityEngine;

public class ScrollAnimator : MonoBehaviour
{
    [Range(0.1f, 1f)] public float animationDuration = 0.35f;
    public AnimationCurve easeCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    public float AnimationDuration => animationDuration;

    private Coroutine _current;

    public void AnimateIn()
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ScaleTo(Vector3.zero, Vector3.one));
    }

    public void AnimateOut()
    {
        if (_current != null) StopCoroutine(_current);
        _current = StartCoroutine(ScaleTo(transform.localScale, Vector3.zero));
    }

    IEnumerator ScaleTo(Vector3 from, Vector3 to)
    {
        float elapsed = 0f;
        while (elapsed < animationDuration)
        {
            elapsed += Time.deltaTime;
            float t = easeCurve.Evaluate(Mathf.Clamp01(elapsed / animationDuration));
            transform.localScale = Vector3.Lerp(from, to, t);
            yield return null;
        }
        transform.localScale = to;
    }
}
