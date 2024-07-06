using System.Collections.Generic;
using UnityEngine;
using Utils;

/*
 * This class uses makes calls to the Grid Manager and Path Manager to drive the inherited character behaviour.
 */
public class DijkstraEnemy : Character
{
    private PriorityQueue<DNode, int> searchHorizon = new PriorityQueue<DNode, int>();
    private Vector2Int goal;
    private List<DNode> nodes = new List<DNode>();
    private int queueCounter = 0;
    private int pathIndex = 0;
    private PathStruct path = new PathStruct();
    private float timer = 0;
    private const float resetTime = 3f;
    private int size;
    private bool found = false;
    private int originNode;
    private bool chasingCursor = false;
    
    private void Awake()
    {
        size = GridManager.Instance.GetSize();
        for (int i = 0; i < size * size; i++)
        {
            nodes.Add(new DNode());
        }
    }
    
    protected override void Update()
    {
        if (!chasingCursor)
        {
            timer += Time.deltaTime;
            
            if (timer >= resetTime)
            {
                ResetTimer();
                goal = GridManager.Instance.GetPlayerGridPos();
                path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
                Debug.Log("called");
                GridManager.Instance.SetDebugTiles(path);
                pathIndex = 0;
            }
        }
        
        if (gridPosition == path.pathList[pathIndex].curGridPos)
            pathIndex++;
        
        if (pathIndex >= path.pathList.Count)
        {
            chasingCursor = false;
            ResetTimer();
            goal = GridManager.Instance.GetPlayerGridPos();
            path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
            GridManager.Instance.SetDebugTiles(path);
            pathIndex = 0;
        }
        
        TryMove(path.pathList[pathIndex].curGridPos - gridPosition);
        
        base.Update();
    }

    public void Begin()
    {
        goal = GridManager.Instance.GetPlayerGridPos();
        path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
        GridManager.Instance.SetDebugTiles(path);
        pathIndex = 0;
    }

    public void ResetTimer()
    {
        timer -= resetTime;
    }

    public void SetCursorAsGoal(Vector2Int pos)
    {
        chasingCursor = true;
        ResetTimer();
        goal = pos;
        path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
        GridManager.Instance.SetDebugTiles(path);
        pathIndex = 0;
    }
}
