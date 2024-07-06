using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.PlayerLoop;

public class Character : MonoBehaviour
{
    [SerializeField] protected Vector2Int gridPosition;
    protected Vector2Int gridMove;
    protected Vector3 moveTo;
    protected float time = 0;
    private bool moving = false;
    
    public void Initialize(Vector2Int gridPos, Vector3 pos)
    {
        gridPosition = gridPos;
        transform.position = pos;
        transform.localScale  = Vector3.one * MazeGen.Instance.SpacingScale();
    }
    
    protected void TryMove(Vector2Int direction)
    {
        if(!moving && MazeGen.Instance.CheckTileIsWall(gridMove = direction + gridPosition))
        {
            moving = true;
            moveTo = (Vector3)(Vector2)(direction + gridPosition) * MazeGen.Instance.SpacingScale() + MazeGen.Instance.transform.position;
        }
    }
    protected virtual void Update()
    {
        if (moving)
        {
            time += Time.deltaTime * 5 / MazeGen.Instance.GetMoveCost(gridPosition);
            transform.position = Vector3.Lerp(transform.position, moveTo, time);
            if (time >= 1)
            {
                transform.position = moveTo;
                gridPosition = gridMove;
                time = 0;
                moving = false;
            }            
        }
    }
    
    public Vector2Int GetGridPos()
    {
        return gridPosition;
    }
}
