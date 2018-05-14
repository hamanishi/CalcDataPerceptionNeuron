using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class debugJoints : MonoBehaviour
{
    public GameObject[] joints;
    public GameObject[] linesGos;
    public LineRenderer[] lines;

    void initJoints()
    {
        var scale = 0.1f;
        joints = new GameObject[21];
        for (var i = 0; i < 21; i++)
        {
            joints[i] = GameObject.CreatePrimitive(PrimitiveType.Cube);
            joints[i].transform.localScale = new Vector3(scale, scale, scale);
        }
    }

    void initLines()
    {
        linesGos = new GameObject[21];
        
        for (var i = 0; i < 21; i++)
        {
            lines[i] = linesGos[i].AddComponent<LineRenderer>();
        }        
    }
    
    void Start()
    {
        initJoints();
        initLines();
    }

    // Update is called once per frame
    void Update()
    {
    }
}