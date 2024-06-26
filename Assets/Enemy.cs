using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

class dNode
{
    public dNode parentNode = null;
    public Vector2Int curGridPos = Vector2Int.zero;
}

public class Enemy : Character
{
    // Too bad I can't use an actual ctor in this program...
    private bool[,] explored = new bool[65,65];
    
    private Queue<dNode> searchHorizon = new Queue<dNode>();
    private Vector2Int playerPos;
    private List<dNode> path = new List<dNode>();
    private int queueCounter = 0;
    private int pathIndex = 0;
    private float timer = 0;
    private const float resetTime = 3;
    
    private void StartPath()
    {
        path.Clear();
        pathIndex = 0;
        ClearExplored();
        queueCounter = 0;
        
        playerPos = MazeGen.Instance.GetPlayerGridPos();
        
        dNode start = new dNode();
        start.curGridPos = gridPosition;
            
        searchHorizon.Enqueue(start);

        while (searchHorizon.Count > 0)
        {
            CheckNode();
        }
    }

    private void CheckNode()
    {
        dNode node = searchHorizon.Dequeue();
        queueCounter++;
        //Debug.Log(queueCounter);
        if (node.curGridPos == playerPos)
        {
            path = Retrace(node);
            path.Reverse();
            return;
        }
        
        explored[node.curGridPos.x, node.curGridPos.y] = true;
        
        Vector2Int up = node.curGridPos + Vector2Int.up;
        Vector2Int down = node.curGridPos + Vector2Int.down;
        Vector2Int right = node.curGridPos + Vector2Int.right;
        Vector2Int left = node.curGridPos + Vector2Int.left;

        if (MazeGen.Instance.CheckTile(up) && !explored[up.x, up.y])
        {
            dNode newNode = new dNode();
            newNode.parentNode = node;
            newNode.curGridPos = up;
            searchHorizon.Enqueue(newNode);
        }
        
        if (MazeGen.Instance.CheckTile(down) && !explored[down.x, down.y])
        {
            dNode newNode = new dNode();
            newNode.parentNode = node;
            newNode.curGridPos = down;
            searchHorizon.Enqueue(newNode);
        }
        
        if (MazeGen.Instance.CheckTile(left) && !explored[left.x, left.y])
        {
            dNode newNode = new dNode();
            newNode.parentNode = node;
            newNode.curGridPos = left;
            searchHorizon.Enqueue(newNode);
        }
        
        if (MazeGen.Instance.CheckTile(right) && !explored[right.x, right.y])
        {
            dNode newNode = new dNode();
            newNode.parentNode = node;
            newNode.curGridPos = right;
            searchHorizon.Enqueue(newNode);
        }
    }

    List<dNode> Retrace(dNode node)
    {
        List<dNode> returnPath = new List<dNode>();
        returnPath.Add(node);
        if (node.parentNode != null)
            returnPath.AddRange(Retrace(node.parentNode));
        return returnPath;
    }

    public void Begin()
    {
        StartPath();
    }

    protected override void Update()
    {
        timer += Time.deltaTime;
        
        if (timer >= resetTime)
        {
            timer -= resetTime;
            StartPath();
        }

        if (gridPosition == path[pathIndex].curGridPos)
            pathIndex++;
        
        TryMove(path[pathIndex].curGridPos - gridPosition);

        foreach (var dn in path)
        {
            if(dn.parentNode != null)
                Debug.DrawLine(MazeGen.Instance.transform.position + Vector3.back + (Vector3)(Vector2)dn.parentNode.curGridPos * MazeGen.Instance.MoveScale(),
                    MazeGen.Instance.transform.position + Vector3.back + (Vector3)(Vector2)dn.curGridPos * MazeGen.Instance.MoveScale(), Color.magenta);
        }
        
        base.Update();
    }

    private void ClearExplored()
    {
        for(int y = 0; y < MazeGen.Instance.GetSize(); y++)
            for (int x = 0; x < MazeGen.Instance.GetSize(); x++)
                explored[x, y] = false;
    }
}
