using UnityEngine;
using System.Collections;

public class URGScript : MonoBehaviour {

    //測距範囲
    public float maxDistance;   //距離
    public float minDistance;
    public float maxAngle;  //角度
    public float minAngle;

    public float stepAngle; //角度分解能[deg]
    public float scanTime;  //走査時間[s]


    public int DataSize { get; private set; }

    private float[][] scanData;

    private Ray ray;
    private float timer;    //操作時間調節用タイマー

    private bool scanning = false;

    void initDataArray()
    {
        DataSize = 0;
        for (float angle = minAngle; angle <= maxAngle; angle += stepAngle) DataSize++;

        scanData = new float[2][];
        scanData[0] = new float[DataSize];  //radian
        scanData[1] = new float[DataSize];  //distance

    }

    void scan()
    {
        Debug.Log("Start scanning");

        // 現在の経過時間を取得
        //float check_time = Time.realtimeSinceStartup;
        int count = 0;
        float dist;
        for (float angle = minAngle; angle <= maxAngle; angle += stepAngle)
        {
            ray = new Ray(transform.position, Quaternion.AngleAxis(angle, -transform.up) * transform.forward);
            // Rayの可視化
            Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.2f);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                // Rayの原点から衝突地点までの距離を得る
                dist = hit.distance;

                scanData[0][count] = DegreeToRadian(angle);
                if (dist >= minDistance) scanData[1][count] = dist;
                else scanData[1][count] = 0.1f;

                //Debug.Log(dis);
            }
            else
            {
                scanData[0][count] = DegreeToRadian(angle);
                scanData[1][count] = 0.0f;
            }
            count++;
        }
        scanning = false;
        //Debug.Log((int)((maxAngle-minAngle)/stepAngle+0.5) +":"+ count);

        // 処理完了後の経過時間から、保存していた経過時間を引く＝処理時間
        //check_time = Time.realtimeSinceStartup - check_time;

        //Debug.Log("check time : " + check_time.ToString("0.00000"));
    }
    private float DegreeToRadian(float deg)
    {
        return deg * Mathf.PI / 180;
    }

    public float[][] GetScanData()
    {
        scanning = true;
        while (scanning) ;
        return scanData;
    }

    // Use this for initialization
    void Start()
    {
        initDataArray();
    }

    // Update is called once per frame
    void Update()
    {
        if (scanning)
        {
            scan();
        }

    }
}
