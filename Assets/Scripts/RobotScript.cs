using UnityEngine;
using System.Collections;

public class RobotScript : MonoBehaviour {
    public float transformSpeed;
    public float rotateSpeed;

    public bool isAcceptKeyboard;

    private Vector3 pos;
    private Vector3 direction;

    private string drivingCommand;  // forward, right, left, back, stop
    private float speedCoefficient;

    public int HitCount { get; private set; }

    public Vector3[] GetMovement()
    {
        return new Vector3[]{ pos,direction };
    }

    public void DrivingInstruction(string command, float coefficient)
    {
        drivingCommand = command;
        speedCoefficient = coefficient;
    }

	// Use this for initialization
	void Start () {
        pos = transform.position;
        direction = transform.eulerAngles;

        drivingCommand = "stop";
        HitCount = 0;
	}
	
	// Update is called once per frame
	void Update () {
        if (isAcceptKeyboard)
        {
            if (Input.GetKey("right"))      transform.eulerAngles += new Vector3(0f, rotateSpeed, 0f);
            else if (Input.GetKey("left"))  transform.eulerAngles += new Vector3(0f, -rotateSpeed, 0f);
            else if (Input.GetKey("up"))    transform.position += transform.forward * transformSpeed;
            else if (Input.GetKey("down"))  transform.position += transform.forward * -transformSpeed;
        }
        else
        {
            if (drivingCommand == "right") transform.eulerAngles += new Vector3(0f, rotateSpeed * speedCoefficient, 0f);
            else if (drivingCommand == "left") transform.eulerAngles += new Vector3(0f, -rotateSpeed * speedCoefficient, 0f);
            else if (drivingCommand == "forward") transform.position += transform.forward * transformSpeed * speedCoefficient;
            else if (drivingCommand == "back") transform.position += transform.forward * -transformSpeed * speedCoefficient;
        }

        pos = transform.position;
        direction = transform.eulerAngles;
	
	}

    void HitDecision()
    {
        HitCount++;
        Debug.Log(HitCount);
    }
    void ExitDicision()
    {
        HitCount--;
        Debug.Log(HitCount);
    }

    void OnCollisionEnter(Collision collision)
    {
        HitDecision();
    }
    void OnCollisionExit(Collision collision)
    {
        ExitDicision();
    }

    void RedirectedOnTriggerEnter(Collider collider) { }
    void RedirectedOnTriggerStay(Collider collider) { }
    void RedirectedOnTriggerExit(Collider collider) { }

    void RedirectedOnCollisionEnter(Collision collision) { HitDecision(); }
    void RedirectedOnCollisionStay(Collision collision) { }
    void RedirectedOnCollisionExit(Collision collision) { ExitDicision(); }
    
}
