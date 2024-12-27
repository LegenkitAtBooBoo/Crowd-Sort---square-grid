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
    public Vector3Int BoatCount;
    [SerializeField] GameObject CellObj;
    [SerializeField] GameObject crowdTile;
    #endregion

    #region Colleciton
    [Header("Level")]
    public List<CrowdType> CrowdsInLevel = new List<CrowdType>();
    public Dictionary<Vector2Int, GridCell> CellDic = new Dictionary<Vector2Int, GridCell>();

    public List<GridCell> StorageCells = new List<GridCell>();
    public List<CrowdTile> CompletedTile = new List<CrowdTile>();
    public List<CrowdType> LevelRequirements = new List<CrowdType>();
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

    GridCell LeftBoat { get; set; }
    GridCell RightBoat { get; set; }
    GridCell GeneratorCell { get; set; }
    GridCell ProviderCell { get; set; }
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
        if (cell.Type == CellType.Boat)
        {
            if (cell.SetID(CellSize, PositionOffset).x < 0)
            {
                LeftBoat = cell;
            }
            else
            {
                RightBoat = cell;
            }
        }
    }
    private void Start()
    {
        GenerateLevel();
        SpawnTile(true);
        StorageCells.Sort((a, b) => a.CellID.x - b.CellID.x);
    }
    void GenerateLevel()
    {
        LevelRequirements.Clear();
        int x = Random.Range(BoatCount.x, BoatCount.y + 1);
        x -= x % BoatCount.z;
        for (int y = 0; y < x; y++)
        {
            LevelRequirements.Add(CrowdsInLevel[Random.Range(0, CrowdsInLevel.Count)]);
        }
        ChangeBoats();
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
    public void CheckForShorting(CrowdTile tile, bool first = true)
    {
        tile.setPriority(CrowdType.none);
        Vector2Int ID = tile.CellID;
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
                CellDic[neighbourID].Tile.setPriority(CrowdType.none);
            }
        }

        List<CrowdType> tempTypes = new List<CrowdType>();

        for (int i = 0; i < tile.crowds.Count; i++)
        {
            tempTypes.Add(tile.crowds[i].type);
        }
        for (int i = 0; i < tempTypes.Count; i++)
        {
            checkNeighbourForCrowd(ID, tempTypes[i]);
        }
        if (!first)
        {
            tile.RepositionPeople();
            tile.CheckStatus();
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

        if (first)
        {
            this.waitFrame(() =>
            {
                CheckForShorting(tile, false);
            }, 2);
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
        AplicableTiles[0].setPriority(type);
        Sort(AplicableTiles, CellDic[ID].Tile, type);
    }
    bool PriorityForExchange(CrowdTile cell1, CrowdTile cell2, CrowdTile mainCell, CrowdType type)
    {
        int crowd1 = cell1.crowds.Count;
        int crowd2 = cell2.crowds.Count;
        if (crowd1 < crowd2 && cell1.Priority == CrowdType.none)
        {
            return true;
        }
        if (crowd1 == crowd2)
        {
            if (crowd1 == 1)
            {
                if (cell2 == mainCell)
                {
                    return true;
                }
                if (cell1.crowds[0].amount > cell2.crowds[0].amount)
                {
                    return true;
                }
            }
            if (cell1.Priority == CrowdType.none)
            {
                return true;
            }
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

    public void ChangeBoats()
    {
        if (LeftBoat.BoatType == CrowdType.none && LevelRequirements.Count>0)
        {
            LeftBoat.InitializeBoat(LevelRequirements[0]);
            LevelRequirements.RemoveAt(0);
        }
        if (RightBoat.BoatType == CrowdType.none && LevelRequirements.Count>0)
        {
            RightBoat.InitializeBoat(LevelRequirements[0]);
            LevelRequirements.RemoveAt(0);
        }
        LeftBoat.gameObject.SetActive(LeftBoat.BoatType != CrowdType.none);
        RightBoat.gameObject.SetActive(RightBoat.BoatType != CrowdType.none);
        FillBoatFromStorage();
    }

    public void TileCompleted(CrowdTile tile)
    {
        if (CompletedTile.Count == StorageCells.Count || CompletedTile.Contains(tile))
        {
            return;
        }
        if (FillUpBoat(tile))
        {
            return;
        }
        if (!FillStorage(tile))
        {
            // level fail
        }
    }
    bool FillUpBoat(CrowdTile tile)
    {
        if (LeftBoat.BoatType == tile.crowds[0].type)
        {
            LeftBoat.TakeCrowdTile(tile);
            return true;
        }
        if (RightBoat.BoatType == tile.crowds[0].type)
        {
            RightBoat.TakeCrowdTile(tile);
            return true;
        }
        return false;
    }
    bool FillStorage(CrowdTile tile)
    {
        foreach (var cell in StorageCells)
        {
            if (cell.Available)
            {
                cell.TakeCrowdTile(tile);
                return true;
            }
        }
        return false;
    }

    void FillBoatFromStorage()
    {
        foreach (var cell in StorageCells)
        {
            if (!cell.Available)
            {
                FillUpBoat(cell.Tile);
            }
        }
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