using Icing;
using UnityEngine;

public class BlueBox_State : CSM_State
{
    protected BlueBox_Data data;

    public override void Init(CSM_Data data)
    {
        this.data = data as BlueBox_Data;
    }

    public override void OnEnter()
    {
        Debug.Log($"[-> Enter] <{GetType().FullName}>");
    }
    protected override void OnExit()
    {
        Debug.Log($"[<-  Exit] <{GetType().FullName}>");
    }
}
