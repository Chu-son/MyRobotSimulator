using UnityEngine;
using System.Collections;

public class RobotScript : MonoBehaviour {
    public float transformSpeed;
    public float rotateSpeed;

    public bool isAcceptKeyboard;
    public bool isNoisy;

    private Vector3 prejudicePos;
    private Vector3 prejudiceDirection;
    private Vector3 forwardVec;

    private Vector3 truePos;
    private Vector3 trueDirection;

    private string drivingCommand;  // forward, right, left, back, stop
    private float speedCoefficient;
    private float drivingValue;
    private Vector3 drivingOrigin;
    private float tolerance;


    private bool isParamUpdating = false;

    public int HitCount { get; private set; }

    public Vector3[] GetMovement()
    {
        return new Vector3[] { prejudicePos, prejudiceDirection, truePos, trueDirection };
    }

    public bool IsDriving()
    {
        while (isParamUpdating) Debug.Log("wait!");
        if (drivingCommand == "stop") return false;
        return true;
    }

    public void DrivingInstruction(string command, float value, float coefficient, float tolerance)
    {
        // パラメータ変更中に参照されると誤作動の原因になるので
        isParamUpdating = true;

        drivingCommand = command;
        speedCoefficient = coefficient;
        drivingValue = value;
        this.tolerance = tolerance;
        
        switch (drivingCommand)
        {
            case "forward":
            case "back":
                drivingOrigin = prejudicePos;
                break;

            case "right":
            case "left":
                drivingOrigin = prejudiceDirection;
                break;
        }

        isParamUpdating = false;
    }

    private float DegreeDisp(float current_deg, float pre_deg)
    {
        float disp = current_deg - pre_deg;
        if (disp >= 180f) disp -= 360f;
        else if (disp <= -180f) disp += 360f;
        return disp;
    }

    // 移動が完了したかチェック
    public bool CheckDrivingEnd()
    {
        float dist;
        switch (drivingCommand)
        {
            case "forward":
            case "back":
                dist = drivingValue * drivingValue - (Mathf.Pow(prejudicePos[0] - drivingOrigin[0], 2) + Mathf.Pow(prejudicePos[2] - drivingOrigin[2], 2));
                if ( dist < tolerance * tolerance)
                {
                    drivingCommand = "stop";
                    return false;
                }
                
                break;

            case "right":
            case "left":
                dist = drivingValue * drivingValue - Mathf.Pow(DegreeDisp(prejudiceDirection[1], drivingOrigin[1]), 2);
                if (dist < tolerance * tolerance)
                {
                    drivingCommand = "stop";
                    return false;
                }
                break;
        }
        return true;
    }

	// Use this for initialization
	void Start () {
        prejudicePos = transform.position;
        prejudiceDirection = transform.eulerAngles;
        forwardVec = Quaternion.Euler(prejudiceDirection) * Vector3.forward;

        drivingCommand = "stop";
        HitCount = 0;
	}
	
	// Update is called once per frame
	void Update () {
        if (isParamUpdating) return;

        if (isAcceptKeyboard)
        {
            if (Input.GetKey("right"))
            {
                Vector3 delta = new Vector3(0f, rotateSpeed * Time.deltaTime, 0f);
                transform.eulerAngles += delta;
                prejudiceDirection += delta;
            }
            else if (Input.GetKey("left"))
            {
                Vector3 delta = new Vector3(0f, -rotateSpeed * Time.deltaTime, 0f);
                transform.eulerAngles += delta;
                prejudiceDirection += delta;
            }
            else if (Input.GetKey("up"))
            {
                Vector3 delta = transform.forward * transformSpeed * Time.deltaTime;
                transform.position += delta;
                prejudicePos += delta;
            }
            else if (Input.GetKey("down"))
            {
                Vector3 delta = transform.forward * -transformSpeed * Time.deltaTime;
                transform.position += delta;
                prejudicePos += delta;
            }
        }
        // クライアントからの駆動指令
        else
        {
            Vector3 delta;
            switch (drivingCommand)
            {
                case "right":
                    delta = new Vector3(0f, speedCoefficient * Time.deltaTime, 0f);
                    transform.eulerAngles += delta;

                    prejudiceDirection += delta;
                    forwardVec = Quaternion.Euler(prejudiceDirection) * Vector3.forward;
                    break;

                case "left":
                    delta = new Vector3(0f, -speedCoefficient * Time.deltaTime, 0f);
                    transform.eulerAngles += delta;

                    prejudiceDirection += delta;
                    forwardVec = Quaternion.Euler(prejudiceDirection) * Vector3.forward;
                    break;

                case "forward":
                    delta = transform.forward * speedCoefficient * Time.deltaTime;
                    transform.position += delta;

                    delta = forwardVec * speedCoefficient * Time.deltaTime;
                    prejudicePos += delta;
                    break;

                case "back":
                    delta = transform.forward * -speedCoefficient * Time.deltaTime;
                    transform.position += delta;

                    delta = forwardVec * -speedCoefficient * Time.deltaTime;
                    prejudicePos += delta;
                    break;
            }

            if (isNoisy && !drivingCommand.Equals("stop"))
            {
                transform.eulerAngles += new Vector3(0f, -1 * Time.deltaTime, 0f); // ノイズ的なの
            }

            CheckDrivingEnd();
        }

        truePos = transform.position;
        trueDirection = transform.eulerAngles;
	
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
