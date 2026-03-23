using UnityEngine;
using System.Collections;

/// <summary>
/// Tự động trả object về pool sau delay.
/// Được thêm tự động bởi ObjectPool.
/// </summary>
public class AutoReturn : MonoBehaviour
{
    [HideInInspector] public string poolName;

    Coroutine returnRoutine;

    public void ReturnAfterDelay(float delay)
    {
        if (returnRoutine != null)
            StopCoroutine(returnRoutine);
        returnRoutine = StartCoroutine(ReturnCoroutine(delay));
    }

    IEnumerator ReturnCoroutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (ObjectPool.Instance != null)
            ObjectPool.Instance.ReturnToPool(gameObject);
        else
            Destroy(gameObject);
    }

    void OnDisable()
    {
        if (returnRoutine != null)
        {
            StopCoroutine(returnRoutine);
            returnRoutine = null;
        }
    }
}
