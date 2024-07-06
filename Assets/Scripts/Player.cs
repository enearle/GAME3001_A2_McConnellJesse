using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/*
 * This class uses input polling to drive the inherited character behaviour.
 */

public class Player : Character
{
    // Update is called once per frame
    protected override void Update()
    {
        if(Input.GetKey(KeyCode.W)) 
            TryMove(new Vector2Int(0,1));
        else if(Input.GetKey(KeyCode.S))
            TryMove(new Vector2Int(0,-1));
        else if(Input.GetKey(KeyCode.D))
            TryMove(new Vector2Int(1,0));
        else if(Input.GetKey(KeyCode.A))
            TryMove(new Vector2Int(-1,0));
        
        base.Update();
    }
}
