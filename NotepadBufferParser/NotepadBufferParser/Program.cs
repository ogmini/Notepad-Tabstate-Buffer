using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.IO.Hashing;


namespace NotepadBufferParser
{
    internal static class Program
    {
        static void Main(string[] args)
        {
            //TODO: Grab copies and parse them.

            string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState";
            Console.WriteLine("********** Starting *********");
            string pwd = Directory.GetCurrentDirectory();

            Console.WriteLine("Copying files from: {0}", folder);
            foreach (var path in Directory.EnumerateFiles(folder ,"*.bin"))
            {
                File.Copy(path, pwd + @"\" + Path.GetFileName(path), true); //TODO: Make flag for overwriting
            }

            foreach (var path in Directory.EnumerateFiles(pwd, "*.bin"))
            {
                ParseFile(path);
            }

            Console.WriteLine("********** Completed **********  ");
            Console.ReadLine();
        }
        private static void ParseFile(string filePath)
        {
            using (var stream = File.OpenRead(filePath))
            {
                using (var reader = new BinaryReader(stream))
                {
                    string hdrType = Encoding.ASCII.GetString(reader.ReadBytes(2));
                    
                    var uu = reader.ReadBytes(1); //TODO: Unknown
                    
                    bool isFile = BitConverter.ToBoolean(reader.ReadBytes(1), 0); //Is this a boolean or some other? Assuming bool for now...

                    if (hdrType == "NP")
                    {
                        Console.WriteLine("=========== Processing File ==========");
                        if (isFile) //Saved file
                        {
                            var fPathLength = ReadLEB128Unsigned(stream); //Filepath string length
                            var fPath = Encoding.Unicode.GetString(reader.ReadBytes((int)fPathLength * 2));
                            Console.WriteLine("Original File Location: {0}", fPath);

                            var fileContentLength = ReadLEB128Unsigned(stream); //Original Filecontent length

                            //TODO: YUCK
                            var delim = WriteLEB128Unsigned(fileContentLength); 
                            var numBytes = (delim.Length * 3) + 4;
                            //end delimiter appears to be fileContentLength 01 00 00 00 fileContentLength
                            reader.ReadBytes(45); //Unknown... This doesn't feel right
                            reader.ReadBytes(numBytes); //Unknown maybe delimiter??? Appears to be the Unsigned LEB128 fileContentLength twice, followed by 01 00 00 00 and the fileContentLength

                            string originalContent = Encoding.Unicode.GetString(reader.ReadBytes((int)fileContentLength * 2));
                            Console.WriteLine("Original Content: {0}", originalContent);

                            reader.ReadBytes(1); //TODO: Unknown 
                            reader.ReadBytes(4); //TODO: CRC 32 
                        }
                        else if (!isFile) //Unsaved Tab
                        { 
                            Console.WriteLine("Unsaved Tab: {0}", Path.GetFileName(filePath));
                            byte[] hdr = reader.ReadBytes(13); //TODO: Unknown
                        }
                        else
                        {
                            Console.WriteLine("Uhh");
                        }

                        if (reader.BaseStream.Length > reader.BaseStream.Position)
                            Console.WriteLine("Parsing Changes in File: {0} ", Path.GetFileName(filePath));

                        while (reader.BaseStream.Length > reader.BaseStream.Position)
                        {
                            List<byte> crc32Check = new List<byte>();

                            var charPos = ReadLEB128Unsigned(stream);
                            foreach (byte b in WriteLEB128Unsigned(charPos))
                            {
                                crc32Check.Add(b);
                            }
                            var charDeletion = ReadLEB128Unsigned(stream);
                            foreach (byte b in WriteLEB128Unsigned(charDeletion))
                            {
                                crc32Check.Add(b);
                            }
                            var charAddition = ReadLEB128Unsigned(stream);
                            foreach (byte b in WriteLEB128Unsigned(charAddition))
                            {
                                crc32Check.Add(b);
                            }

                            //TODO: This might be cleaner than below code
                            //if (charDeletion > 0)
                            //{
                            //    Console.WriteLine("Deletion at Position " + charPos.ToString() + " for " + charDeletion.ToString() + " position(s)");
                            //}

                            //if (charAddition > 0)
                            //{
                            //    for (int p = 0; p < (int)charAddition; p++)
                            //    {
                            //        var bytesChar = reader.ReadBytes(2);
                            //        var str = Encoding.Unicode.GetString(bytesChar);

                            //        Console.WriteLine("Addition at Position " + ((int)charPos + p).ToString() + " - Character " + str + " | " + bytesChar[0].ToString("X2"));
                            //    }
                            //}


                            if (charDeletion == 0 && charAddition > 0)
                            {
                                for (int p = 0; p < (int)charAddition; p++)
                                {
                                    var bytesChar = reader.ReadBytes(2);
                                    foreach (byte b in bytesChar)
                                    {
                                        crc32Check.Add(b);
                                    }
                                    var str = Encoding.Unicode.GetString(bytesChar);

                                    Console.WriteLine("Addition at Position {0}: Character {1} | {2}", ((int)charPos + p).ToString(), str, bytesChar[0].ToString("X2"));
                                }
                            }
                            else if (charDeletion > 0 && charAddition == 0)
                            {
                                Console.WriteLine("Deletion at Position {0} for {1} position(s)", charPos.ToString(), charDeletion.ToString());
                            }
                            else if (charDeletion > 0 && charAddition > 0)
                            {
                                Console.WriteLine("Deletion at Position {0} for {1} position(s)", charPos.ToString(), charDeletion.ToString());
                                for (int p = 0; p < (int)charAddition; p++)
                                {
                                    var bytesChar = reader.ReadBytes(2);
                                    foreach (byte b in bytesChar)
                                    {
                                        crc32Check.Add(b);
                                    }
                                    var str = Encoding.Unicode.GetString(bytesChar);

                                    Console.WriteLine("Insertion at Position {0}: Character {1} | {2}", ((int)charPos + p).ToString(), str, bytesChar[0].ToString("X2"));
                                }
                            }
                            else
                            {
                                Console.WriteLine("Uhh");
                            }

                            var crc32calculated = Crc32.Hash(crc32Check.ToArray());
                            Array.Reverse(crc32calculated);
                            
                            var crc32 = reader.ReadBytes(4); //CRC 32 

                            Console.WriteLine("CRC Match: {0}", crc32calculated.SequenceEqual(crc32) ? "PASS" : "FAIL");
                        }

                        Console.WriteLine("End of Stream");
                    }                   
                    else
                    {
                        Console.WriteLine("Invalid File");
                    }
                }
            }
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

        private static ulong ReadLEB128Unsigned(this Stream stream)
        {
            ulong value = 0;
            int shift = 0;
            bool more = true;

            while (more)
            {
                var next = stream.ReadByte();
                if (next < 0) { throw new InvalidOperationException("Unexpected end of stream"); }

                byte b = (byte)next;

                more = (b & 0x80) != 0;   // extract msb
                ulong chunk = b & 0x7fUL; // extract lower 7 bits
                value |= chunk << shift;
                shift += 7;
            }

            return value;
        }

        private static byte[] WriteLEB128Unsigned(ulong value)
        {
            byte[] bArray = new byte[0];

            bool more = true;

            while (more)
            {
                byte chunk = (byte)(value & 0x7fUL); // extract a 7-bit chunk
                value >>= 7;

                more = value != 0;
                if (more) { chunk |= 0x80; } // set msb marker that more bytes are coming

                bArray = AddByteToArray(bArray, chunk);
                
            };

            Array.Reverse(bArray);

            return bArray;
        }

        private static byte[] AddByteToArray(byte[] bArray, byte newByte)
        {
            byte[] newArray = new byte[bArray.Length + 1];
            bArray.CopyTo(newArray, 1);
            newArray[0] = newByte;
            return newArray;
        }

        
    }
}
