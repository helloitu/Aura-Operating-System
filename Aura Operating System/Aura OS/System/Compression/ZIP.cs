﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Aura_OS.System.Compression
{
    class ZIP
    {
        private string ZIPFile;
        int pointer = 0;

        public ZIP(string filename)
        {
            ZIPFile = filename;
        }

        private Byte[] FileHeader()
        {
            Byte[] zip = File.ReadAllBytes(ZIPFile);
            return zip;            
        }

        private bool IsZIPFile()
        {
            Byte[] zip = FileHeader();
            if ((zip[0] == 80) && (zip[1] == 75) && (((zip[2] == 3) && (zip[3] == 4)) || ((zip[2] == 5) && (zip[6] == 6)))) //80 75 3 4 or 80 75 5 6 
                                                                                                                            //(first for a zip file and second for an empty zip file)
            {
                return true;
            }
            return false;
        }

        private bool IsVersion()
        {
            Byte[] zip = FileHeader();
            if ((zip[4] == 10) && (zip[5] == 00))
            {
                return true;
            }
            return false;
        }

        private void ListFiles()
        {
            Byte[] zip = FileHeader();
            Byte[] test = zip;
            List<Byte> bname = new List<Byte>();
            List<string> signatures = new List<string>();

            //detect all 80 75 3 4
            //register their positions
            //get their names
            string signature = "";
            int pointer = -4;

            foreach (Byte file in test)
            {
                pointer = pointer + 1;
                signature = signature + file;

                if (signature.Contains("807534"))
                {
                    signature = "";
                    signatures.Add("1 file found > " + pointer);
                }
            }

            foreach (string sign in signatures)
            {
                Console.WriteLine(sign);
            }

            //int lenght = zip[26] + zip[27]; //13 + 0 = 13
            //Console.WriteLine("zip> filename lenght: " + lenght);
            //for (int i = 30; i < 30+lenght; i++){
            //    bname.Add(zip[i]);
            //    pointer = i;
            //    Console.WriteLine("zip> byte 0x" + pointer + " " + zip[i]);
            //}
            //pointer = pointer + 1;
        }

        public void Open()
        {
            Byte[] zip = FileHeader();
            if (IsZIPFile()) //if it's a zip file
            {
                if (IsVersion()) //if the zip file is supported
                {
                    Console.ForegroundColor = ConsoleColor.Green;                    
                    Console.WriteLine("Good version: executing zip file...");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                ListFiles();
            }
        }
    }
}
