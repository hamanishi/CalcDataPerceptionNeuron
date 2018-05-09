using UnityEngine;
using System.Collections;

public class NeuronVRAdapter : MonoBehaviour
{
    public Transform            bindTransform = null;
	public Transform 			hipsTransform = null;
	public bool					alignToHipRotation = true;

	void OnPreCull()
	{
		// Re-Position the camera to our head bind Target
		transform.position = bindTransform.position;
	}
    

	void Update( )
    {        
		// Re-Position the camera to our head bind Target
		if (bindTransform != null) 
		{
			transform.position = bindTransform.position;
		}

		//Allow for resetting of Camera
		if (Input.GetKeyDown(KeyCode.R))
		{
			if (hipsTransform != null && alignToHipRotation) 
			{
				transform.rotation = Quaternion.Euler (0f, hipsTransform.eulerAngles.y, 0f);
			}

			UnityEngine.XR.InputTracking.Recenter ();
		}
    }
}
