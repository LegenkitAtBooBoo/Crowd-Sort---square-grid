using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridCell : MonoBehaviour
{
    [SerializeField] MeshRenderer Renderer;
    #region Properties
    [SerializeField] Vector2Int cellID;
    public Vector2Int CellID { get => cellID; set => cellID = value; }
    Material Mat => Renderer.material;

    public CrowdTile Tile { get; set; } = null;
    public bool Available => Tile == null;
    #endregion

    public Vector2Int SetID(Vector2 CellSize, Vector2 offset)
    {
        CellID = new Vector2Int(Mathf.RoundToInt((transform.position.x-offset.x) / CellSize.x), Mathf.RoundToInt((transform.position.z - offset.y) / CellSize.y));
        return CellID;
    }
    public bool SpawnCrowdTile(CrowdTile tile)
    {
        if (!Available) return false;
        Mat.SetColor("_BaseColor", (Color.gray));
        transform.localScale = Vector3.one;
        Tile = tile;
        tile.transform.parent = transform;
        tile.transform.localPosition = Vector3.zero;
        Tile.transform.localRotation = Quaternion.Euler(0, 0, 0);
        Tile.InitTile(this);
        return true;
    }
    public bool TakeCrowdTile(CrowdTile tile)
    {
        if (!Available) return false;
        Mat.SetColor("_BaseColor", (Color.gray));
        transform.localScale = Vector3.one;
        Tile = tile;
        tile.transform.parent = transform;
        Tile.MoveTile(this);
        return true;
    }
    public void TileMoved(CrowdTile tile)
    {
        if(Tile != tile)
        {
            return;
        }
        Tile = null;
        Mat.SetColor("_BaseColor", (Color.white));
        transform.localScale = Vector3.one;

    }
    public CrowdTile ReplaceTile(CrowdTile tile)
    {
        var v = Tile;
        Tile = null;
        TakeCrowdTile(tile);
        return v;
    }
    public void ChangeColor(bool active)
    {
        if (!Available)
        {
            return;
        }
        Mat.SetColor("_BaseColor", (active ? Color.yellow : Color.white));
        transform.localScale = Vector3.one * (active ? 1.1f : 1);
    }

    public bool DoesTileContain(CrowdType type)
    {
        if(Tile == null)
        {
            return false;
        }
        return Tile.HaveCrowd(type);
    }

}
