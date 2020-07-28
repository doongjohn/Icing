using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BlueBox_Idle : BlueBox_State
{
    public override void OnEnter()
    {
        base.OnEnter();

        data.SmallBoxSR.color = Color.white;
    }
}
