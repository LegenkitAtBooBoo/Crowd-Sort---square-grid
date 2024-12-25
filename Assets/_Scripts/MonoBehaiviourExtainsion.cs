using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public static class MonoBehaiviourExtainsion
{
    public static Coroutine wait(this MonoBehaviour mono, UnityAction callback, float delay, float interval = 0)
    {
        if (callback != null)
        {
            return mono.StartCoroutine(ExecuteAction(callback, delay, interval)); 
        }
        return null;
    }

    public static IEnumerator ExecuteAction(UnityAction callback, float delay, float interval = 0)
    {
        yield return new WaitForSeconds(delay);
        if (interval > 0)
        {
            while (callback != null)
            {
                callback.Invoke();
                yield return new WaitForSeconds(interval);
            }
        }


        callback.Invoke();
        yield break;
    }

    public static Coroutine waitFrame(this MonoBehaviour mono, UnityAction callback, int Frames = 1)
    {
        if (callback != null)
        {
            return  mono.StartCoroutine(ExecuteActionFrame(callback, Frames));
        }
        return null;
    }

    public static IEnumerator ExecuteActionFrame(UnityAction callback, int frames)
    {
        while (frames > 0)
        {
            frames--;
            yield return new WaitForEndOfFrame();
        }
        callback.Invoke();
        yield break;
    }
    //______________________________________________________________________________________________________________

    public static bool Bool(this MonoBehaviour mono, int value)
    {
        return value >= 1;
    }
}
