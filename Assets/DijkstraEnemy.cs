using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Utils;

class DNode
{
    public int parentNode = -1;
    public Vector2Int curGridPos = Vector2Int.zero;
    public int cost;
}

public class DijkstraEnemy : Character
{
    private PriorityQueue<DNode, int> searchHorizon = new PriorityQueue<DNode, int>();
    private Vector2Int playerPos;
    private List<DNode> path = new List<DNode>();
    private List<DNode> nodes = new List<DNode>();
    private int queueCounter = 0;
    private int pathIndex = 0;
    private float timer = 0;
    private const float resetTime = 3f;
    private int size;
    private bool found = false;
    private int originNode;
    
    private void Awake()
    {
        size = MazeGen.Instance.GetSize();
        for (int i = 0; i < size * size; i++)
        {
            nodes.Add(new DNode());
        }
    }
    
    protected override void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= resetTime)
        {
            ResetTimer();
            StartPath();
        }

        if (gridPosition == path[pathIndex].curGridPos)
            pathIndex++;
        
        TryMove(path[pathIndex].curGridPos - gridPosition);

        if(MazeGen.Instance.Debug)
            foreach (var dn in path)
                if(dn.parentNode != originNode)
                    Debug.DrawLine(MazeGen.Instance.transform.position + Vector3.back + (Vector3)(Vector2)nodes[dn.parentNode].curGridPos * MazeGen.Instance.MoveScale(),
                        MazeGen.Instance.transform.position + Vector3.back + (Vector3)(Vector2)dn.curGridPos * MazeGen.Instance.MoveScale(), Color.magenta);
        
        base.Update();
    }
    
    private void StartPath()
    {
        foreach (var dNode in nodes)
        {
            dNode.cost = Int32.MaxValue;
        }
        
        path.Clear();
        path.Capacity = size * size;
        pathIndex = 0;
        queueCounter = 0;
        originNode = gridPosition.y * size + gridPosition.x;
        found = false;
        
        playerPos = MazeGen.Instance.GetPlayerGridPos();
        
        nodes[0].cost = 0;
        nodes[0].parentNode = -1;
        nodes[0].curGridPos = gridPosition;
        searchHorizon.Clear();
        searchHorizon.EnsureCapacity(size * size);
        searchHorizon.Enqueue(nodes[0], nodes[0].cost);

        while (searchHorizon.Count > 0 && found == false)
        {
            CheckNode();
        }
    }

    private void CheckNode()
    {
        DNode node = searchHorizon.Dequeue();
        queueCounter++;
        
        if (node.curGridPos == playerPos)
        {
            path = Retrace(node); 
            path.Reverse();
            found = true;
            return;
        }
        
        Vector2Int[] adjacents = new[]
        {
            new Vector2Int((node.curGridPos + Vector2Int.up).x, (node.curGridPos + Vector2Int.up).y),
            new Vector2Int((node.curGridPos + Vector2Int.down).x, (node.curGridPos + Vector2Int.down).y),
            new Vector2Int((node.curGridPos + Vector2Int.right).x, (node.curGridPos + Vector2Int.right).y),
            new Vector2Int((node.curGridPos + Vector2Int.left).x, (node.curGridPos + Vector2Int.left).y)
        };

        foreach (var a in adjacents)
        {
            if (a.y * size + a.x != node.parentNode && MazeGen.Instance.CheckTile(a))
            {
                nodes[a.y * size + a.x].curGridPos = a;
                if (nodes[a.y * size + a.x].cost > node.cost + MazeGen.Instance.GetTileCost(a))
                {
                    nodes[a.y * size + a.x].parentNode = node.curGridPos.y * size + node.curGridPos.x;
                    nodes[a.y * size + a.x].cost = node.cost + MazeGen.Instance.GetTileCost(a);
                    searchHorizon.Enqueue(nodes[a.y * size + a.x], nodes[a.y * size + a.x].cost);
                }
            }
        }
    }
    
    List<DNode> Retrace(DNode node)
    {
        List<DNode> returnPath = new List<DNode>();
        returnPath.Add(node);
        
        while(node.parentNode != originNode)
        {
            node = nodes[node.parentNode];
            returnPath.Add(node);
        }

        return returnPath;
    }

    public void Begin()
    {
        StartPath();
    }

    public void ResetTimer()
    {
        timer -= resetTime;
    }
}
