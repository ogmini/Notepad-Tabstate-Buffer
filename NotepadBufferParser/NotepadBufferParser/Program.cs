using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NotepadBufferParser
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            string f = "d.bin";
            string fileName = @"C:\\Users\\Reversing\\Desktop\\Copies\\1\\" + f;

            using (var stream = File.OpenRead(fileName))
            {
                using (var reader = new BinaryReader(stream))
                {
                    string hdrType = Encoding.ASCII.GetString(reader.ReadBytes(2));
                    Console.WriteLine(hdrType);
                    byte[] hdr = reader.ReadBytes(15); //Header ignore for now //TODO: Determine what this is

                    if (hdrType == "NP")
                    {

                        while (reader.BaseStream.Length > reader.BaseStream.Position)
                        {
                            var charPos = ReadLEB128Unsigned(stream, out int nextBytePos); 
                            var charDeletion = ReadLEB128Unsigned(stream, out int nextByteDel);
                            var charAddition = ReadLEB128Unsigned(stream, out int nextByteAdd);

                            //TODO: Clean this up. It is not optimal if/else
                            if (charDeletion == 0 && charAddition > 0)
                            {
                                for (int p = 0; p < (int)charAddition; p++)
                                {
                                    var bytesChar = reader.ReadBytes(2);
                                    var str = Encoding.Unicode.GetString(bytesChar);

                                    Console.WriteLine("Addition at Position " + ((int)charPos + p).ToString() + " - Character " + str + " | " + bytesChar[0].ToString("X2"));
                                }
                            }
                            else if (charDeletion > 0 && charAddition == 0)
                            {
                                Console.WriteLine("Deletion at Position " + charPos.ToString() + " for " + charDeletion.ToString() + " position(s)");
                            }
                            else if (charDeletion > 0 && charAddition > 0)
                            {
                                Console.WriteLine("Deletion at Position " + charPos.ToString() + " for " + charDeletion.ToString() + " position(s)");
                                for (int p = 0; p < (int)charAddition; p++)
                                {
                                    var bytesChar = reader.ReadBytes(2);
                                    var str = Encoding.Unicode.GetString(bytesChar);

                                    Console.WriteLine("Insertion at Position " + ((int)charPos + p).ToString() + " - Character " + str + " | " + bytesChar[0].ToString("X2"));
                                }
                            }
                            else
                            {
                                Console.WriteLine("Uhh"); 
                            }   

                            var unKnown = reader.ReadBytes(4); //TODO: Determine what this is
                        }
                        Console.WriteLine("End of Stream");
                    }
                    else
                    {
                        Console.WriteLine("Invalid File");
                    }
                }
            }

            Console.ReadLine();
        }

        private static string ReadCharacter(byte charByte)
        {
            char c = Convert.ToChar(charByte);
            if (char.IsWhiteSpace(c))
            {
                return "0x" + charByte.ToString("X2");
            }
            else
            {
                return c.ToString();
            }

            //TODO: Shit way of doing this
            //switch(chunk[3])
            //{
            //    case 13:
            //        charFound = "New Line";
            //        break;
            //    case 32:
            //        charFound = "Space";
            //        break;
            //    default:
            //        charFound = Convert.ToChar(chunk[3]).ToString();
            //        break;
            //}

            // 13 ius new line
            //32 is space
        }
        private static ulong ReadLEB128Unsigned(this Stream stream, out int bytes)
        {
            bytes = 0;

            ulong value = 0;
            int shift = 0;
            bool more = true;

            while (more)
            {
                var next = stream.ReadByte();
                if (next < 0) { throw new InvalidOperationException("Unexpected end of stream"); }

                byte b = (byte)next;
                bytes += 1;

                more = (b & 0x80) != 0;   // extract msb
                ulong chunk = b & 0x7fUL; // extract lower 7 bits
                value |= chunk << shift;
                shift += 7;
            }

            return value;
        }


    }
}
