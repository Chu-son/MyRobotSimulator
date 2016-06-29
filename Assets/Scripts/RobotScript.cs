using UnityEngine;
using System.Collections;

public class RobotScript : MonoBehaviour {
    public float transformSpeed;
    public float rotateSpeed;

    private Vector3 pos;
    private Vector3 direction;

    private bool updateing;

    public Vector3[] GetMovement()
    {
        return new Vector3[]{ pos,direction };
    }

	// Use this for initialization
	void Start () {
        pos = transform.position;
        direction = transform.eulerAngles;

        updateing = false;
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
        pos = transform.position;
        direction = transform.eulerAngles;
	
	}
}
