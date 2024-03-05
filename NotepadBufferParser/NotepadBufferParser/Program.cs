using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading.Tasks;
using System.IO.Hashing;
using System.Text.RegularExpressions;
using CommandLine;


namespace NotepadBufferParser
{
    internal static class Program
    {
        public class Options
        {

        }
        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                //TODO: Grab copies and parse them.
                Console.WriteLine("********** Starting *********");
                string folder = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState";
                string pwd = Directory.GetCurrentDirectory();

                Console.WriteLine("Copying files from: {0} to {1}", folder, pwd);
                foreach (var path in Directory.EnumerateFiles(folder, "*.bin"))
                {
                    if (!Path.GetFileNameWithoutExtension(path).EndsWith(".0") && !Path.GetFileNameWithoutExtension(path).EndsWith(".1")) //Shitty, use REGEX or something. OR just learn to parse these
                        File.Copy(path, pwd + @"\" + Path.GetFileName(path), true); //TODO: Make flag for overwriting
                }

                foreach (var path in Directory.EnumerateFiles(pwd, "*.bin"))
                {
                    ParseFile(path);
                }

                Console.WriteLine("********** Completed **********");
                Console.ReadLine();

            });
            
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
                        List<char> buffer = new List<char>(); //TODO: Use this for playback 

                        if (isFile) //Saved file
                        {
                            CRC32Check c = new CRC32Check();

                            var fPathLength = ReadLEB128Unsigned(stream); //Filepath string length
                            c.AddBytes(WriteLEB128Unsigned(fPathLength));

                            var fPathBytes = reader.ReadBytes((int)fPathLength * 2);
                            c.AddBytes(fPathBytes);

                            var fPath = Encoding.Unicode.GetString(fPathBytes);
                            Console.WriteLine("Original File Location: {0}", fPath);

                            var fileContentLength = ReadLEB128Unsigned(stream); //Original Filecontent length
                            c.AddBytes(WriteLEB128Unsigned(fileContentLength));

                            //TODO: YUCK. There is something more going on here...
                            var delim = WriteLEB128Unsigned(fileContentLength); 
                            var numBytes = (delim.Length * 1) + 8;
                            //end delimiter appears to be 00 01 00 00 01 00 00 00 fileContentLength
                            var un1 = reader.ReadBytes(43); //Unknown... This doesn't feel right
                            c.AddBytes(un1);


                            var un2 = reader.ReadBytes(numBytes); //Unknown maybe delimiter??? Appears to be the Unsigned LEB128 fileContentLength twice, followed by 01 00 00 00 and the fileContentLength
                            c.AddBytes(un2);

                            var originalContentBytes = reader.ReadBytes((int)fileContentLength * 2);
                            c.AddBytes(originalContentBytes);

                            var originalContent = Encoding.Unicode.GetChars(originalContentBytes);
                            buffer.InsertRange(0, originalContent);

                            Console.WriteLine("Original Content: {0}", new string(originalContent));

                            var un3 = reader.ReadBytes(1); //TODO: Unknown 
                            c.AddBytes(un3);

                            Console.WriteLine("Original Content CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "FAIL"); //CRC32

                        }
                        else if (!isFile) //Unsaved Tab
                        { 
                            Console.WriteLine("Unsaved Tab: {0}", Path.GetFileName(filePath));

                            CRC32Check c = new CRC32Check();

                            //TODO: YUCK. There is something more going on here...
                            var un1 = reader.ReadBytes(1); //TODO: Unknown 
                            c.AddBytes(un1);

                            var fileContentLength = ReadLEB128Unsigned(stream);
                            c.AddBytes(WriteLEB128Unsigned(fileContentLength));
                            

                            var delim = WriteLEB128Unsigned(fileContentLength);
                            var numBytes = (delim.Length * 2) + 4; //Why is this different from above 2 vs 3?? Something isn't right... I'd expect the same for both
                            var un2 = reader.ReadBytes(numBytes);
                            c.AddBytes(un2);

                            var originalContentBytes = reader.ReadBytes((int)fileContentLength * 2);
                            c.AddBytes(originalContentBytes);

                            var originalContent = Encoding.Unicode.GetChars(originalContentBytes);
                            buffer.InsertRange(0, originalContent);

                            Console.WriteLine("Original Content: {0}", new string(originalContent));
                            var un3 = reader.ReadBytes(1); //TODO: Unknown 
                            c.AddBytes(un3);

                            Console.WriteLine("Original Content CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "FAIL"); //CRC32
                        }
                        else
                        {
                            Console.WriteLine("Uhh");
                        }

                        if (reader.BaseStream.Length > reader.BaseStream.Position)
                            Console.WriteLine("Parsing Changes in File: {0} ", Path.GetFileName(filePath));

                        while (reader.BaseStream.Length > reader.BaseStream.Position)
                        {
                            CRC32Check c = new CRC32Check();

                            var charPos = ReadLEB128Unsigned(stream);
                            c.AddBytes(WriteLEB128Unsigned(charPos));
                            
                            var charDeletion = ReadLEB128Unsigned(stream);
                            c.AddBytes(WriteLEB128Unsigned(charDeletion));

                            var charAddition = ReadLEB128Unsigned(stream);
                            c.AddBytes(WriteLEB128Unsigned(charAddition));


                            if (charDeletion > 0)
                            {
                                buffer.RemoveRange((int)charPos, (int)charDeletion);

                                Console.WriteLine("Deletion at Position {0} for {1} position(s)", charPos.ToString(), charDeletion.ToString());
                            }

                            if (charAddition > 0)
                            {
                                for (int p = 0; p < (int)charAddition; p++)
                                {
                                    var bytesChar = reader.ReadBytes(2);
                                    c.AddBytes(bytesChar);
                                    var str = Encoding.Unicode.GetChars(bytesChar);

                                    buffer.InsertRange(((int)charPos + p), str);

                                    Console.WriteLine("{0} at Position {1}: Character {2} | {3}", charDeletion > 0 ? "Insertion" : "Addition", ((int)charPos + p).ToString(), new string(str), bytesChar[0].ToString("X2"));
                                }
                            }

                            Console.WriteLine("Chunk CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "FAIL"); //CRC32
                            Console.WriteLine(String.Join("", buffer));
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
