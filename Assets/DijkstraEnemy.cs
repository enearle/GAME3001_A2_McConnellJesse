using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Utils;

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
                path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
                pathIndex = 0;
            }
        }
        
        Debug.Log($"{pathIndex}/{path.pathList.Count} --- {gridPosition}");
        if (gridPosition == path.pathList[pathIndex].curGridPos)
            pathIndex++;
        
        if (pathIndex >= path.pathList.Count)
        {
            chasingCursor = false;
            ResetTimer();
            goal = GridManager.Instance.GetPlayerGridPos();
            path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
            pathIndex = 0;
        }
        

        TryMove(path.pathList[pathIndex].curGridPos - gridPosition);


        if(GridManager.Instance.IsDebug)
            foreach (var dn in path.pathList)
                if(dn.parentNode != originNode)
                    Debug.DrawLine(GridManager.Instance.transform.position + Vector3.back + (Vector3)(Vector2)nodes[dn.parentNode].curGridPos * GridManager.Instance.MoveScale(),
                        GridManager.Instance.transform.position + Vector3.back + (Vector3)(Vector2)dn.curGridPos * GridManager.Instance.MoveScale(), Color.magenta);
        
        base.Update();
    }

    public void Begin()
    {
        goal = GridManager.Instance.GetPlayerGridPos();
        path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
    }

    public void ResetTimer()
    {
        timer -= resetTime;
    }

    public void SetCursorAsGoal(Vector2Int pos)
    {
        chasingCursor = true;
        goal = pos;
        path = PathManager.GetPath(goal, gridPosition, size, size / 4 * 3 + size % 4);
        pathIndex = 0;
        Debug.Log(pos);
    }
}
