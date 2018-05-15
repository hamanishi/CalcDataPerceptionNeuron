using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class debugJoints : MonoBehaviour
{
    public static int length = 21; 
    public GameObject[] joints;
    public GameObject[] linesGos;
    public LineRenderer[] lines;
    public Queue<Vector3>[] linesBuf;
    public float width = 1f;

    
    	// When added to an object, draws colored rays from the
	// transform position.
	public int lineCount = 100;
	public float radius = 3.0f;

	static Material lineMaterial;
	static void CreateLineMaterial ()
	{
		if (!lineMaterial)
		{
			// Unity has a built-in shader that is useful for drawing
			// simple colored things.
			Shader shader = Shader.Find ("Hidden/Internal-Colored");
			lineMaterial = new Material (shader);
			lineMaterial.hideFlags = HideFlags.HideAndDontSave;
			// Turn on alpha blending
			lineMaterial.SetInt ("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
			lineMaterial.SetInt ("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
			// Turn backface culling off
			lineMaterial.SetInt ("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
			// Turn off depth writes
			lineMaterial.SetInt ("_ZWrite", 0);
		}
	}

	// Will be called after all regular rendering is done
	public void OnRenderObject ()
	{
		CreateLineMaterial ();
		// Apply the line material
		lineMaterial.SetPass (0);

		GL.PushMatrix ();
		// Set transformation matrix for drawing to
		// match our transform
		GL.MultMatrix (transform.localToWorldMatrix);

		// Draw lines
		GL.Begin (GL.LINES);
		for (int i = 0; i < 100; ++i)
		{
			GL.Color (Color.green);
			//GL.Vertex3 (0, 0, 0);
			GL.Vertex3(linesBuf[i+1%100].ToArray()[0].x,linesBuf[i+1%100].x,linesBuf[i+1%100].z);
			lines[i].SetPositions(linesBuf[i].ToArray());
			GL.Vertex3 (Mathf.Cos (angle) * radius, Mathf.Sin (angle) * radius, 0);
		}
		GL.End ();
		GL.PopMatrix ();
	}
    
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
        try
        {
            linesGos = new GameObject[length];
            lines = new LineRenderer[length];
            linesBuf = new Queue<Vector3>[length];
            
            for (var i = 0; i < length; i++)
            {
                linesGos[i] = new GameObject("lines"+i);
                linesGos[i].transform.position = Vector3.zero;
                lines[i] = linesGos[i].AddComponent<LineRenderer>();
                lines[i].material.color = Color.green;
                lines[i].SetVertexCount(100);
                lines[i].SetWidth(width, width);
                lines[i].SetPosition(0, Vector3.forward);
                lines[i].SetPosition(1, Vector3.left);
                lines[i].loop = false;
                
                linesBuf[i] = new Queue<Vector3>();
                for (var j = 0; j < lines[i].positionCount; j++)
                {
                    linesBuf[i].Enqueue(UnityEngine.Random.insideUnitSphere);
                }
            }
        }
        catch (Exception e)
        {
            Debug.Log(e);
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
        for (var i = 0; i < length; i++)
        {
            //Debug.Log("lines:" + linesBuf[i]);
            lines[i].SetPositions(linesBuf[i].ToArray());
        }
    }
}