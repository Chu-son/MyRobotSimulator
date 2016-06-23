using UnityEngine;
using System.Collections;

public class RobotScript : MonoBehaviour {
    public float transformSpeed;
    public float rotateSpeed;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
        if (Input.GetKey("right"))
        {
            transform.eulerAngles += new Vector3(0f, rotateSpeed, 0f);
        }
        else if (Input.GetKey("left"))
        {
            transform.eulerAngles += new Vector3(0f, -rotateSpeed, 0f);
        }
        else if (Input.GetKey("up"))
        {
            transform.position += transform.forward * transformSpeed;
        }
        else if (Input.GetKey("down"))
        {
            transform.position += transform.forward * -transformSpeed;
        }
	
	}
}
