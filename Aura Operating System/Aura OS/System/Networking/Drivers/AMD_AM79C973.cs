﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Aura_OS.System.Networking;
using Cosmos.HAL;
using static Cosmos.Core.INTs;

namespace Aura_OS.System.Networking.Drivers
{
    unsafe class AMD_AM79C973
    {

        public struct initialization_block {

            public ushort mode;

            public uint reserved1;
            public uint reserved2;
            public ushort reserved3;

            public uint num_send_buffers;
            public uint num_recv_buffers;

            public ulong physical_address;
            public ulong logical_address;

            public uint recv_buffer_desc_address;
            public uint send_buffer_desc_address;

        };

        public struct buffer_desc
        {

            public uint address;
            public uint flags;
            public uint flags2;
            public uint avail;

        };


        static ushort MAC_address_0_port;
        static ushort MAC_address_2_port;
        static ushort MAC_address_4_port;

        static ushort IP_address_0_port;
        static ushort IP_address_2_port;

        static ushort register_data_port;
        static ushort register_address_port;

        static ushort reset_port;
        static ushort bus_control_register_data_port;

        static ulong MAC0;
        static ulong MAC1;
        static ulong MAC2;
        static ulong MAC3;
        static ulong MAC4;
        static ulong MAC5;

        //static uint IP0;
        //static uint IP1;

        //static uint IP;
        static ulong MAC;

        public static initialization_block init_block;

        static buffer_desc* send_buffer_desc;
        //static byte[,] send_buffers = new byte[2 * 1024 + 15, 8];
        static byte[] send_buffers = new byte[2 * 1024 + 15];
        static byte[] send_buffer_desc_memory = new byte[2048 + 15];
        static byte current_send_buffer;

        public static buffer_desc* recv_buffer_desc;
        //static byte[,] recv_buffers = new byte[2 * 1024 + 15, 8];
        static byte[] recv_buffers = new byte[2 * 1024 + 15];
        static byte[] recv_buffer_desc_memory = new byte[2048 + 15];
        static byte current_recv_buffer;

        public static PCIDevice device;

        public static void amd_am79c973_analyse_status()
        {
            CDDI.outw(register_address_port, 0);
            ushort status = CDDI.inw(register_data_port);

            Console.WriteLine("Status: " + status);
            Console.ReadKey();

            if ((status & 0x8000) == 0x8000) Console.WriteLine("AMD am79c973 ERROR");
            if ((status & 0x2000) == 0x2000) Console.WriteLine("AMD am79c973 COLLISION ERROR");
            if ((status & 0x1000) == 0x1000) Console.WriteLine("AMD am79c973 MISSED FRAME");
            if ((status & 0x0800) == 0x0800) Console.WriteLine("AMD am79c973 MEMORY ERROR");
            if ((status & 0x0400) == 0x0400) //123 ?
            {
                Console.WriteLine("AMD am79c973 received data");
                Console.ReadKey();
                amd_am79c973_receive();
            }
            if ((status & 0x0200) == 0x0200) // 499 
            {
                Console.WriteLine("AMD am79c973 sent data"); Console.ReadKey();
            }

            CDDI.outw(register_address_port, 0);
            CDDI.outw(register_data_port, status);
            
            // 115 normal, 123 sent
            //    499       256        256
            if ((status & 0x0100) == 0x0100)
            {
                Console.WriteLine("AMD am79c973 init done");
                Console.ReadKey();
            }

            
        }

        public static void amd_am79c973_handler(ref IRQContext aContext)
        {
	        Console.WriteLine("Interrupt recieved from AMD am79c973 network card. Analysing ...");
            amd_am79c973_analyse_status();
        }

        public static void amd_am79c973_init(PCIDevice _device)
        {
            device = _device;

            MAC_address_0_port = (ushort)(device.BaseAddressBar[0].BaseAddress);
            MAC_address_2_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x02);
            MAC_address_4_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x04);

            //IP_address_0_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x08);
            //IP_address_2_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x10); // NOTE: I have no idea where to find the IP address

            register_data_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x10);
            register_address_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x12);

            reset_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x14);
            bus_control_register_data_port = (ushort)(device.BaseAddressBar[0].BaseAddress + 0x16);

            current_send_buffer = 0;
            current_recv_buffer = 0;

            MAC0 = (ulong)(CDDI.inw(MAC_address_0_port) % 256);
            MAC1 = (ulong)(CDDI.inw(MAC_address_0_port) / 256);
            MAC2 = (ulong)(CDDI.inw(MAC_address_2_port) % 256);
            MAC3 = (ulong)(CDDI.inw(MAC_address_2_port) / 256);
            MAC4 = (ulong)(CDDI.inw(MAC_address_4_port) % 256);
            MAC5 = (ulong)(CDDI.inw(MAC_address_4_port) / 256);

            Console.WriteLine("MAC Address: " + MAC0 + ":"+ MAC1 + ":" + MAC2 + ":" + MAC3 + ":" + MAC4 + ":" + MAC5);

            //IP0 = (uint)(CDDI.inw(IP_address_0_port) % 256);
            //IP1 = (uint)(CDDI.inw(IP_address_2_port) / 256);

            MAC = (MAC5 << 40)
		        | (MAC4 << 32)
		        | (MAC3 << 24)
		        | (MAC2 << 16)
		        | (MAC1 << 8)
		        | (MAC0);

            //IP = ((IP1 << 8)
            //    | IP0);

            //Reset the card
            amd_am79c973_reset();
            Thread.Sleep(10);

            //32 Bits mod
            CDDI.outw(register_address_port, 20);
            CDDI.outw(bus_control_register_data_port, 0x102);


            // STOP reset
            CDDI.outw(register_address_port, 0);
            CDDI.outw(register_data_port, 0x04);

            // initBlock
            init_block.mode = 0x0000;

            init_block.reserved1 = 0;
            init_block.num_send_buffers = 3;
            init_block.reserved2 = 0;
            init_block.num_recv_buffers = 3;
            init_block.physical_address = MAC;
            init_block.reserved3 = 0;
            init_block.logical_address = 0;

            fixed (byte* test = &send_buffer_desc_memory[0])
            {
                send_buffer_desc = (buffer_desc*)(((uint)test+15) & ~((uint)0xF)); ;
            }
            init_block.send_buffer_desc_address = (uint)send_buffer_desc;
            fixed (byte* test = &recv_buffer_desc_memory[0])
            {
                recv_buffer_desc = (buffer_desc*)(((uint)test + 15) & ~((uint)0xF));
            } 
            init_block.recv_buffer_desc_address = (uint)recv_buffer_desc;

            for (ushort i = 0; i < 8; i++)
            {

                fixed (byte* test = &send_buffers[i])
                {
                    send_buffer_desc[i].address = ((((uint)test) + 15) & ~((uint)0xF));
                }


                send_buffer_desc[i].flags = 0x7FF | 0xF000;
                send_buffer_desc[i].flags2 = 0;
                send_buffer_desc[i].avail = 0;

                fixed (byte* test = &recv_buffers[i])
                {
                    recv_buffer_desc[i].address = ((((uint)test) + 15) & ~((uint)0xF));
                }
                
                recv_buffer_desc[i].flags = 0xF7FF | 0x80000000;
                recv_buffer_desc[i].flags2 = 0;
                recv_buffer_desc[i].avail = 0;

            }

            CDDI.outw(register_address_port, 1);

            fixed (initialization_block* test = &init_block)
            {
                CDDI.outw(register_data_port, (ushort)((ushort)(test) & 0xFFFF));
            }

            CDDI.outw(register_address_port, 2);

            fixed (initialization_block* test = &init_block)
            {
                CDDI.outw(register_data_port, (ushort)(((ushort)(test) >> 16) & 0xFFFF));
            }
        }

        public static void amd_am79c973_activate()
        {
            CDDI.outw(register_address_port, 0);
            CDDI.outw(register_data_port, 0x0041); //Init card and enable interrupts, IRQ should arrive NOW

            CDDI.outw(register_address_port, 4);
            ushort status = CDDI.inw(register_data_port);

            CDDI.outw(register_address_port, 4);
            CDDI.outw(register_data_port, (ushort)(status | 0xC00));

            CDDI.outw(register_address_port, 0);
            CDDI.outw(register_data_port, 0x42);

        }

        static int amd_am79c973_reset()
        {
            CDDI.inw(reset_port);
            CDDI.outw(reset_port, 0);

            return 10;

        }

        public static void amd_am79c973_send(uint* data, int _size)
        {
            int send_descriptor = current_send_buffer;
            current_send_buffer = (byte)((current_send_buffer + 1) % 8);
            //Console.WriteLine("WARNING you are trying to send more than 1518 bytes of data. All data that is over that limit will be discarded.");
            if (_size > 1518)
            {
                Console.WriteLine("WARNING you are trying to send more than 1518 bytes of data. All data that is over that limit will be discarded.");
                _size = 1518;

            }

            byte *source = (byte*)(data + _size - 1);
            byte *destination = (byte*)(send_buffer_desc[send_descriptor].address + _size - 1);
            for (; source >= data; source--, destination--)
            {
                *destination = *source;

            }

            send_buffer_desc[send_descriptor].avail = 0;
            send_buffer_desc[send_descriptor].flags2 = 0;
            send_buffer_desc[send_descriptor].flags = 0x8300F000 | ((ushort)((-_size) & 0xFFF));

            CDDI.outw(register_address_port, 0);
            CDDI.outw(register_data_port, 0x48);

        }

        static void amd_am79c973_receive()
        {
            for (; (recv_buffer_desc[current_recv_buffer].flags & 0x80000000) == 0; current_recv_buffer = (byte)((current_recv_buffer + 1) % 8))
            {
                if (!((recv_buffer_desc[current_recv_buffer].flags & 0x40000000) == 00) && (recv_buffer_desc[current_recv_buffer].flags & 0x03000000) == 0x03000000)
                {
                    uint _size = recv_buffer_desc[current_recv_buffer].flags & 0xFFF;

                    if (_size > 64)
                    {
                        _size -= 4;

                    }

                    byte* buffer = (byte*)(recv_buffer_desc[current_recv_buffer].address);

                    int i;
                    for (i = 0; i < _size; i++)
                    {
                        Console.WriteLine(buffer[i]);

                    }

                    recv_buffer_desc[current_recv_buffer].flags2 = 0;
                    recv_buffer_desc[current_recv_buffer].flags = 0x8000F7FF;

                }

            }

        }

        static ulong amd_am79c973_MAC_address()
        {
            return MAC;

        }

        //static uint amd_am79c973_IP_address()
        //{
        //    return IP;

        //}

    }
}