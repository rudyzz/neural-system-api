﻿/// Final Project Code
/// Written by Yudi Zhang

using System.Collections.Concurrent;
using System.Drawing;
using System.Net.Mail;

namespace DistributedDLL
{
    enum PacketProcessState
    {
        SEEKING_HEADER,
        SEEKING_MSG_BODY
    }

    public partial class DistributedAPI
    {
        private const int STREAM_BUFFER_CAPACITY = 100;

        private const double DYNAMIC_RANGE = 3932.0;
        private const double X_MIN = -1885.0032958984373;

        private TextWriter? _fsWriter = null;

        private readonly BlockingCollection<Packet> _streamBuffer;
        private readonly ConcurrentQueue<Request> _pendingRequests;

        private Thread _serialRxThread, _streamLoggerThread;

        public event EventHandler<StreamDataEventArgs>? StreamDataEvent;

        /// <summary>
        /// Executes a transaction with the distributed system which begins data streaming. Additionally opens a new file to log the data streamed to.
        /// </summary>
        public void StartStreaming()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            // Check if we're already streaming.
            if (this.IsStreaming)
            {
                Console.WriteLine("Distributed Interface DLL - Error: Can't start streaming since we are already streaming.");
                return;
            }

            Packet req = new(PacketType.TRANSACTION, this._packetID, new byte[1] { (byte)OpCode.STREAM_BEGIN });
            Packet? response = this.SendPacket(req);

            if (response == null)
            {
                Console.WriteLine("Distributed Interface DLL - Start Streaming: Operation timed out.");
                return;
            }

            if (!req.bytes.SequenceEqual(response.bytes))
            {
                if (response.Payload[0] == (byte)ErrCode.BAD_CHECKSUM)
                {
                    Console.WriteLine("Distributed Interface DLL - Start Streaming: bad checksum received by Arduino");
                }
                else if (response.Payload[0] == (byte)ErrCode.PAYLOAD_LENGTH_EXCEEDS_MAX)
                {
                    Console.WriteLine("Distributed Interface DLL - Start Streaming: payload too large.");
                }

                return;
            }

            Console.WriteLine("Distributed Interface DLL - Streaming Started");

            // Create a new log file if the file logger is null
            this._fsWriter ??= new StreamWriter($"StreamLogFile_{DateTime.Now.Ticks}.txt");


            // Send the command to start streaming
            this.IsStreaming = true;
        }

        /// <summary>
        /// Executes a transaction with the distributed system which stops data streaming.
        /// </summary>>
        public void StopStreaming()
        {
            if (this.IsDisposed)
            {
                throw new ObjectDisposedException(this.ToString());
            }

            // Check if we're already streaming.
            if (!this.IsStreaming)
            {
                Console.WriteLine("Distributed Interface DLL - Error: Can't stop streaming since we aren't currently streaming.");
                return;
            }

            Packet req = new(PacketType.TRANSACTION, this._packetID, new byte[1] { (byte)OpCode.STREAM_END });
            Packet? response = this.SendPacket(req);

            if (response == null)
            {
                Console.WriteLine($"Distributed Interface DLL - Watchdog Service Error: Operation timed out.");
                return;
            }

            if (!req.bytes.SequenceEqual(response.bytes))
            {
                if (response.Payload[0] == (byte)ErrCode.BAD_CHECKSUM)
                {
                    Console.WriteLine("Distributed Interface DLL - Watchdog Service Error: bad checksum received by Arduino");
                }
                else if (response.Payload[0] == (byte)ErrCode.PAYLOAD_LENGTH_EXCEEDS_MAX)
                {
                    Console.WriteLine("Distributed Interface DLL - Watchdog Service Error: payload too large.");
                }

                return;
            }

            Console.WriteLine("Distributed Interface DLL - Streaming Stopped");

            this.IsStreaming = false;
        }


        private static double DecodeStreamData(Packet streamPacket)
        {
            ushort data = (ushort)(streamPacket.Payload[4] | streamPacket.Payload[5] << 8);
            return data / 65536.0 * DYNAMIC_RANGE + X_MIN;
        }

        private static uint DecodeStreamTimestamp(Packet streamPacket)
        {
            uint timestamp = 0;

            for (int i = 0; i < 4; i++)
            {
                timestamp |= (uint)(streamPacket.Payload[i] << (8 * i));
            }

            return timestamp;
        }

        private void StreamLogThreadHandler()
        {
            while (this.IsConnected)
            {
                if (this._streamBuffer.TryTake(out Packet? streamPacket, 100))
                {
                    double data = DistributedAPI.DecodeStreamData(streamPacket);
                    uint timestamp_ms = DistributedAPI.DecodeStreamTimestamp(streamPacket);

                    this.StreamDataEvent?.Invoke(this, new StreamDataEventArgs(streamPacket.PacketID, 0.001f * timestamp_ms, data));

                    this._fsWriter?.WriteLine(data.ToString() + "," + timestamp_ms.ToString());

                    if (this._signalBuffer.Count >= SIGNAL_BUFFER_CAPACITY)
                    {
                        this._signalBuffer.Take();
                    }
                    this._signalBuffer.Add(data);
                }
            }
        }
        
        /// <summary>
        /// Polls <see cref="DistributedAPI._serialPort"/> for incoming data and routes it accordingly.
        /// </summary>
        private void RxThreadHandler()
        {
            byte[]? recvBuffer;

            while (this.IsConnected)
            {
                try
                {
                    recvBuffer = this.RecvRawPacket();

                    if (recvBuffer == null)
                    {
                        Console.WriteLine("Distributed Interface DLL - RX Thread Error: Invalid Packet.");
                        continue;
                    }
                }
                catch (TimeoutException)
                {
                    // Do nothing, expected
                    continue;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Distributed Interface DLL - RX Thread Error: " + e.Message);
                    continue;
                }

                Packet receivedPacket = new(recvBuffer);

                if (!receivedPacket.IsValidChecksum)
                {
                    continue;
                }

                // We have a valid packet

                if (receivedPacket.PacketType == PacketType.TRANSACTION)
                {
                    while (this._pendingRequests.TryDequeue(out Request? request))
                    {
                        if (receivedPacket.PacketID == request.PacketID)
                        {
                            request.response = receivedPacket;
                            request.RequestWaitHandle.Set();
                            break;
                        }

                        Console.WriteLine($"Distributed Interface DLL - RX Thread Error: request ID {request.PacketID} was dropped.");
                    }
                }
                else if (receivedPacket.PacketType == PacketType.STREAM)
                {
                    // Console.WriteLine("Distributed Interface DLL - Streaming Packet Received");
                    this._streamBuffer.Add(receivedPacket); // Blocks if the buffer is full.
                }
            }
        }
    }
}
