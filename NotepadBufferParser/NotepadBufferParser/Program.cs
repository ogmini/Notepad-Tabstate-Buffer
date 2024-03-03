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
            //string fileName = "C:\\Users\\Reversing\\Desktop\\Copies\\8a547232-5156-490b-96d9-f9d48ec9c325.bin";
            //string fileName = "C:\\Users\\Reversing\\Desktop\\Copies\\e893561e-87f3-4f6b-b1a3-5612df9c96c9.bin";
            string f = "2.bin";
            //83ac1000-f1ce-4e7d-9919-feb0bc0415c5.bin
            string fileName = "C:\\Users\\Reversing\\Desktop\\Copies\\1\\" + f;
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
                            var charPos = ReadLEB128Unsigned(stream, out int nextByte); //Position encoded as unsigned LEB128
                            var charAction = reader.ReadByte(); //TODO: Verify/expand on this

                            switch (charAction)
                            {
                                case 0: //Insertion
                                    var unknowndelim = reader.ReadByte(); //TODO: Determine what this is
                                    var charByte = reader.ReadByte(); //ASCII 
                                    var unknown = reader.ReadBytes(5); //TODO: Determine what this is
                                    Console.WriteLine("Insertion at Position " + charPos.ToString() + " - Character " + ReadCharacter(charByte) + " | " + charByte.ToString("X2"));
                                    break;
                                case 1: //Deletion
                                    var unknownDel = reader.ReadBytes(5); //TODO: Determine what this is
                                    Console.WriteLine("Deletion at Position " + charPos.ToString());
                                    break;
                                default:
                                    Console.WriteLine("Unknown Action - " + charAction.ToString());
                                    break;
                            }

                            

                            
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
