using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utils;

/*
 * This class uses Dijkstra's algorithm to find a path through maze created by the Grid Manager.
 */

public class DNode
{
    public int parentNode = -1;
    public Vector2Int curGridPos = Vector2Int.zero;
    public int cost;
}
public struct PathStruct
{
    PathStruct(PathStruct p)
    {
        pathList = new List<DNode>(p.pathList);
        searchedList = new List<bool>(p.searchedList);
    }
    
    public List<DNode> pathList;
    public List<bool> searchedList;
}
public static class PathManager
{
    private static PriorityQueue<DNode, int> searchHorizon = new PriorityQueue<DNode, int>();
    private static Vector2Int objective;
    private static List<DNode> path = new List<DNode>();
    private static List<DNode> nodes = new List<DNode>();
    private static int xDim;
    private static bool found = false;
    private static int originNode;
    private static List<bool> searched = new List<bool>();

    public static PathStruct GetPath(Vector2Int goal, Vector2Int curPos, int xSize, int yDim)
    {
        objective = goal;
        xDim = xSize;
        nodes.Clear();
        nodes.Capacity = yDim * xDim;
        searched.Clear();
        searched.Capacity = yDim * xDim;
        
        for (int i = 0; i < yDim * xDim; i++)
        {
            DNode n = new DNode();
            n.cost = Int32.MaxValue;
            nodes.Add(n);
            searched.Add(false);
        }
        
        path.Clear();
        path.Capacity = yDim * xDim;

        found = false;

        originNode = curPos.y * xDim + curPos.x;
        nodes[originNode].cost = 0;
        nodes[originNode].parentNode = -1;
        nodes[originNode].curGridPos = curPos;
        searchHorizon.Clear();
        searchHorizon.EnsureCapacity(yDim * xDim);
        searchHorizon.Enqueue(nodes[originNode], nodes[originNode].cost);

        while (searchHorizon.Count > 0 && found == false)
        {
            CheckNode();
        }

        PathStruct outPath = new PathStruct();
        outPath.pathList = new List<DNode>(path);
        outPath.searchedList = new List<bool>(searched);
        
        return outPath;
    }
    
    private static void CheckNode()
    {
        DNode node = searchHorizon.Dequeue();
        searched[node.curGridPos.y * xDim + node.curGridPos.x] = true;
        
        if (node.curGridPos == objective)
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
            if (a.y * xDim + a.x != node.parentNode && GridManager.Instance.CheckTile(a))
            {
                nodes[a.y * xDim + a.x].curGridPos = a;
                if (nodes[a.y * xDim + a.x].cost > node.cost + GridManager.Instance.GetTileCost(a))
                {
                    nodes[a.y * xDim + a.x].parentNode = node.curGridPos.y * xDim + node.curGridPos.x;
                    nodes[a.y * xDim + a.x].cost = node.cost + GridManager.Instance.GetTileCost(a);
                    searchHorizon.Enqueue(nodes[a.y * xDim + a.x], nodes[a.y * xDim + a.x].cost);
                }
            }
        }
    }
    
    private static List<DNode> Retrace(DNode node)
    {
        List<DNode> returnPath = new List<DNode>();
        returnPath.Add(node);
        
        while(node.parentNode != -1)
        {
            node = nodes[node.parentNode];
            returnPath.Add(node);
        }

        return returnPath;
    }
}
