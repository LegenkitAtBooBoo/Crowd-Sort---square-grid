using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

public class CrowdTile : MonoBehaviour
{
    
    public List<Crowd> crowds /*{ get; private set; }*/ = new List<Crowd>();
    List<People> peoples = new List<People>();
    public bool Full => peoples.Count == 6;
    public bool Empty => crowds.Count == 0;
    public bool Complete => crowds.Count == 1 && Full;
    public Vector2Int CellID { get; private set; }
    public GridCell ParentCell { get; private set; } = null;

    public void InitTile(GridCell parent)
    {
        CellID = parent.CellID;
        ParentCell = parent;
        crowds.Clear();
        crowds = GameManager.NewCrowdSet();
        for (int i = 0; i < crowds.Count; i++)
        {
            for (int j = 0; j < crowds[i].amount; j++)
            {
                var p = PoolManager.RequestPeople(crowds[i].type).GetComponent<People>();
                peoples.Add(p);
                crowds[i].people.Add(p);
                peoples[peoples.Count - 1].SetPosition(transform, PositionOf(peoples.Count - 1));
            }
        }
    }
    public void MoveTile(GridCell parent)
    {
        ChangeParent(parent);
        StartCoroutine(RepositionTile());
    }
    public void ChangeParent(GridCell parent)
    {
        ParentCell.TileMoved(this);
        ParentCell = parent;
        CellID = parent.CellID;
    }
    Vector3 PositionOf(int ID)
    {
        int row = ID / 3;
        int col = ID % 3;
        return new Vector3(((float)col - 1f) * GameManager.instance.PeoplePadding.x, 0, (0.5f - row) * GameManager.instance.PeoplePadding.y);
    }

    public bool TakePeople(CrowdType type, People people)
    {
        if (Full)
        {
            return false;
        }
        bool added = false;
        foreach (var v in crowds)
        {
            if (v.type == type)
            {
                v.amount++;
                v.people.Add(people);
                peoples.Add(people);
                added = true;
            }
        }
        if (!added)
        {
            var c = new Crowd(type, 1);
            c.people.Add(people);
            peoples.Add(people);
            crowds.Add(c);

        }        
        return true;
    }
    public People RemovePeople(CrowdType type)
    {
        if (Complete)
        {
            return null;
        }
        foreach (var v in crowds)
        {
            if (v.type == type)
            {
                v.amount--;
                var p = v.people[0];
                v.people.RemoveAt(0);
                peoples.Remove(p);
                if (v.amount == 0)
                {
                    crowds.Remove(v);
                }
                return p;
            }
        }
        return null;
    }

    public void RepositionPeople()
    {
        peoples.Clear();
        foreach (var p in crowds)
        {
            peoples.AddRange(p.people);
        }
        for (int i = 0; i < peoples.Count; i++)
        {
            peoples[i].MoveTo(transform, PositionOf(i));
        }
    }
    public void CheckStatus()
    {
        if (Empty)
        {
            this.waitFrame(() =>
            {
                DestroySelf();
            }, 2);
        }
        if (Complete)
        {
            GameManager.instance.TileCompleted(this);            
        }
    }
    void DestroySelf()
    {
        ParentCell.TileMoved(this);
        PoolManager.ReturnPeople(peoples);
        peoples.Clear();
        //gameObject.SetActive(false);
        Destroy(gameObject);
    }

    IEnumerator RepositionTile()
    {
        Vector3 start = transform.localPosition;
        float dis = start.magnitude;
        float duration = dis * 0.1f;
        float t = 0;
        while (t < 1)
        {
            Vector3 pos = Vector3.Lerp(start, Vector3.zero, t);
            transform.localPosition = pos;
            foreach (var v in peoples)
            {
                v.transform.LookAt(transform.parent);
            }
            t += Time.deltaTime / duration;
            yield return null;
        }
        transform.localPosition = Vector3.zero;
        transform.localRotation = Quaternion.Euler(0, 0, 0);
        GameManager.instance.CheckForShorting(this);
    }


    public bool HaveCrowd(CrowdType type)
    {
        foreach (var v in crowds)
        {
            if (v.type == type)
            {
                return true;
            }
        }
        return false;
    }
}
