using UnityEngine;
using System.Collections;

using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

public class IPCServerScript : MonoBehaviour
{
    public GameObject urg;
    public GameObject robot;

    private URGScript urgscript;
    private RobotScript robotscript;

    private static bool isCreated = false;

    void Awake()
    {
        if(isCreated)
        {
            Destroy(this);
        }
        else
        {
            DontDestroyOnLoad(this); // シーン読み込みの際に破棄されなくなる
            isCreated = true;
        }
        
    }

    // Use this for initialization
    void Start()
    {
        urgscript = urg.GetComponent<URGScript>();
        robotscript = robot.GetComponent<RobotScript>();

        StartListening();
    }

    // Update is called once per frame
    void Update()
    {

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

                /*
                 *  "lrf [LRFオブジェクトの識別名]" : LRFスキャンデータの要求    
                 *  
                 *  "move ["get" or "send"] 
                 *          [ if "send" ["send mode"] [if "direction" [direction] [value] [speed] [tolerance] else if "coordinate"  ] ]
                 *          [ if "get" ["get mode"] " : 移動情報の送受信
                 *      send mode : direction, coordinate
                 *      get mode : coordinate, isdriving
                 *      direction : forward, right, left, back
                 *      
                 *  "hit" : 衝突しているオブジェクトの数を要求
                 */
                string[] commandArray = content.Split(' ');
                foreach (var item in commandArray)
                {
                    Debug.Log("\"" + item + "\"");
                }
                switch (commandArray[0])
                {
                    // 測域センサの点群情報を送信
                    case "lrf":
                        SendLRFScanData(commandArray[1] , handler);
                        break;

                    // 移動情報の送受信
                    case "move":

                        switch (commandArray[1])
                        {
                            case "get":
                                switch (commandArray[2])
                                {
                                    case "coordinate":
                                        SendMovementState(handler);
                                        break;

                                    case "isdriving":
                                        SendIsDriving(handler);
                                        break;
                                }
                                break;

                            case "send":
                                switch(commandArray[2])
                                {
                                    case "direction":
                                        ApplyMovementCommand(commandArray[3], float.Parse(commandArray[4]), float.Parse(commandArray[5]), float.Parse(commandArray[6]), handler);
                                        break;

                                    case "coordinate":
                                        break;
                                }
                                
                                break;

                            default:
                                // send error
                                break;
	                    }

                        break;

                    // 衝突しているオブジェクトの個数を送信
                    // 強化学習用だっけ？（用途忘れた）
                    case "hit":
                        SendHitCount(handler);
                        break;

                    default:
                        // send error
                        break;
                }

                //clear data in object before next receive
                //StringbuilderクラスはLengthを0にしてクリアする
                state.sb.Length = 0;

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

    private void SendLRFScanData(string name, Socket handler)
    {
        Debug.Log("SendLRFScanData");

        float[][] scanData = urgscript.GetScanData();

        Debug.Log("Making send data");
        int typeSize = sizeof(float);
        // 角度+距離+終了文字
        byte[] sendData = new byte[urgscript.DataSize * 2 * typeSize + typeSize];
        int index = 0;

        foreach (var item in scanData[0])
        {
            Array.Copy((BitConverter.GetBytes(item)), 0, sendData, index * typeSize, typeSize);
            index++;
        }
        foreach (var item in scanData[1])
        {
            Array.Copy((BitConverter.GetBytes(item)), 0, sendData, index * typeSize, typeSize);
            index++;
        }
        Array.Copy((BitConverter.GetBytes(-1000.0f)), 0, sendData, index * typeSize, typeSize);


        Debug.Log("sendData:" + sendData.Length);
        Debug.Log("scanData:" + scanData[0].Length);

        // Begin sending the data to the remote device.
        handler.BeginSend(sendData, 0, sendData.Length, 0,
            new AsyncCallback(SendCallback), handler);

    }
    private void SendMovementState(Socket handler )
    {
        Debug.Log("SendMovementState");
        // [pos, direction]
        Vector3[] vec = robotscript.GetMovement();

        Debug.Log("Making send data");
        int typeSize = sizeof(float);
        byte[] sendData = new byte[vec.Length * 3 * typeSize + typeSize];
        int index = 0;

        float[] data = new float[] { vec[0].x, vec[0].y, vec[0].z, vec[1].x, vec[1].y, vec[1].z };

        foreach (var item in data)
        {
            Array.Copy((BitConverter.GetBytes(item)), 0, sendData, index * typeSize, typeSize);
            index++;
        }
        Array.Copy((BitConverter.GetBytes(-1000.0f)), 0, sendData, index * typeSize, typeSize);

        Debug.Log("sendData:" + sendData.Length);

        // Begin sending the data to the remote device.
        handler.BeginSend(sendData, 0, sendData.Length, 0,
            new AsyncCallback(SendCallback), handler);

    }
    private void SendIsDriving(Socket handler)
    {
        Debug.Log("SendIsDriving");
        float isDriving;
        if (robotscript.IsDriving()) isDriving = 1;
        else isDriving = 0;
        
        Debug.Log("Making send data");
        int typeSize = sizeof(float);
        byte[] sendData = new byte[1 * typeSize + typeSize];
        int index = 0;

        float[] data = new float[] { isDriving };

        foreach (var item in data)
        {
            Array.Copy((BitConverter.GetBytes(item)), 0, sendData, index * typeSize, typeSize);
            index++;
        }
        Array.Copy((BitConverter.GetBytes(-1000.0f)), 0, sendData, index * typeSize, typeSize);

        Debug.Log("sendData:" + sendData.Length);

        // Begin sending the data to the remote device.
        handler.BeginSend(sendData, 0, sendData.Length, 0,
            new AsyncCallback(SendCallback), handler);

    }

    // [m or degree], [ m/s ], [ m or degree ]
    private void ApplyMovementCommand(string direction, float val, float speed, float tolerance, Socket handler)
    {
        robotscript.isAcceptKeyboard = false;
        robotscript.DrivingInstruction(direction, val, speed, tolerance);

        byte[] sendData = BitConverter.GetBytes(-1000.0f);
        // Begin sending the data to the remote device.
        handler.BeginSend(sendData, 0, sendData.Length, 0,
            new AsyncCallback(SendCallback), handler);
    }
    private void SendHitCount(Socket handler)
    {
        int typeSize = sizeof(float);
        byte[] sendData = new byte[typeSize * 2];
        int index = 0;
        Array.Copy( ( BitConverter.GetBytes( (float)robotscript.HitCount ) ), 0, sendData, index * typeSize, typeSize);
        index++;
        Array.Copy((BitConverter.GetBytes(-1000.0f)), 0, sendData, index * typeSize, typeSize);

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

}
