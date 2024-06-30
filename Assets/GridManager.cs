using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using Random = UnityEngine.Random;

public class DTile
{
    public bool isWall;
    public int cost;
    public Color colour;

    public DTile(bool w, int c, Color col)
    {
        isWall = w;
        cost = c;
        colour = col;
    }
}


public class GridManager : MonoBehaviour
{
    public static GridManager Instance;
    
    [SerializeField] private GameObject tilePrefab;
    [SerializeField, Range(16, 128), Tooltip("Size of grid. Grid = (N+1)^2")] 
    private int finalSize = 64;
    [SerializeField, Tooltip("Number of walls tiles removed at random.")] 
    private int randomPasses = 23;
    [SerializeField, Range(0f, 1f), Tooltip("Weighted random removal that targets fractal edges.")] 
    private float powerTwoPassCoef = 0.02f;
    [SerializeField, Tooltip("Removes dead-ends. Number of unbiased passes over entire grid. I don't think if biasing matters here, but I prevented it anyway.")] 
    private int deadEndRemovalPasses = 2; 
    [SerializeField, Tooltip("Tile scale.")] 
    private float spacing = 0.2f;
    [SerializeField, Tooltip("Revert to base Fractal Maze algorithm.")] 
    private bool trueFractal = false;
    [SerializeField, Tooltip("Use 5 space per second move speed.")] 
    private bool flatMoveSpeed = false;
    [SerializeField] private bool debug = false;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    [SerializeField] private bool disableRandomPasses = false;
    [SerializeField] private bool disableRotations = false;
    [SerializeField] private bool disableTransposes = false;
    [SerializeField] private bool disableDeadEndRemoval = false;
    private List<int> explored = new List<int>();
    public bool Debug { get { return debug; } private set {}}
    private List<int> removableWalls = new List<int>();
    private Vector2Int playerPos = new Vector2Int(1, 1);
    private Vector2Int enemyPos;
    private List<DTile> finalTiles = new List<DTile>();
    private Player p;
    private DijkstraEnemy d;
    
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        enemyPos = new Vector2Int(finalSize - 1, finalSize / 4 * 3 - 1);
        List<bool> firstTiles = new List<bool>();
        firstTiles.Capacity = 16;
        
        for (int y = 0; y < 4; y++)
            for (int x = 0; x < 4; x++)
            {
                if(y % 2 == 0 || x % 2 == 0)
                    firstTiles.Add(true);
                else
                    firstTiles.Add(false);
            }
        
        List<int> firstDoors = new List<int>{ 6, 9, 11, 14 };

        firstDoors.RemoveAt(Random.Range(0, 4));
        
        foreach (var door in firstDoors)
            firstTiles[door] = false;

        List<bool> fractalTiles = fractalTiles = Fractal(firstTiles, 4);

        if (!trueFractal && !disableRandomPasses)
        {
            PowerTwoPass(ref fractalTiles);
            RandomPass(ref fractalTiles);
        }

        ThreeQuarterChop(ref fractalTiles);
        
        List<Vector2Int> voronoiNodes = GenerateVoronoiNodes();
        
        finalTiles.Capacity = finalSize + 1 * finalSize + 1;
        for(int y = 0; y < finalSize / 4 * 3 + 1; y++)
            for(int x = 0; x < finalSize + 1; x++)
                if((x == finalSize || y == finalSize / 4 * 3) || fractalTiles[y * finalSize + x])
                    finalTiles.Add(new DTile(true, Int32.MaxValue, Color.black));
                else
                    if(debug)
                        finalTiles.Add(new DTile(false, (int)Mathf.Round(DistToNearestVNode(voronoiNodes, new Vector2Int(x,y))), 
                                new Color(1, 
                                    0.5f, 
                                    1)
                            )
                        );
                    else
                        finalTiles.Add(new DTile(false, (int)Mathf.Round(DistToNearestVNode(voronoiNodes, new Vector2Int(x,y))), 
                                new Color(1 - DistToNearestVNode(voronoiNodes, new Vector2Int(x,y)) / 33.94f, 
                                    1 - DistToNearestVNode(voronoiNodes, new Vector2Int(x,y)) / 33.94f / 2, 
                                    0.5f + DistToNearestVNode(voronoiNodes, new Vector2Int(x,y)) / 33.94f / 2)
                            )
                        );
        
        if(!trueFractal && !disableDeadEndRemoval)
            for (int i = 0; i < deadEndRemovalPasses; i++)
                DeadEndRemoval(ref finalTiles);
    }
        
    private void Start()
    {
        for (int i = 0; i < (finalSize / 4 * 3 + 1) * (finalSize + 1); i++)
        {
            Vector2 location;
            location.x = (i % (finalSize + 1)) * spacing;
            location.y = (i / (finalSize + 1)) * spacing;
            GameObject t = Instantiate(tilePrefab, transform.position + (Vector3)location, Quaternion.identity);
            t.transform.localScale *= spacing;
            SpriteRenderer sr = t.GetComponentInChildren<SpriteRenderer>();
            sr.color = finalTiles[i].colour;
        }
        
        p = Instantiate(playerPrefab).GetComponent<Player>();
        p.Initialize(playerPos, transform.position + (Vector3)(Vector2)playerPos * spacing);

        d = Instantiate(enemyPrefab).GetComponent<DijkstraEnemy>();
        d.Initialize(enemyPos, 
            transform.position + (Vector3)(Vector2)enemyPos * spacing);
        d.Begin();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            d.Initialize(enemyPos, 
                transform.position + (Vector3)(Vector2)enemyPos * spacing);
            d.ResetTimer();
            d.Begin();
        }
    }

    private List<bool> Fractal(List<bool> inPattern, int curSize)
    {
        if (inPattern.Count >= finalSize * finalSize)
            return inPattern;
        
        List<bool> outPattern = new List<bool>();
        outPattern.Capacity = curSize * curSize;
        
        for(int y = 0; y < curSize * 2; y++)
            for (int x = 0; x < curSize * 2; x++)
                outPattern.Add(inPattern[(y % curSize) * curSize + x % curSize]);

        if (!trueFractal)
        {
            if (!disableTransposes)
            {
                if(RandomBool())
                    TransposeMatrix(ref outPattern, curSize + 1, curSize * 2, 1, curSize, curSize * 2);
                if(RandomBool())
                    TransposeMatrix(ref outPattern, 1, curSize, curSize + 1, curSize * 2, curSize * 2);
                if(RandomBool())
                    TransposeMatrix(ref outPattern, curSize + 1, curSize * 2, curSize + 1, curSize * 2, curSize * 2);                
            }
            
            if (!disableRotations)
            {
                for(int r = 0; r < Random.Range(0, 4); r++)
                    RotateMatrix(ref outPattern, curSize + 1, curSize * 2, 1, curSize, curSize * 2);            
                for(int r = 0; r < Random.Range(0, 4); r++)
                    RotateMatrix(ref outPattern, 1, curSize, curSize + 1, curSize * 2, curSize * 2);            
                for(int r = 0; r < Random.Range(0, 4); r++)
                    RotateMatrix(ref outPattern, curSize + 1, curSize * 2, curSize + 1, curSize * 2, curSize * 2);                
            }
        }
        
        int[] doorToFactal =
        {
            Random.Range(0, curSize / 2) * 2,
            Random.Range(0, curSize / 2) * 2,
            Random.Range(0, curSize / 2) * 2,
            Random.Range(0, curSize / 2) * 2
        };

        List<int> indOfDoor = new List<int>
        {
            curSize * 2 * (doorToFactal[0] + 1) + curSize,
            curSize * 2 * curSize + curSize * 2 * (doorToFactal[1] + 1) + curSize,
            curSize * 2 * curSize + doorToFactal[2] + 1,
            curSize * 2 * curSize + curSize + doorToFactal[3] + 1 
        };
        
        if(trueFractal)
            indOfDoor.RemoveAt(Random.Range(0,4));

        foreach (var i in indOfDoor)
            outPattern[i] = false;
        
        return Fractal(outPattern, curSize * 2);
    }
    
    private void PowerTwoPass(ref List<bool> tiles)
    {
        List<bool> editList = new List<bool>(tiles);
        
        for(int y = 1; y < finalSize; y += 2)
            for (int x = 4; x < finalSize; x += 4)
            {
                var xPower = Mathf.ClosestPowerOfTwo(x) <= Mathf.ClosestPowerOfTwo(finalSize - x) ? Mathf.ClosestPowerOfTwo(x) : Mathf.ClosestPowerOfTwo(finalSize - x);
                if (Random.value < xPower * powerTwoPassCoef * 0.01)
                {
                    editList[y * finalSize + x] = false;
                }
            }
        
        for(int y = 4; y < finalSize; y += 4)
            for (int x = 1; x < finalSize; x += 2)
            {
                var yPower = Mathf.ClosestPowerOfTwo(y) <= Mathf.ClosestPowerOfTwo(finalSize - y) ? Mathf.ClosestPowerOfTwo(y) : Mathf.ClosestPowerOfTwo(finalSize - y);
                if (Random.value < yPower * powerTwoPassCoef * 0.01)
                {
                    editList[y * finalSize + x] = false;
                }
            }

        tiles = new List<bool>(editList);
    }
    
    private void RandomPass(ref List<bool> tiles)
    {
        for(int y = 1; y < finalSize; y++ )
            for (int x = 1; x < finalSize; x += 2)
            {
                if((y % 2 == 1 && x + 1 < finalSize) && tiles[y * finalSize + x + 1])
                    removableWalls.Add(y * finalSize + x + 1);
                else if (tiles[y * finalSize + x])
                    removableWalls.Add(y * finalSize + x);
            }
        
        for (int i = 0; i < randomPasses; i++)
        {
            if (removableWalls.Count > 0)
            {
                int r = Random.Range(0, removableWalls.Count);
                tiles[removableWalls[r]] = false;
                removableWalls.RemoveAt(r);
            }
        }
    }

    private void DeadEndRemoval(ref List<DTile> tiles)
    {
        for(int i = 0; i < 2; i++)
            for(int y = 1; y < finalSize / 4 * 3 + 1; y += 2)
                for (int x = 1 + (y + i) % 2 * 2 ; x < finalSize + 1; x += 4)
                {
                    bool up = !tiles[(y + 1) * (finalSize + 1) + x].isWall;
                    bool down = !tiles[(y - 1) * (finalSize + 1) + x].isWall;
                    bool left = !tiles[y * (finalSize + 1) + x - 1].isWall;
                    bool right = !tiles[y * (finalSize + 1) + x + 1].isWall;

                    int neighborCount = 0;

                    if (up)
                        neighborCount++;
                    if (down)
                        neighborCount++;
                    if (left)
                        neighborCount++;
                    if (right)
                        neighborCount++;

                    if (neighborCount == 1 && (x != 1 || y != 1) && (x != finalSize - 1 || y != finalSize / 4 * 3 - 1))
                    {
                        if (up)
                        {
                            tiles[(y + 1) * (finalSize + 1) + x].isWall = true;
                            tiles[(y + 1) * (finalSize + 1) + x].colour = Color.black;
                            tiles[(y + 1) * (finalSize + 1) + x].cost = Int32.MaxValue;
                        }

                        if (down)
                        {
                            tiles[(y - 1) * (finalSize + 1) + x].isWall = true;
                            tiles[(y - 1) * (finalSize + 1) + x].colour = Color.black;
                            tiles[(y - 1) * (finalSize + 1) + x].cost = Int32.MaxValue;
                        }

                        if (left)
                        {
                            tiles[y * (finalSize + 1) + x - 1].isWall = true;
                            tiles[y * (finalSize + 1) + x - 1].colour = Color.black;
                            tiles[y * (finalSize + 1) + x - 1].cost = Int32.MaxValue;
                        }

                        if (right)
                        {
                            tiles[y * (finalSize + 1) + x + 1].isWall = true;
                            tiles[y * (finalSize + 1) + x + 1].colour = Color.black;
                            tiles[y * (finalSize + 1) + x + 1].cost = Int32.MaxValue;
                        }
                            
                        
                        tiles[y * (finalSize + 1) + x ].isWall = true;
                        tiles[y * (finalSize + 1) + x ].colour = Color.black;
                        tiles[y * (finalSize + 1) + x ].cost = Int32.MaxValue;
                    }
                }
    }

    private void ThreeQuarterChop(ref List<bool> tiles)
    {
        List<bool> newTiles = new List<bool>();
        for(int y = 0; y < finalSize / 4 * 3; y++)
            for(int x = 0; x < finalSize; x++)
                newTiles.Add(tiles[y * finalSize + x]);
        tiles = new List<bool>(newTiles);
    }

    private void TransposeMatrix(ref List<bool> tiles, int xStart, int xEnd, int yStart, int yEnd, int curSize)
    {
        for (int y = 0; y < curSize / 2 - 1; y++)
            for (int x = 0; x < curSize / 2 - y - 2; x++)
            {
                (tiles[(yEnd - x - 1) * curSize + xEnd - y - 1], tiles[(y + yStart) * curSize + x + xStart]) 
                    = (tiles[(y + yStart) * curSize + x + xStart], tiles[(yEnd - x - 1) * curSize + xEnd - y - 1]);
            }
    }

    private void RotateMatrix(ref List<bool> tiles, int xStart, int xEnd, int yStart, int yEnd, int curSize)
    {
        List<bool> newTiles = new List<bool>();
        foreach (var t in tiles)
        {
            newTiles.Add(t);
        }
        
        for (int y = 0; y < curSize / 2 - 1; y++)
            for (int x = 0; x <curSize / 2 - 1; x++)
            {
                newTiles[(yEnd - x - 1) * curSize + y + xStart] = tiles[(yStart + y) * curSize + x + xStart];
            }
                
        for (int i = 0; i < tiles.Count; i++)
            tiles[i] = newTiles[i];
    }
    
    private bool RandomBool()
    {
        return Random.Range(0, 2) % 2 == 1;
    }

    private List<Vector2Int> GenerateVoronoiNodes()
    {
        List<Vector2Int> outVNodes = new List<Vector2Int>();
        for(int y = 0; y < finalSize / 4 * 3; y += 32 )
            for (int x = 0; x < finalSize; x += 32)
            {
                outVNodes.Add(new Vector2Int(x + Random.Range(8, 24), y + Random.Range(8, 24)));
            }

        return outVNodes;
    }
    private float DistToNearestVNode(List<Vector2Int> inVNodes, Vector2Int curTilePos)
    {
        float shortest = (curTilePos - inVNodes[0]).magnitude;

        for (int i = 1; i < inVNodes.Count; i++)
        {
            float distance = (curTilePos - inVNodes[i]).magnitude;
            if (distance < shortest)
                shortest = distance;
            
        }
        return shortest;
    }
    
    public bool CheckTile(Vector2Int tile)
    {
        if (tile.y * (finalSize + 1) + tile.x >= finalTiles.Count || tile.y * (finalSize + 1) + tile.x < 0)
        {
            return false;
        }
        else
            return !finalTiles[tile.y * (finalSize + 1) + tile.x].isWall;
    }
    
    public int GetTileCost(Vector2Int tile)
    {
        return finalTiles[tile.y * (finalSize + 1) + tile.x].cost;
    }
    
    public int GetMoveCost(Vector2Int tile)
    {
        if (flatMoveSpeed)
            return 1;
        
        return 1 + finalTiles[tile.y * (finalSize + 1) + tile.x].cost / 10;
    }
    
    public float MoveScale()
    {
        return spacing;
    }

    public int GetSize()
    {
        return finalSize + 1;
    }

    public Vector2Int GetPlayerGridPos()
    {
        return p.GetGridPos();
    }
}