﻿using ShimmerAPI.Radios;
using ShimmerAPI.Utilities;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace ShimmerAPI.Protocols
{
    public class SpeedTestProtocol
    {
        AbstractRadio Radio;
        protected byte[] OldTestData = new byte[0];
        protected bool TestFirstByteReceived = false;
        protected long TestSignalTotalNumberOfBytes = 0;
        protected long TestSignalTotalEffectiveNumberOfBytes = 0;
        protected long NumberofBytesDropped = 0;
        protected double TestSignalTSStart = 0;
        protected bool TestSignalEnabled = false;
        protected bool ProcessData = false;

        ConcurrentQueue<byte> cq = new ConcurrentQueue<byte>();

        public SpeedTestProtocol(AbstractRadio radio)
        {
            Radio = radio;
            Radio.BytesReceived += Radio_BytesReceived;
        }

        public void Connect()
        {
            Radio.Connect();
        }

        public void Disconnect()
        {
            Radio.Disconnect();
        }

        public void StopTestSignal()
        {
            OldTestData = new byte[0];
            TestFirstByteReceived = false;
            TestSignalTotalNumberOfBytes = 0;
            TestSignalTotalEffectiveNumberOfBytes = 0;
            NumberofBytesDropped = 0;
            System.Console.WriteLine("Stop Test Signal");
            TestSignalTSStart = (DateTime.UtcNow - ShimmerBluetooth.UnixEpoch).TotalMilliseconds;
            if (Radio.WriteBytes(new byte[2] { (byte)0xA4, (byte)0x00 }))
            {
                TestSignalEnabled = false;
            }
        }

        public void StartTestSignal()
        {
            Thread thread = new Thread(ProcessDataPackets);
            // Start the thread
            thread.Start();
            OldTestData = new byte[0];
            TestFirstByteReceived = false;
            TestSignalTotalNumberOfBytes = 0;
            System.Console.WriteLine("Start Test Signal");
            TestSignalTSStart = (DateTime.UtcNow - ShimmerBluetooth.UnixEpoch).TotalMilliseconds;
            if (Radio.WriteBytes(new byte[2] { (byte)0xA4, (byte)0x01 }))
            {
                TestSignalEnabled = true;
            }
        }

        private void ProcessDataPackets()
        {
            ProcessData = true;
            int lengthOfPacket = 5;
            int value = 0;
            while (ProcessData)
            {

                if (TestSignalEnabled)
                {
                    int qSize = cq.Count();
                    if (qSize > 0)
                    {
                        byte[] buffer = DequeueBytes(cq, qSize);
                        if (!TestFirstByteReceived)
                        {
                            Console.WriteLine("DISCARD BYTE");
                            TestFirstByteReceived = true;
                            ProgrammerUtilities.CopyAndRemoveBytes(ref buffer, 1);

                        }

                        TestSignalTotalNumberOfBytes += buffer.Length;
                        /*
                        Console.WriteLine();
                        Debug.WriteLine(ProgrammerUtilities.ByteArrayToHexString(buffer));
                        */

                        byte[] data = OldTestData.Concat(buffer).ToArray();
                        //byte[] data = newdata;
                        double testSignalCurrentTime = (DateTime.UtcNow - ShimmerBluetooth.UnixEpoch).TotalMilliseconds;
                        double duration = (testSignalCurrentTime - TestSignalTSStart) / 1000.0; //make it seconds
                        //Console.WriteLine("Throughput (bytes per second): " + (TestSignalTotalNumberOfBytes / duration));
                        //Console.WriteLine("RXB OTD:" + BitConverter.ToString(OldTestData).Replace("-", ""));
                        //Console.WriteLine("RXB:" + BitConverter.ToString(data).Replace("-", ""));
                        int charPrintCount = 0;
                        while(data.Length >= lengthOfPacket+1)
                        {
                            if (data[0] == 0XA5 && data[5] == 0XA5)
                            {
                                byte[] bytesFullPacket = new byte[lengthOfPacket];
                                System.Array.Copy(data, 0, bytesFullPacket, 0, lengthOfPacket);
                                data = ProgrammerUtilities.RemoveBytesFromArray(data, lengthOfPacket);
                                if (bytesFullPacket[0] == 0xA5)
                                {
                                    TestSignalTotalEffectiveNumberOfBytes += 5;
                                    //Array.Reverse(bytes);
                                    byte[] bytes = new byte[lengthOfPacket - 1];
                                    System.Array.Copy(bytesFullPacket, 1, bytes, 0, bytes.Length);
                                    int intValue = BitConverter.ToInt32(bytes, 0);
                                    if (((intValue - value) >= 0) && ((intValue-value) < 1000))
                                    {

                                    } else
                                    {
                                        Console.WriteLine("ERROR WITH PARSING");
                                        StopTestSignal();
                                    }
                                    value = intValue;
                                    var intValueString = intValue.ToString();
                                    Console.Write(intValueString + " , ");
                                    charPrintCount+= intValueString.Length;
                                    if (charPrintCount % 120 == 0)
                                    {
                                        Console.WriteLine();
                                    }
                                }
                            } else
                            {
                                data = ProgrammerUtilities.RemoveBytesFromArray(data, 1);
                                NumberofBytesDropped++;
                            }
                        }
                        testSignalCurrentTime = (DateTime.UtcNow - ShimmerBluetooth.UnixEpoch).TotalMilliseconds;
                        duration = (testSignalCurrentTime - TestSignalTSStart) / 1000.0; //make it seconds
                        Console.WriteLine();
                        Console.WriteLine("Effective Throughput (bytes per second): " + (TestSignalTotalEffectiveNumberOfBytes / duration) +  " Number of Bytes Dropped (Duration S): " + NumberofBytesDropped + "("+ duration + ")");
                        

                        int remainder = data.Length % lengthOfPacket;
                        if (remainder != 0)
                        {
                            OldTestData = new byte[remainder];
                            System.Array.Copy(data, data.Length - remainder, OldTestData, 0, remainder);
                        }
                        else
                        {
                            OldTestData = new byte[0];
                        }
                    }
                }
            }
        }

        private void Radio_BytesReceived(object sender, byte[] buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                cq.Enqueue(buffer[i]);
            }

        }

        static byte[] DequeueBytes(ConcurrentQueue<byte> queue, int count)
        {
            byte[] result = new byte[count];
            for (int i = 0; i < count; i++)
            {
                if (queue.TryDequeue(out byte dequeuedByte))
                {
                    result[i] = dequeuedByte;
                }
                else
                {
                    // Queue is empty before dequeuing the desired count of bytes
                    // You can handle this case based on your requirements
                    Array.Resize(ref result, i);
                    break;
                }
            }
            return result;
        }


    }
}
