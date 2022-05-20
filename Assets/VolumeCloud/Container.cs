using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Container : MonoBehaviour
{
    public Color colour = Color.green;
    public bool displayOutLine = true;
    private void OnDrawGizmos() {
        if (displayOutLine) {
            Gizmos.color = colour;
            Gizmos.DrawWireCube(transform.position,transform.localScale);
        }
    }
}
