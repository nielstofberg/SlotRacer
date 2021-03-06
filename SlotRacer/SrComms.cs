﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Collections;

namespace SlotRacer
{
    class SrComms
    {
        private static readonly int TIMEOUT = 200;    //! Timeout in Milliseconds
        private static readonly Int32 BAUD_RATE = 19200;
        private static readonly Parity PARITY = Parity.None;
        private static readonly Int32 DATA_BITS = 8;
        private static readonly StopBits STOP_BITS = StopBits.One;
        private static readonly byte[] CRC8_LOOK_UP_TABLE = new byte[256]
        {
            0x00,0x07,0x0e,0x09,0x1c,0x1b,0x12,0x15,0x38,0x3f,0x36,0x31,0x24,0x23,0x2a,0x2d,
            0x70,0x77,0x7E,0x79,0x6C,0x6B,0x62,0x65,0x48,0x4F,0x46,0x41,0x54,0x53,0x5A,0x5D,
            0xE0,0xE7,0xEE,0xE9,0xFC,0xFB,0xF2,0xF5,0xD8,0xDF,0xD6,0xD1,0xC4,0xC3,0xCA,0xCD,
            0x90,0x97,0x9E,0x99,0x8C,0x8B,0x82,0x85,0xA8,0xAF,0xA6,0xA1,0xB4,0xB3,0xBA,0xBD,
            0xC7,0xC0,0xC9,0xCE,0xDB,0xDC,0xD5,0xD2,0xFF,0xF8,0xF1,0xF6,0xE3,0xE4,0xED,0xEA,
            0xB7,0xB0,0xB9,0xBE,0xAB,0xAC,0xA5,0xA2,0x8F,0x88,0x81,0x86,0x93,0x94,0x9D,0x9A,
            0x27,0x20,0x29,0x2E,0x3B,0x3C,0x35,0x32,0x1F,0x18,0x11,0x16,0x03,0x04,0x0D,0x0A,
            0x57,0x50,0x59,0x5E,0x4B,0x4C,0x45,0x42,0x6F,0x68,0x61,0x66,0x73,0x74,0x7D,0x7A,
            0x89,0x8E,0x87,0x80,0x95,0x92,0x9B,0x9C,0xB1,0xB6,0xBF,0xB8,0xAD,0xAA,0xA3,0xA4,
            0xF9,0xFE,0xF7,0xF0,0xE5,0xE2,0xEB,0xEC,0xC1,0xC6,0xCF,0xC8,0xDD,0xDA,0xD3,0xD4,
            0x69,0x6E,0x67,0x60,0x75,0x72,0x7B,0x7C,0x51,0x56,0x5F,0x58,0x4D,0x4A,0x43,0x44,
            0x19,0x1E,0x17,0x10,0x05,0x02,0x0B,0x0C,0x21,0x26,0x2F,0x28,0x3D,0x3A,0x33,0x34,
            0x4E,0x49,0x40,0x47,0x52,0x55,0x5C,0x5B,0x76,0x71,0x78,0x7F,0x6A,0x6D,0x64,0x63,
            0x3E,0x39,0x30,0x37,0x22,0x25,0x2C,0x2B,0x06,0x01,0x08,0x0F,0x1A,0x1D,0x14,0x13,
            0xAE,0xA9,0xA0,0xA7,0xB2,0xB5,0xBC,0xBB,0x96,0x91,0x98,0x9F,0x8A,0x8D,0x84,0x83,
            0xDE,0xD9,0xD0,0xD7,0xC2,0xC5,0xCC,0xCB,0xE6,0xE1,0xE8,0xEF,0xFA,0xFD,0xF4,0xF3
        };

        private static readonly int REPLY_COUNT = 15;
        private static readonly byte START_BUTTON = 0x01;
        private static readonly byte ENTER_BUTTON = 0x08;
        private static readonly byte UP_BUTTON = 0x04;
        private static readonly byte DOWN_BUTTON = 0x20;
        private static readonly byte LEFT_BUTTON = 0x10;
        private static readonly byte RIGHT_BUTTON = 0x02;

        private SerialPort sp1;
        private string portName = string.Empty;

        public bool Connected
        {
            get { return sp1.IsOpen; }
        }

        public SrComms()
        {
            string[] spn = SerialPort.GetPortNames();
            if (spn.Length > 0)
            {
                portName = spn[0];
            }
            sp1 = new SerialPort(portName, BAUD_RATE, PARITY,DATA_BITS, STOP_BITS);
            sp1.ReadTimeout = TIMEOUT;
            sp1.WriteTimeout = TIMEOUT;
        }

        ~SrComms()
        {
            Dispose();
        }

        public void Dispose()
        {
            Disconnect();
        }

        /// <summary>
        /// Connect using the currently selected port name
        /// </summary>
        /// <returns></returns>
        public int Connect()
        {
            try
            {
                sp1.Open();
            }
            catch
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Set portName and connect using the passed port name
        /// </summary>
        /// <param name="port"></param>
        /// <returns></returns>
        public int Connect(string port)
        {
            try
            {
                portName = port;
                sp1.PortName = port;
                sp1.Open();
            }
            catch
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Disconnect/close serial port.
        /// </summary>
        /// <returns></returns>
        public int Disconnect()
        {
            try
            {
                if (sp1.IsOpen)
                {
                    sp1.Close();
                }
            }
            catch
            {
                return 1;
            }
            return 0;
        }

        /// <summary>
        /// Send message to Scalextric based on an OutStatusInstance
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public int SendCommand(OutStatus cmd)
        {
            byte[] message = new byte[9];

            message[0] = 0xff; //last receipt was good
            
            //! Set LEDs in byte 7
            for (int i = 0; i < cmd.Leds.Length; i++)
            {
                if (cmd.Leds[i])
                {
                    message[7] |= (byte)(1 << i);
                }
            }

            //! Set car bytes in byte 1 to 6
            for (int i = 0; i < OutStatus.CAR_COUNT; i++)
            {
                byte carByte = 0x00;
                carByte = (byte) ((cmd.Cars[i].Power> 63) ? 63: cmd.Cars[i].Power);
                if (cmd.Cars[i].LaneChange)
                {
                    carByte |= (1 << 6);
                }
                if (cmd.Cars[i].Brake)
                {
                    carByte |= (1 << 7);
                }
                message[i + 1] = (byte)~carByte;
            }

            message[message.Length-1] = GetChecksum(message);

            try
            {
                if (!sp1.IsOpen)
                {
                    sp1.Open();
                }
                sp1.DiscardInBuffer();
                sp1.DiscardOutBuffer();
                sp1.Write(message, 0, message.Length);
            }
            catch
            {
                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Get a reply from Scalextric 
        /// </summary>
        /// <param name="stat"></param>
        /// <returns></returns>
        public int GetReply(out InStatus stat)
        {
            byte[] message = new byte[REPLY_COUNT];
            DateTime to = DateTime.Now.AddMilliseconds(TIMEOUT);
            stat = new InStatus();
            try
            {
                if (!sp1.IsOpen)
                {
                    sp1.Open();
                }
                //! Wait until whole packet is in buffer
                while (sp1.BytesToRead < REPLY_COUNT)
                {
                    if (DateTime.Now > to)
                    {
                        sp1.ReadExisting();
                        return 1;
                    }
                }
                //! Read packet from buffer
                sp1.Read(message, 0, message.Length);
            }
            catch (TimeoutException)
            {
                return 1;
            }
            catch
            {
                return 3;
            }

            if (message[message.Length - 1] == GetChecksum(message))
            {
                stat = new InStatus();
                stat.Controllers = new Controller[6];
                for (int i = 0; i < 6; i++)
                {
                    stat.Controllers[i].Connected = (message[0] & (1 << (i + 1))) == (1 << (i + 1));
                    if (stat.Controllers[i].Connected)
                    {
                        byte ctrlByte = (byte)~message[i + 1];
                        stat.Controllers[i].ThrottlePosition = ctrlByte & 0x3f;
                        stat.Controllers[i].LaneChangePressed = (ctrlByte & (1 << 6)) == (1 << 6);
                        stat.Controllers[i].BrakeButtonPressed = (ctrlByte & (1 << 7)) == (1 << 7);
                    }
                }

                stat.AuxPortCurrent = message[7];
                LapTime lt = new LapTime();
                int carId = message[8] & 7;
                lt.CarId = carId;
                UInt32 tc = (UInt32)(message[9] | message[10] << 8 | message[11] << 16 | message[12] << 24);
                lt.Time = (long)((tc * 6.4) / 1000);
                stat.LastLapTime = lt;
                stat.StartPressed = (~message[13] & START_BUTTON) == START_BUTTON;
                stat.EnterPressed = (~message[13] & ENTER_BUTTON) == ENTER_BUTTON;
                stat.UpPressed = (~message[13] & UP_BUTTON) == UP_BUTTON;
                stat.DownPressed = (~message[13] & DOWN_BUTTON) == DOWN_BUTTON;
                stat.LeftPressed = (~message[13] & LEFT_BUTTON) == LEFT_BUTTON;
                stat.RightPressed = (~message[13] & RIGHT_BUTTON) == RIGHT_BUTTON;
                stat.TrackPowered = (message[0] & 1) == 1;

                return 0;
            }
            else
            {
                return 2;
            }
        }

        private bool CheckreplyPacket(byte[] packet)
        {
            byte cs = GetChecksum(packet);
            return (packet[packet.Length - 1] == cs) ;
        }

        /// <summary>
        /// Calculate the checksum for a message
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        private byte GetChecksum(byte[] buffer)
        {
            int counts = buffer.Length - 1;
            byte[] from6CPB_Msg = buffer; // new byte[16];

            byte crc8Rx = CRC8_LOOK_UP_TABLE[buffer[0]]; //first byte
            for (int i = 1; i < counts; i++) //loop for 14 times for incoming packet
            {
                crc8Rx = CRC8_LOOK_UP_TABLE[crc8Rx ^ buffer[i]];
            }

            return crc8Rx;
        }
    }

}
