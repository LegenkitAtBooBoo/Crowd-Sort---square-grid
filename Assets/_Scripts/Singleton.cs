using UnityEngine;

public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T instance { get; set; }


    protected virtual void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        instance = this as T;
    }
    /*private void OnDestroy()
    {
        if (instance == this)
        {
            instance = null;
        }
    }*/
}
