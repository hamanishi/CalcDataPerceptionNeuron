﻿/************************************************************************************
 Copyright: Copyright 2014 Beijing Noitom Technology Ltd. All Rights reserved.
 Pending Patents: PCT/CN2014/085659 PCT/CN2014/071006

 Licensed under the Perception Neuron SDK License Beta Version (the “License");
 You may only use the Perception Neuron SDK when in compliance with the License,
 which is provided at the time of installation or download, or which
 otherwise accompanies this software in the form of either an electronic or a hard copy.

 A copy of the License is included with this package or can be obtained at:
 http://www.neuronmocap.com

 Unless required by applicable law or agreed to in writing, the Perception Neuron SDK
 distributed under the License is provided on an "AS IS" BASIS,
 WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 See the License for the specific language governing conditions and
 limitations under the License.
************************************************************************************/

using System;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

namespace NeuronDataReaderManaged
{
    public enum SocketStatus
    {
        CS_Running,
        CS_Starting,
        CS_OffWork,
    }

    public struct DataVersion
    {
        public byte BuildNumb; // Build number
        public byte Revision; // Revision number
        public byte Minor; // Subversion number
        public byte Major; // Major version number
    }

    /// <summary>
    /// Header format of BVH data
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BvhDataHeader
    {
        public ushort Token1; // Package start token: 0xDDFF
        public DataVersion DataVersion; // Version of community data format: 1.1.0.0
        public ushort DataCount; // Values count
        public byte bWithDisp; // With/out displacement
        public byte bWithReference; // With/out reference bone data at first
        public uint AvatarIndex; // Avatar index

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AvatarName; // Avatar name

        public uint FrameIndex; // Frame data index
        public uint Reserved; // Reserved, only enable this package has 64bytes length
        public uint Reserved1; // Reserved, only enable this package has 64bytes length
        public uint Reserved2; // Reserved, only enable this package has 64bytes length
        public ushort Token2; // Package end token: 0xEEFF
    };

    /// <summary>
    /// Header format of Calc
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CalcDataHeader
    {
        public ushort Token1; // Package start token: 0x88FF
        public DataVersion DataVersion; // Version of community data format. e.g.: 1.0.0.3
        public UInt32 DataCount; // Values count
        public UInt32 AvatarIndex; // Avatar index

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string AvatarName; // Avatar name

        public UInt32 FrameIndex; // Frame data index
        public UInt32 Reserved1; // Reserved, only enable this package has 64bytes length
        public UInt32 Reserved2; // Reserved, only enable this package has 64bytes length
        public UInt32 Reserved3; // Reserved, only enable this package has 64bytes length
        public ushort Token2; // Package end token: 0x99FF
    };

    public struct CalcDataBody
    {
        public Vector3 position;
        public Vector3 velocity;
        public Vector3 accelaration;
        public Quaternion quaternion;
        public Vector3 gyro;

        public CalcDataBody(Vector3 p, Vector3 v, Vector3 a, Quaternion q, Vector3 g)
        {
            position = p;
            velocity = v;
            accelaration = a;
            quaternion = q;
            gyro = g;
        }
    }


    public enum CmdId
    {
        Cmd_BoneSize, // Id used to request bone size from server
        Cmd_AvatarName, // Id used to request avatar name from server
        Cmd_FaceDirection, // Id used to request face direction from server
        Cmd_DataFrequency, // Id used to request data sampling frequency from server
        Cmd_BvhInheritance, // Id used to request bvh inheritance from server
        Cmd_AvatarCount, // Id used to request avatar count from server
        Cmd_CombinationMode, // 
        Cmd_RegisterEvent, // 
        Cmd_SetAvatarName, // 
    };

    // Sensor binding combination mode
    public enum SensorCombinationModes
    {
        SC_ArmOnly, // Left arm or right arm only
        SC_UpperBody, // Upper body, include one arm or both arm, must have chest node
        SC_FullBody, // Full body mode
    };

    /// <summary>
    /// Header format of Command returned from server
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CommandPack
    {
        public UInt16 Token1; // Command start token: 0xAAFF
        public UInt32 DataVersion; // Version of community data format. e.g.: 1.0.0.2
        public UInt32 DataLength; // Package length of command data, by byte.
        public UInt32 DataCount; // Count in data array, related to the specific command.
        public CmdId CommandId; // Identity of command.

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] CmdParaments; // Command paraments

        public UInt32 Reserved1; // Reserved, only enable this package has 32bytes length. Maybe used in the future.
        public UInt16 Token2; // Package end token: 0xBBFF
    };


    /// <summary>
    /// Fetched bone size from server
    /// </summary> 
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct CmdResponseBoneSize
    {
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 60)]
        public string BoneName; // Bone name

        public float BoneLength; // Bone length
    };

    abstract class Session
    {
        Thread receivingThread = null;
        bool exitFlag = false;
        int recvBufferSize = 2048;
        int reserveBufferSize = 4096;
        int headerSize = 64;
        int reserveSize = 0;
        byte[] recvBuffer = null;
        byte[] reserveBuffer = null;
        IntPtr pinnedHeader = IntPtr.Zero;
        IntPtr pinnedData = IntPtr.Zero;
        int dataSize = 0;

        internal static IntPtr GetHashValue(string address, int port)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Parse(address), port);
            return (IntPtr) ep.GetHashCode();
        }

        public Session(int recvBufferSize)
        {
            this.recvBufferSize = recvBufferSize;
            recvBuffer = new byte[recvBufferSize];
        }

        public abstract bool Connect(string address, int port);
        public abstract bool Connect(int port);
        public abstract void Disconnect();
        public abstract void Receive(byte[] buffer, int size, out int receivedSize, int recvWaitTime = 0);
        public abstract IntPtr GetHashValue();

        public void Start()
        {
//            receivingThread = new Thread(new ThreadStart(DoReceive));
            receivingThread = new Thread(new ThreadStart(CalcDoReceive));
            receivingThread.IsBackground = true;
            receivingThread.Start();
        }

        public void Stop()
        {
            ReleaseHeader();
            ReleaseData();

            exitFlag = true;
            while (receivingThread.IsAlive) ;
        }

//        public void DoReceive()
//        {
//            while (!exitFlag)
//            {
//                int recvSize = 0;
//                Receive(recvBuffer, recvBufferSize, out recvSize, 0);
//
//                if (reserveBuffer == null)
//                {
//                    reserveBuffer = new byte[reserveBufferSize];
//                }
//
//                Buffer.BlockCopy(recvBuffer, 0, reserveBuffer, reserveSize, recvSize);
//                reserveSize += recvSize;
//
//                if (reserveSize >= headerSize)
//                {
//                    BvhDataHeader header = AcquireHeader(reserveBuffer);
//                    if (ValidateHeader(header))
//                    {
//                        // check complete packet
//                        int packetSize = headerSize + (int) header.DataCount * sizeof(float);
//                        //Debug.LogFormat( "reserveSize {0} >= {1} packetSize", reserveSize, packetSize );
//                        if (reserveSize >= packetSize)
//                        {
//                            //Debug.LogWarningFormat( "received packet received size = {0}", reserveSize );
//                            if (NeuronDataReader.frameDataReceivedCallback != null)
//                            {
//                                AcquireData(reserveBuffer, header);
//                                NeuronDataReader.frameDataReceivedCallback(IntPtr.Zero, GetHashValue(), pinnedHeader,
//                                    pinnedData);
//                            }
//
//                            // clean reserveBuffer
//                            Buffer.BlockCopy(reserveBuffer, packetSize, reserveBuffer, 0, reserveSize - packetSize);
//                            reserveSize -= packetSize;
//                        }
//                    }
//                    else
//                    {
//                        Debug.LogErrorFormat("Invalid packet HeadToken = {0} TailToken = {1} received size = {2}",
//                            header.Token1.ToString("X4"), header.Token2.ToString("X4"), recvSize);
//                    }
//                }
//            }
//        }

        public void CalcDoReceive()
        {
            while (!exitFlag)
            {
                int recvSize = 0;
                Receive(recvBuffer, recvBufferSize, out recvSize, 0);

                if (reserveBuffer == null)
                {
                    reserveBuffer = new byte[reserveBufferSize];
                }

                Buffer.BlockCopy(recvBuffer, 0, reserveBuffer, reserveSize, recvSize);
                reserveSize += recvSize;

                if (reserveSize >= headerSize)
                {
                    var header = CalcAcquireHeader(reserveBuffer);
//                    Debug.Log(header.AvatarName);
//                    Debug.LogErrorFormat("Invalid packet HeadToken = {0} TailToken = {1} received size = {2}", header.Token1.ToString("X4"), header.Token2.ToString("X4"), recvSize);
                    if (CalcValidateHeader(header))
                    {
                        // check complete packet
                        int packetSize = headerSize + (int) header.DataCount * sizeof(float);
                        Debug.LogFormat("reserveSize {0} >= {1} packetSize", reserveSize, packetSize);
                        if (reserveSize >= packetSize)
                        {
                            //Debug.LogWarningFormat( "received packet received size = {0}", reserveSize );
                            if (NeuronDataReader.calcFrameDataReceivedCallback != null)
                            {
                                AcquireData(reserveBuffer, header);
                                NeuronDataReader.calcFrameDataReceivedCallback(IntPtr.Zero, GetHashValue(), pinnedHeader,
                                    pinnedData);
                            }

                            // clean reserveBuffer
                            Buffer.BlockCopy(reserveBuffer, packetSize, reserveBuffer, 0, reserveSize - packetSize);
                            reserveSize -= packetSize;
                        }
                    }
                    else
                    {
                        Debug.LogErrorFormat("Invalid packet HeadToken = {0} TailToken = {1} received size = {2}",
                            header.Token1.ToString("X4"), header.Token2.ToString("X4"), recvSize);
                    }
                }
            }
        }

        BvhDataHeader AcquireHeader(byte[] buffer)
        {
            if (pinnedHeader == IntPtr.Zero)
            {
                pinnedHeader = Marshal.AllocHGlobal(headerSize);
            }

            Marshal.Copy(buffer, 0, pinnedHeader, headerSize);
            return (BvhDataHeader) Marshal.PtrToStructure(pinnedHeader, typeof(BvhDataHeader));
        }

        CalcDataHeader CalcAcquireHeader(byte[] buffer)
        {
            if (pinnedHeader == IntPtr.Zero)
            {
                pinnedHeader = Marshal.AllocHGlobal(headerSize);
            }

            Marshal.Copy(buffer, 0, pinnedHeader, headerSize);
            return (CalcDataHeader) Marshal.PtrToStructure(pinnedHeader, typeof(CalcDataHeader));
        }

        void ReleaseHeader()
        {
            if (pinnedHeader != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pinnedHeader);
                pinnedHeader = IntPtr.Zero;
            }
        }

        void AcquireData(byte[] buffer, BvhDataHeader header)
        {
            int requiredSize = (int) header.DataCount * sizeof(float);
            if (pinnedHeader == IntPtr.Zero || dataSize < requiredSize)
            {
                ReleaseData();
                pinnedData = Marshal.AllocHGlobal(requiredSize);
                dataSize = requiredSize;
            }

            Marshal.Copy(buffer, 64, pinnedData, requiredSize);
        }

        void AcquireData(byte[] buffer, CalcDataHeader header)
        {
            int requiredSize = (int) header.DataCount * sizeof(float);
            if (pinnedHeader == IntPtr.Zero || dataSize < requiredSize)
            {
                ReleaseData();
                pinnedData = Marshal.AllocHGlobal(requiredSize);
                dataSize = requiredSize;
            }

            Marshal.Copy(buffer, 64, pinnedData, requiredSize);
        }

        void ReleaseData()
        {
            if (pinnedData != IntPtr.Zero)
            {
                Marshal.FreeHGlobal(pinnedData);
                pinnedData = IntPtr.Zero;
            }
        }

        void SwapEndian(byte[] buffer, int offset, int size)
        {
            byte temp = 0;
            int end = size / 2 * 2;
            for (int i = offset; i < end; i += 2)
            {
                temp = buffer[i + 1];
                buffer[i + 1] = buffer[i];
                buffer[i] = temp;
            }
        }

        bool ValidateHeader(BvhDataHeader header)
        {
            return header.Token1 == 0xDDFF && header.Token2 == 0xEEFF;
        }

        bool CalcValidateHeader(CalcDataHeader header)
        {
            return header.Token1 == 0x88FF && header.Token2 == 0x99FF;
        }
    }

    class TCPSession : Session
    {
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        IntPtr hashValue = IntPtr.Zero;

        ManualResetEvent connectEvent = new ManualResetEvent(false);

        void connectedCallBack(IAsyncResult asyncResult)
        {
            connectEvent.Set();
            Socket s = (Socket) asyncResult.AsyncState;
            s.EndConnect(asyncResult);
        }

        public TCPSession(int recvBufferSize)
            : base(recvBufferSize)
        {
        }

        public override bool Connect(string address, int port)
        {
            connectEvent.Reset();
            socket.BeginConnect(address, port, new AsyncCallback(connectedCallBack), socket);

            connectEvent.WaitOne(500);

            if (socket.Connected)
            {
                hashValue = GetHashValue(address, port);
                Start();
                return true;
            }

            return false;
        }

        public override bool Connect(int port)
        {
            return false;
        }

        public override void Disconnect()
        {
            socket.Shutdown(SocketShutdown.Both);
            socket.Disconnect(false);
            Stop();
        }

        public override void Receive(byte[] buffer, int size, out int receivedSize, int recvWaitTime = 0)
        {
            //if( socket.Poll( recvWaitTime, SelectMode.SelectRead ) )
            {
                receivedSize = socket.Receive(buffer, size, SocketFlags.None);
            }
            //else
            //{
            //	receivedSize = 0;
            //}
        }

        public override IntPtr GetHashValue()
        {
            return hashValue;
        }
    }

    class UDPSession : Session
    {
        IntPtr hashValue = IntPtr.Zero;
        Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
        IPEndPoint sourceAddress = new IPEndPoint(IPAddress.Any, 0);

        public UDPSession(int recvBufferSize)
            : base(recvBufferSize)
        {
        }

        public override bool Connect(string address, int port)
        {
            sourceAddress = new IPEndPoint(IPAddress.Parse(address), port);
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            hashValue = GetHashValue("0.0.0.0", port);
            Start();
            return true;
        }

        public override bool Connect(int port)
        {
            socket.Bind(new IPEndPoint(IPAddress.Any, port));
            hashValue = GetHashValue("0.0.0.0", port);
            Start();
            return true;
        }

        public override void Disconnect()
        {
            socket.Close();
            Stop();
        }

        public override void Receive(byte[] buffer, int size, out int receivedSize, int recvWaitTime = 0)
        {
            //if( socket.Poll( recvWaitTime, SelectMode.SelectRead ) )
            {
                EndPoint remote = new IPEndPoint(IPAddress.Any, 0);
                try
                {
                    receivedSize = socket.ReceiveFrom(buffer, size, SocketFlags.None, ref remote);
                    if (sourceAddress.Address != IPAddress.Any &&
                        sourceAddress.Address != ((IPEndPoint) remote).Address)
                    {
                        receivedSize = 0;
                    }
                }
                catch (Exception e)
                {
                    receivedSize = 0;
                    Debug.LogException(e);
                }
            }
            //else
            //{
            //	receivedSize = 0;
            //}
        }

        public override IntPtr GetHashValue()
        {
            return hashValue;
        }
    }

//    public delegate void FrameDataReceived(IntPtr customObject, IntPtr sockRef, IntPtr bvhDataHeader, IntPtr data);
    public delegate void CalcFrameDataReceived(IntPtr customObject, IntPtr sockRef, IntPtr CalcDataHeader, IntPtr data);

    
    public delegate void SocketStatusChanged(IntPtr customObject, IntPtr sockRef, SocketStatus status,
        [MarshalAs(UnmanagedType.LPStr)] string msg);

    public delegate void CommandDataReceived(IntPtr customedObj, IntPtr sockRef, IntPtr cmdHeader, IntPtr cmdData);

    public class NeuronDataReader
    {
        static int MaxPacketSize = 64 + 60 * 6 * sizeof(float);

        static Dictionary<IntPtr, Session> sessions = new Dictionary<IntPtr, Session>();
//        public static FrameDataReceived frameDataReceivedCallback;
        public static CalcFrameDataReceived calcFrameDataReceivedCallback;
        public static SocketStatusChanged socketStatusChangedCallback;

//        public static void BRRegisterFrameDataCallback(IntPtr customedObj, FrameDataReceived handle)
//        {
//            frameDataReceivedCallback = handle;
//        }

        public static void BRRegisterFrameDataCallback(IntPtr customedObj,CalcFrameDataReceived handle)
        {
            calcFrameDataReceivedCallback = handle;
        }
        
        public static void BRRegisterSocketStatusCallback(IntPtr customedObj, SocketStatusChanged handle)
        {
            socketStatusChangedCallback = handle;
        }

        public static IntPtr BRConnectTo(string serverIP, int nPort)
        {
            Session newSession = new TCPSession(MaxPacketSize);
            if (newSession.Connect(serverIP, nPort))
            {
                IntPtr key = Session.GetHashValue(serverIP, nPort);
                sessions.Add(key, newSession);
                return key;
            }

            return IntPtr.Zero;
        }

        public static IntPtr BRStartUDPServiceAt(int nPort)
        {
            Session newSession = new UDPSession(MaxPacketSize);
            if (newSession.Connect(nPort))
            {
                sessions.Add(Session.GetHashValue("0.0.0.0", nPort), newSession);
                return newSession.GetHashValue();
            }

            return IntPtr.Zero;
        }

        public static void BRCloseSocket(IntPtr sockRef)
        {
            Session session = null;
            if (sessions.TryGetValue(sockRef, out session))
            {
                session.Disconnect();
            }

            sessions.Remove(sockRef);
        }

        // dummy functions
        public static void BRRegisterCommandDataCallback(IntPtr customObj, CommandDataReceived commandCallback)
        {
        }

        public static bool BRCommandFetchAvatarDataFromServer(IntPtr sockRef, int avatarIndex, CmdId cmdId)
        {
            return false;
        }


        public static bool BRCommandFetchDataFromServer(IntPtr sockRef, CmdId cmdId)
        {
            return false;
        }
    }
}