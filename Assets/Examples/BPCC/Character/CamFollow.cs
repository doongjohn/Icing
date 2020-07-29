using Icing;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CamFollow : MonoBehaviour
{
    public Transform followTarget;

    private void LateUpdate()
    {
        transform.position = followTarget.position.Change(z: transform.position.z);
    }
}
