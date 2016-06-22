using UnityEngine;
using System.Collections;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class URGScript : MonoBehaviour
{

    //測距範囲
    public float maxDistance;   //距離
    public float minDistance;
    public float maxAngle;  //角度
    public float minAngle;

    public float stepAngle; //角度分解能[deg]
    public float scanTime;  //走査時間[s]

    private int dataSize;
    private float[][] scanData;

    private Ray ray;
    private float timer;    //操作時間調節用タイマー

    private bool scanning = false;

    void initDataArray()
    {
        dataSize = 0;
        for (float angle = minAngle; angle <= maxAngle; angle += stepAngle) dataSize++;

        scanData = new float[2][];
        scanData[0] = new float[dataSize];  //radian
        scanData[1] = new float[dataSize];  //distance
    
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
            Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.1f);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                // Rayの原点から衝突地点までの距離を得る
                dist = hit.distance;

                scanData[0][count] = DegreeToRadian( angle );
                if (dist >= minDistance) scanData[1][count] = dist;
                else scanData[1][count] = 0.0f;

                //Debug.Log(dis);
            }
            else
            {
                scanData[0][count] = angle;
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
    

	// Use this for initialization
	void Start () {
        initDataArray();

        StartListening();
	}
	
	// Update is called once per frame
	void Update () {

        //timer += Time.deltaTime;

        //if (timer >= scanTime)
        //{
        //    timer = 0;
        //    scan();
        //}

        if (scanning)
        {
            scan();        
        }

	}

    // State object for reading client data asynchronously
    public class StateObject
    {
        // Client  socket.
        public Socket workSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder sb = new StringBuilder();
    }

    List<StateObject> activeConnections = new List<StateObject>();

    public void StartListening()
    {

        // Establish the local endpoint for the socket.
        // The DNS name of the computer
        // running the listener is "host.contoso.com".
        IPAddress ipAddress = IPAddress.Parse(GetIPAddress("localhost"));
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 8000);

        // Create a TCP/IP socket.
        Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.
        try
        {
            listener.Bind(localEndPoint);
            listener.Listen(10);

            // Start an asynchronous socket to listen for connections.
            listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }

    }

    public void AcceptCallback(IAsyncResult ar)
    {
        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;

        //確立した接続のオブジェクトをリストに追加
        activeConnections.Add(state);

        Debug.LogFormat("there is {0} connections", activeConnections.Count);

        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);

        //接続待ちを再開しないと次の接続を受け入れなくなる
        listener.BeginAccept(new AsyncCallback(AcceptCallback), listener);

    }

    public void ReadCallback(IAsyncResult ar)
    {
        String content = String.Empty;

        // Retrieve the state object and the handler socket
        // from the asynchronous state object.
        StateObject state = (StateObject)ar.AsyncState;
        Socket handler = state.workSocket;

        // Read data from the client socket. 
        int bytesRead = handler.EndReceive(ar);

        if (bytesRead > 0)
        {
            // There  might be more data, so store the data received so far.
            state.sb.Append(Encoding.ASCII.GetString(state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read 
            // more data.
            content = state.sb.ToString();

            //MSDNのサンプルはEOFを検知して出力をしているけれどもncコマンドはEOFを改行時にLFしか飛ばさないので\nを追加
            if (content.IndexOf("\n") > -1 || content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the 
                // client. Display it on the console.
                Debug.LogFormat("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                // Echo the data back to the client.
                //Send(handler, content);
                SendScanDatas(handler);

                //clear data in object before next receive
                //StringbuilderクラスはLengthを0にしてクリアする
                state.sb.Length = 0; ;

                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);

            }
            else
            {
                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                    new AsyncCallback(ReadCallback), state);
            }
        }
    }

    private void SendScanDatas(Socket handler)
    {
        scanning = true;
        while (scanning) ;

        Debug.Log("Making send data");
        int typeSize = sizeof(float);
        byte[] sendData = new byte[dataSize * 2 * typeSize + typeSize];
        int index = 0;

        foreach (var item in scanData[0])
        {
            Array.Copy((BitConverter.GetBytes(item)), 0, sendData, index * typeSize , typeSize);
            index++;
        }
        foreach (var item in scanData[1])
        {
            Array.Copy((BitConverter.GetBytes(item)), 0, sendData, index * typeSize, typeSize);
            index++;
        }
        Array.Copy((BitConverter.GetBytes(-1.0f)), 0, sendData, index * typeSize, typeSize);


        Debug.Log("sendData:" + sendData.Length);
        Debug.Log("scanData:" + scanData[0].Length);

        // Begin sending the data to the remote device.
        handler.BeginSend(sendData, 0, sendData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private void Send(Socket handler, String data)
    {
        // Convert the string data to byte data using ASCII encoding.
        byte[] byteData = Encoding.ASCII.GetBytes(data);

        // Begin sending the data to the remote device.
        handler.BeginSend(byteData, 0, byteData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }

    private void SendCallback(IAsyncResult ar)
    {
        try
        {
            // Retrieve the socket from the state object.
            Socket handler = (Socket)ar.AsyncState;

            // Complete sending the data to the remote device.
            int bytesSent = handler.EndSend(ar);
            Debug.LogFormat("Sent {0} bytes to client.", bytesSent);

            //この２つはセットでつかるらしい
            //handler.Shutdown(SocketShutdown.Both);
            //handler.Close();

        }
        catch (Exception e)
        {
            Debug.Log(e.ToString());
        }
    }

    private string GetIPAddress(string hostname)
    {
        IPHostEntry host;
        host = Dns.GetHostEntry(hostname);

        foreach (IPAddress ip in host.AddressList)
        {
            if (ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork)
            {
                //System.Diagnostics.Debug.WriteLine("LocalIPadress: " + ip);
                return ip.ToString();
            }
        }
        return string.Empty;
    }

    private float DegreeToRadian(float deg)
    {
        return deg * Mathf.PI / 180;
    }

}
