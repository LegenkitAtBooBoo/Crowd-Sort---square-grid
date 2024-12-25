using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PoolManager : Singleton<PoolManager>
{
    public GameObject People;
    public List<Material> PeopleMat;

    List<GameObject> PeopleList = new List<GameObject>();

    public static GameObject RequestPeople(CrowdType type)
    {
        foreach (var p in instance.PeopleList)
        {
            if (!p.activeSelf)
            {
                p.gameObject.SetActive(true);
                p.GetComponent<People>().Init(type);
                return p;
            }
        }
        instance.PeopleList.Add(Instantiate(instance.People, instance.transform));
        instance.PeopleList[instance.PeopleList.Count - 1].GetComponent<People>().Init(type);
        return instance.PeopleList[instance.PeopleList.Count - 1];
    }
    public static Material GetMaterial(CrowdType type)
    {
        return instance.PeopleMat[(int)type];
    }

    public static void ReturnPeople(List<People> peoples)
    {
        foreach (var v in peoples)
        {
            v.gameObject.SetActive(false);
            v.transform.parent = instance.transform;
        }
    }
}
