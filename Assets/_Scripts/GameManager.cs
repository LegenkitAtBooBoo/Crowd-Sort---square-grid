using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEditor.iOS;
using UnityEngine;

[DefaultExecutionOrder(-1000)]
public class GameManager : Singleton<GameManager>
{
    #region Set
    public Vector2Int[] NeighboutSet =
    {
        new Vector2Int( 0,-1),
        new Vector2Int(-1, 0),
        new Vector2Int( 0, 1),
        new Vector2Int( 1, 0)
    };
    #endregion

    #region Var
    [Header("World")]
    public Vector2Int GridSize;
    public Vector2 CellSize;
    public Vector2 PeoplePadding;
    [SerializeField] GameObject CellObj;
    [SerializeField] GameObject crowdTile;
    GridCell GeneratorCell;
    GridCell ProviderCell;
    #endregion

    #region Colleciton
    [Header("Level")]
    public List<CrowdType> CrowdsInLevel = new List<CrowdType>();
    public Dictionary<Vector2Int, GridCell> CellDic = new Dictionary<Vector2Int, GridCell>();

    public List<GridCell> StorageCells = new List<GridCell>();
    public List<CrowdTile> CompletedTile = new List<CrowdTile>();
    #endregion

    #region Properties
    public Vector2 PositionOffset;

    Vector3 MousePosition
    {
        get
        {
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                ValidHover = true;
                return hit.point;
            }
            ValidHover = false;
            return Vector3.zero;
        }
    }
    bool ValidHover { get; set; } = false;
    [SerializeField] Vector2Int posID;
    public Vector2Int PosID
    {
        get
        {
            posID.x = Mathf.RoundToInt((MousePosition.x - PositionOffset.x) / CellSize.x);
            posID.y = Mathf.RoundToInt((MousePosition.z - PositionOffset.y) / CellSize.y);
            return posID;
        }
    }

    GridCell HoveredCell { get; set; } = null;
    CrowdTile ReadyTile => ProviderCell.Tile;
    CrowdTile GeneratedTile => GeneratorCell.Tile;
    #endregion

    #region Initialization
    protected override void Awake()
    {
        base.Awake();
        GenerateGrid();
        InitializeGrid();
    }
    void GenerateGrid()
    {
        PositionOffset.x = (GridSize.x % 2 == 0) ? CellSize.x / 2 : 0;
        PositionOffset.y = (GridSize.y % 2 == 0) ? CellSize.y / 2 : 0;
        Vector3 pos;
        for (int i = 0; i < GridSize.x; i++)
        {
            for (int j = 0; j < GridSize.y; j++)
            {
                pos = new Vector3((i - GridSize.x / 2) * CellSize.x, 0, (j - GridSize.y / 2) * CellSize.y + PositionOffset.y);
                Instantiate(CellObj, transform).transform.localPosition = pos;
            }
        }
    }
    void InitializeGrid()
    {
        foreach (var v in FindObjectsOfType<GridCell>())
        {
            CellDic.Add(v.SetID(CellSize, PositionOffset), v);
            Debug.Log(CellDic.Count + " - " + v.CellID.ToString());
        }
    }

    public void SubscribeCells(GridCell cell)
    {
        if (cell.Type == CellType.Storage)
        {
            StorageCells.Add(cell);
        }
        if (cell.Type == CellType.Generator)
        {
            if (cell.SetID(CellSize, PositionOffset).x == 0)
            {
                ProviderCell = cell;
            }
            else
            {
                GeneratorCell = cell;
            }
        }
    }
    private void Start()
    {
        SpawnTile(true);
        StorageCells.Sort((a, b) => a.CellID.x - b.CellID.x);
    }

    #endregion

    #region Updation
    private void Update()
    {
        UpdateCellColor();

        if (Input.GetMouseButtonDown(0))
        {
            MoveTileTo(PosID);
        }
        if (Input.GetKeyDown(KeyCode.R))
        {
            SwitchGeneratedTiles();
        }

    }
    void UpdateCellColor()
    {
        if (!CellDic.ContainsKey(PosID) || HoveredCell == CellDic[PosID] || !ValidHover)
        {
            return;
        }
        HoveredCell?.ChangeColor(false);
        HoveredCell = CellDic[PosID];
        HoveredCell.ChangeColor(true);
    }
    void SpawnTile(bool first = false)
    {
        GeneratorCell.SpawnCrowdTile(Instantiate(crowdTile, transform).GetComponent<CrowdTile>());
        if (first)
        {
            ProviderCell.TakeCrowdTile(GeneratedTile);
            GeneratorCell.SpawnCrowdTile(Instantiate(crowdTile, transform).GetComponent<CrowdTile>());
        }
    }
    void MoveTileTo(Vector2Int ID)
    {
        if (!CellDic.ContainsKey(ID) || CellDic[ID].Type != CellType.Normal)
        {
            return;
        }

        if (CellDic[ID].TakeCrowdTile(ReadyTile))
        {
            if (GeneratedTile == null)
            {
                SpawnTile();
            }
            ProviderCell.TakeCrowdTile(GeneratedTile);
            this.wait(() =>
            {
                SpawnTile();
            }, 0.5f);
        }
    }

    #endregion

    #region Shorting
    public void CheckForShorting(CrowdTile tile)
    {
        List<CrowdType> tempTypes = new List<CrowdType>();

        for (int i = 0; i < tile.crowds.Count; i++)
        {
            tempTypes.Add(tile.crowds[i].type);
        }
        Vector2Int ID = tile.CellID;
        for (int i = 0; i < tempTypes.Count; i++)
        {
            checkNeighbourForCrowd(ID, tempTypes[i]);
        }
        tile.RepositionPeople();
        tile.CheckStatus();
        Vector2Int neighbourID;
        foreach (var v in NeighboutSet)
        {
            neighbourID = ID + v;
            if (!CellDic.ContainsKey(neighbourID))
            {
                continue;
            }
            if (CellDic[neighbourID].Tile != null)
            {
                CellDic[neighbourID].Tile.RepositionPeople();
                CellDic[neighbourID].Tile.CheckStatus();
            }
        }
    }
    void checkNeighbourForCrowd(Vector2Int ID, CrowdType type)
    {
        List<CrowdTile> AplicableTiles = new List<CrowdTile>();
        if (CellDic[ID].DoesTileContain(type))
        {
            AplicableTiles.Add(CellDic[ID].Tile);
        }
        Vector2Int neighbourID;
        foreach (var v in NeighboutSet)
        {
            neighbourID = ID + v;
            if (!CellDic.ContainsKey(neighbourID))
            {
                continue;
            }
            if (CellDic[neighbourID].DoesTileContain(type))
            {
                AplicableTiles.Add(CellDic[neighbourID].Tile);
            }
        }
        int ApplicantCount = AplicableTiles.Count;
        if (ApplicantCount == 1)
        {
            return;
        }

        for (int i = 0; i < ApplicantCount; i++)
        {
            for (int j = i + 1; j < ApplicantCount; j++)
            {
                if (PriorityForExchange(AplicableTiles[j], AplicableTiles[i], CellDic[ID].Tile, type))
                {
                    var temp = AplicableTiles[i];
                    AplicableTiles[i] = AplicableTiles[j];
                    AplicableTiles[j] = temp;
                }
            }
        }
        /*CrowdTile DominentTile = AplicableTiles[0];
        AplicableTiles.RemoveAt(0);

        if (DominentTile != CellDic[ID].Tile)
        {
            AplicableTiles.Remove(CellDic[ID].Tile);
            AplicableTiles.Insert(0, CellDic[ID].Tile);
        }
        DominentTile.TakeCrowdFrom(AplicableTiles, type);*/
        Sort(AplicableTiles, CellDic[ID].Tile, type);
    }
    bool PriorityForExchange(CrowdTile cell1, CrowdTile cell2, CrowdTile mainCell, CrowdType type)
    {
        int crowd1 = cell1.crowds.Count;
        int crowd2 = cell2.crowds.Count;
        if (crowd1 < crowd2)
        {
            return true;
        }
        if (crowd1 == 1 && crowd2 == 1 && cell2 == mainCell)
        {
            return true;
        }
        if (crowd1 == 1 && crowd2 == 1 && cell1.crowds[0].amount > cell2.crowds[0].amount)
        {
            return true;
        }
        return false;
    }

    void Sort(List<CrowdTile> tiles, CrowdTile bridge, CrowdType type)
    {
        int TileCount = tiles.Count;
        int giveIndex = TileCount - 1;
        People temp;
        for (int i = 0; i < TileCount && giveIndex > i;)
        {
            temp = tiles[giveIndex].RemovePeople(type);
            if (temp == null)
            {
                giveIndex--;
                continue;
            }
            if (!tiles[i].TakePeople(type, temp))
            {
                i++;
                tiles[giveIndex].TakePeople(type, temp);
            }
        }
    }
    #endregion

    #region Public Methods
    public void SwitchGeneratedTiles()
    {
        ProviderCell.ReplaceTile(GeneratorCell.ReplaceTile(ReadyTile));
    }
    public static List<Crowd> NewCrowdSet()
    {
        List<Crowd> Temp = new List<Crowd>();
        int Length = Random.Range(1, 6);
        int Count = Random.Range(1, Mathf.Min(6, Length, instance.CrowdsInLevel.Count + 1));
        int populationPerCrowd = Length / Count;
        int remainingLength = Length - populationPerCrowd * Count;
        List<CrowdType> TempCrowd = new List<CrowdType>();
        TempCrowd.AddRange(instance.CrowdsInLevel);
        for (int i = 0; i < Count; i++)
        {
            int x = Random.Range(0, TempCrowd.Count);
            Crowd crowd = new Crowd(TempCrowd[x], populationPerCrowd);
            if (remainingLength > 0)
            {
                crowd++;
                remainingLength--;
            }
            Temp.Add(crowd);
            TempCrowd.RemoveAt(x);
        }
        return Temp;
    }

    public void TileCompleted(CrowdTile tile)
    {
        if (CompletedTile.Count == StorageCells.Count || CompletedTile.Contains(tile))
        {
            return;
        }
        StorageCells[CompletedTile.Count].TakeCrowdTile(tile);
        CompletedTile.Add(tile);
    }
    #endregion
}

[System.Serializable]
public class Crowd
{
    public CrowdType type;
    public List<People> people = new List<People>();
    public int amount;
    public Crowd()
    {

    }
    public Crowd(CrowdType type, int amount)
    {
        this.type = type;
        this.amount = amount;
    }
    public Crowd(Crowd crowd)
    {
        this.type = crowd.type;
        this.amount = crowd.amount;
    }
    public static Crowd operator +(Crowd crowd1, Crowd crowd2)
    {
        crowd1.amount += crowd2.amount;
        return crowd1;
    }
    public static Crowd operator ++(Crowd crowd1)
    {
        crowd1.amount++;
        return crowd1;
    }
    public static Crowd operator -(Crowd crowd1, Crowd crowd2)
    {
        crowd1.amount -= crowd2.amount;
        return crowd1;
    }
}