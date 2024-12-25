using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class People : MonoBehaviour
{
    public MeshRenderer PeopleRenderer;
    public CrowdType Type {  get; private set; }
    
    public void Init(CrowdType type)
    {
        Type = type;
        PeopleRenderer.material = PoolManager.GetMaterial(type);
    }
    public void SetPosition(Transform parent, Vector3 position)
    {
        transform.parent = parent;
        transform.localPosition = position;
    }
    public void MoveTo(Transform parent,Vector3 position)
    {
        transform.parent = parent;
        StartCoroutine(Reposition(position));
    }
    public void MoveTo(Vector3 position)
    {
        StartCoroutine(Reposition(position));
    }
    IEnumerator Reposition(Vector3 target)
    {
        Vector3 start = transform.localPosition;
        float dis = start.magnitude;
        float duration = dis * 0.1f;
        float t = 0;
        while (t < 1)
        {
            Vector3 pos = Vector3.Lerp(start, target, t);
            transform.localPosition = pos;
            transform.LookAt(transform.parent,Vector3.up);
            t += Time.deltaTime / duration;
            yield return null;
        }
        transform.localPosition = target;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
    }
}
