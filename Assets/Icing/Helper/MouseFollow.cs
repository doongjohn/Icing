using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Icing
{
    public class MouseFollow : MonoBehaviour
    {
        private Camera cam;
        Vector2 mouseWorldPos;

        private void Awake()
        {
            cam = Camera.main;
        }
        private void LateUpdate()
        {
            mouseWorldPos = cam.ScreenToWorldPoint(Input.mousePosition);
            transform.position = transform.position.Change(
                x: mouseWorldPos.x,
                y: mouseWorldPos.y
            );
        }
    }
}
