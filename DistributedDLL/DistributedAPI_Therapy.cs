/// Final Project Code
/// Written by Yudi Zhang

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DistributedDLL
{
    public partial class DistributedAPI
    {
        private const int SIGNAL_BUFFER_CAPACITY = 178;
        private BlockingCollection<double> _signalBuffer;

        private Thread _therapyControllerThread;

        private LDA lda;

        private Packet _therapyStartPacket = new(PacketType.TRANSACTION, 0, new byte[1] { (byte)OpCode.STIMULATION_BEGIN });
        private Packet _therapyStopPacket = new(PacketType.TRANSACTION, 0, new byte[1] { (byte)OpCode.STIMULATION_END });

        /// <summary>
        /// Executes a transaction with the distributed system which starts therapy
        /// </summary>
        private void StartTherapy()
        {
            // Check if already started therapy
            if (this.IsTherapyEnabled)
            {
                Console.WriteLine("Distributed Interface DLL - Error: Can't start therapy since therapy has already started.");
                return;
            }

            this._therapyStartPacket.UpdatePacketID(this._packetID++);
            this.SendPacket(this._therapyStartPacket);

            this.IsTherapyEnabled = true;
        }

        /// <summary>
        /// Executes a transaction with the distributed system which stops therapy
        /// </summary>
        private void StopTherapy()
        {
            // Check if not started therapy yet
            if (!this.IsTherapyEnabled)
            {
                Console.WriteLine("Distributed Interface DLL - Error: Can't stop therapy since therapy has not started yet.");
                return;
            }

            this._therapyStopPacket.UpdatePacketID(this._packetID++);
            this.SendPacket(this._therapyStopPacket);

            this.IsTherapyEnabled = false;
        }

        private void TherapyControlThreadHandler()
        {
            while (this.IsConnected)
            {
                if (this.IsStreaming && this._signalBuffer.Count == SIGNAL_BUFFER_CAPACITY)
                {
                    // Console.WriteLine(this._signalBuffer.Count);
                    double[] signal = this._signalBuffer.ToArray();
                    bool seizure = this.lda.seizurePositive(signal);
                    if (!this.IsTherapyEnabled && seizure)
                    {
                        this.StartTherapy();
                    } else if (this.IsTherapyEnabled && !seizure)
                    {
                        this.StopTherapy();
                    }

                    Console.WriteLine($"Distributed Interface DLL - Seizure: {seizure}.");
                }
                
            }
        }
    }
}
