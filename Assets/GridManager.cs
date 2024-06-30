using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{

    public static GridManager Instance;
    [SerializeField] private Sprite[] sprites;
    [SerializeField] private Tilemap tilemap;
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private GameObject enemyPrefab;
    private Vector2Int playerPos = new Vector2Int(1, 1); 
    private Vector2Int enemyPos = new Vector2Int(64, 64); 
    private Player p;
    private DijkstraEnemy d;
    private List<bool> sprite;
    
    // Start is called before the first frame update
    void Start()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        DontDestroyOnLoad(gameObject);
        Instance = this;
    }

    public void SetTiles(ref List<DTile> tiles)
    {

        Tile wall = new Tile();
        Tile floor = new Tile();
        wall.sprite = sprites[0];
        floor.sprite = sprites[1];
        
        int size = MazeGen.Instance.GetSize();
        for (int i = 0; i < tiles.Count; i++)
        {
            tilemap.SetTile(new Vector3Int(i % size, i / size), tiles[i].isWall ? wall : floor);
        }
    }

    public void SpawnCharacters()
    {
        p = Instantiate(playerPrefab).GetComponent<Player>();
        p.Initialize(playerPos, transform.position + (Vector3)(Vector2)playerPos * MazeGen.Instance.SpacingScale());

        d = Instantiate(enemyPrefab).GetComponent<DijkstraEnemy>();
        d.Initialize(enemyPos, 
            transform.position + (Vector3)(Vector2)enemyPos * MazeGen.Instance.SpacingScale());
        d.Begin();
    }
    
    public Vector2Int GetPlayerGridPos()
    {
        return p.GetGridPos();
    }
}
