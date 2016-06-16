using UnityEngine;
using System.Collections;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

public class URGScript : MonoBehaviour
{

    //測距範囲
    public float maxDistance;   //距離
    public float minDistance;
    public float maxAngle;  //角度
    public float minAngle;

    public float stepAngle; //角度分解能[deg]
    public float scanTime;  //走査時間[s]

    private Ray ray;
    private float timer;    //操作時間調節用タイマー

    void scan()
    {
        // 現在の経過時間を取得
        float check_time = Time.realtimeSinceStartup;

        for (float angle = minAngle; angle <= maxAngle; angle += stepAngle)
        {
            ray = new Ray(transform.position, Quaternion.AngleAxis(angle, -transform.up) * transform.forward);
            // Rayの可視化
            Debug.DrawRay(ray.origin, ray.direction, Color.red, 0.1f);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, maxDistance))
            {
                // Rayの原点から衝突地点までの距離を得る
                float dis = hit.distance;
                Debug.Log(dis);
            }
        }

        // 処理完了後の経過時間から、保存していた経過時間を引く＝処理時間
        check_time = Time.realtimeSinceStartup - check_time;

        Debug.Log("check time : " + check_time.ToString("0.00000"));
    }
    

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {

        timer += Time.deltaTime;

        if (timer >= scanTime)
        {
            timer = 0;
            scan();
        }
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
//
// とりあえず断念
//

public class AsynchronousSocketListener 
{
    // Thread signal.
    public static ManualResetEvent allDone = new ManualResetEvent(false);
    Socket listener;

    public AsynchronousSocketListener()
    {
    }

    protected void Start()
    {
        // Data buffer for incoming data.
        byte[] bytes = new Byte[1024];

        // Establish the local endpoint for the socket.
        // The DNS name of the computer
        // running the listener is "host.contoso.com".
        IPHostEntry ipHostInfo = Dns.Resolve(Dns.GetHostName());
        IPAddress ipAddress = ipHostInfo.AddressList[0];
        IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11000);

        // Create a TCP/IP socket.
        listener = new Socket(AddressFamily.InterNetwork,
            SocketType.Stream, ProtocolType.Tcp);

        // Bind the socket to the local endpoint and listen for incoming connections.
        listener.Bind(localEndPoint);
        listener.Listen(100);
        
    }

    protected void Update()
    {
        // Set the event to nonsignaled state.
        allDone.Reset();

        // Start an asynchronous socket to listen for connections.
        Debug.Log("Waiting for a connection...");
        listener.BeginAccept(
            new AsyncCallback(AcceptCallback),
            listener);

        // Wait until a connection is made before continuing.
        allDone.WaitOne();
    }

    public void AcceptCallback(IAsyncResult ar)
    {
        // Signal the main thread to continue.
        allDone.Set();

        // Get the socket that handles the client request.
        Socket listener = (Socket)ar.AsyncState;
        Socket handler = listener.EndAccept(ar);

        // Create the state object.
        StateObject state = new StateObject();
        state.workSocket = handler;
        handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
            new AsyncCallback(ReadCallback), state);
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
            state.sb.Append(Encoding.ASCII.GetString(
                state.buffer, 0, bytesRead));

            // Check for end-of-file tag. If it is not there, read 
            // more data.
            content = state.sb.ToString();
            if (content.IndexOf("<EOF>") > -1)
            {
                // All the data has been read from the 
                // client. Display it on the console.
                Console.WriteLine("Read {0} bytes from socket. \n Data : {1}",
                    content.Length, content);
                // Echo the data back to the client.
                Send(handler, content);
            }
            else
            {
                // Not all data received. Get more.
                handler.BeginReceive(state.buffer, 0, StateObject.BufferSize, 0,
                new AsyncCallback(ReadCallback), state);
            }
        }
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
            Console.WriteLine("Sent {0} bytes to client.", bytesSent);

            handler.Shutdown(SocketShutdown.Both);
            handler.Close();

        }
        catch (Exception e)
        {
            Console.WriteLine(e.ToString());
        }
    }
}