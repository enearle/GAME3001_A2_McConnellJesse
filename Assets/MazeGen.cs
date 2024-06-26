using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.U2D.Aseprite;
using UnityEngine;
using Random = UnityEngine.Random;

public class MazeGen : MonoBehaviour
{
    public static MazeGen Instance;
    
    [SerializeField] private GameObject tilePrefab;
    [SerializeField] private int finalSize = 64;
    [SerializeField] private int randomPasses = 23;
    [SerializeField] private float spacing = 0.2f;
    [SerializeField] private bool trueFractal = false;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    
    private Vector2Int playerPos = new Vector2Int(1, 1);
    private List<bool> finalTiles = new List<bool>();
    private Player p;
    
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

        List<bool> fractalTiles = new List<bool>();
        fractalTiles.Capacity = finalSize * finalSize;
        fractalTiles = Fractal(firstTiles, 4);

        if(!trueFractal)
            RandomPass(ref fractalTiles);
        
        finalTiles.Capacity = finalSize + 1 * finalSize + 1;
        for(int y = 0; y < finalSize + 1; y++)
            for(int x = 0; x < finalSize + 1; x++)
                if(x == finalSize || y == finalSize)
                    finalTiles.Add(true);
                else
                    finalTiles.Add(fractalTiles[y * finalSize + x]);
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
            if(RandomBool())
                Transpose(ref outPattern, curSize + 1, curSize * 2, 1, curSize, curSize * 2);
            for(int r = 0; r < Random.Range(0, 4); r++)
                Rotate(ref outPattern, curSize + 1, curSize * 2, 1, curSize, curSize * 2);
            if(RandomBool())
                Transpose(ref outPattern, 1, curSize, curSize + 1, curSize * 2, curSize * 2);
            for(int r = 0; r < Random.Range(0, 4); r++)
                Rotate(ref outPattern, 1, curSize, curSize + 1, curSize * 2, curSize * 2);
            if(RandomBool())
                Transpose(ref outPattern, curSize + 1, curSize * 2, curSize + 1, curSize * 2, curSize * 2);
            for(int r = 0; r < Random.Range(0, 4); r++)
                Rotate(ref outPattern, curSize + 1, curSize * 2, curSize + 1, curSize * 2, curSize * 2);
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

    private void RandomPass(ref List<bool> tiles)
    {
        List<int> removableWalls = new List<int>();
        
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

    private void Transpose(ref List<bool> tiles, int xStart, int xEnd, int yStart, int yEnd, int curSize)
    {
        for (int y = 0; y < curSize / 2 - 1; y++)
            for (int x = 0; x < curSize / 2 - y - 2; x++)
            {
                (tiles[(yEnd - x - 1) * curSize + xEnd - y - 1], tiles[(y + yStart) * curSize + x + xStart]) 
                    = (tiles[(y + yStart) * curSize + x + xStart], tiles[(yEnd - x - 1) * curSize + xEnd - y - 1]);
            }

    }

    private void Rotate(ref List<bool> tiles, int xStart, int xEnd, int yStart, int yEnd, int curSize)
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
    
    private void Start()
    {
        for (int i = 0; i < (finalSize + 1) * (finalSize + 1); i++)
        {
            Vector2 location;
            location.x = (i % (finalSize + 1)) * spacing;
            location.y = (i / (finalSize + 1)) * spacing;
            GameObject t = Instantiate(tilePrefab, transform.position + (Vector3)location, Quaternion.identity);
            t.transform.localScale *= spacing;
            if (finalTiles[i])
            {
                SpriteRenderer sr = t.GetComponentInChildren<SpriteRenderer>();
                sr.color = Color.black;
            }
        }
        
        p = Instantiate(playerPrefab).GetComponent<Player>();
        p.Initialize(playerPos, transform.position + (Vector3)(Vector2)playerPos * spacing);

        Enemy e = Instantiate(enemyPrefab).GetComponent<Enemy>();
        e.Initialize(new Vector2Int(finalSize - 1, finalSize - 1), 
            transform.position + new Vector3(finalSize - 1,finalSize - 1) * spacing);
        e.Begin();

    }

    public bool CheckTile(Vector2Int tile)
    {
        
        if (tile.y * (finalSize + 1) + tile.x >= finalTiles.Count || tile.y * (finalSize + 1) + tile.x < 0)
        {
            Debug.Log($"Out of range. Final tiles: {finalTiles.Count} Index: {tile.y * (finalSize + 1) + tile.x}");
            return false;
        }
        else
            return !finalTiles[tile.y * (finalSize + 1) + tile.x];
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
