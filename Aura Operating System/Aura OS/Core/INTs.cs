﻿/*
* PROJECT:          Aura Operating System Development
* CONTENT:          CPU Exceptions.
* PROGRAMMERS:      Valentin Charbonnier <valentinbreiz@gmail.com>
*/

using IL2CPU.API.Attribs;
using System;
using static Cosmos.Core.INTs;

namespace Aura_OS.Core
{
    [Plug(Target = typeof(Cosmos.Core.INTs))]

    public class INTs
    {
        public static void HandleException(uint aEIP, string aDescription, string aName, ref IRQContext ctx, uint lastKnownAddressValue = 0)
        {
            const string xHex = "0123456789ABCDEF";

            string ctxinterrupt = "";
            ctxinterrupt = ctxinterrupt + xHex[(int)((ctx.Interrupt >> 4) & 0xF)];
            ctxinterrupt = ctxinterrupt + xHex[(int)(ctx.Interrupt & 0xF)];

            string lastsknowaddress = "";

            if (lastKnownAddressValue != 0)
            {
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 28) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 24) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 20) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 16) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 12) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 8) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)((lastKnownAddressValue >> 4) & 0xF)];
                lastsknowaddress = lastsknowaddress + xHex[(int)(lastKnownAddressValue & 0xF)];
            }

            System.Crash.StopKernel(aName, aDescription, lastsknowaddress, ctxinterrupt);
        }

        //IRQ 09 - (Added for AMD PCNet network card)
        //public static IRQDelegate IRQ09;
        //public static void HandleInterrupt_29(ref IRQContext aContext)
        //{
          //  Console.WriteLine("HandleInterrupt_29");
            //Cosmos.Core.INTs.IRQ(0x29, ref aContext);
            //Global.PIC.EoiSlave();
        //}
    }
}