﻿using System;
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
            [Option('o', "output", Required = false, Default = "results", HelpText = "Output folder")]
            public string OutputFolder { get; set; }

            [Option('i', "input", Required = false, HelpText = "Input folder")]
            public string InputFolder { get; set; }
        }

        static void Main(string[] args)
        {
            Parser.Default.ParseArguments<Options>(args).WithParsed<Options>(o =>
            {
                //TODO: Grab copies and parse them.
                Console.WriteLine("********** Starting *********");
                string folder = (string.IsNullOrWhiteSpace(o.InputFolder) ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Packages\Microsoft.WindowsNotepad_8wekyb3d8bbwe\LocalState\TabState") : o.InputFolder);
                string pwd = Path.Combine(Directory.GetCurrentDirectory(), o.OutputFolder);

                Directory.CreateDirectory(pwd);

                Console.WriteLine("Copying files from: {0} to {1}", folder, pwd);
                foreach (var path in Directory.EnumerateFiles(folder, "*.bin"))
                {
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
                if (stream.Length > 0) //Check file actually has some data
                {
                    using (var reader = new BinaryReader(stream))
                    {
                        string hdrType = Encoding.ASCII.GetString(reader.ReadBytes(2));

                        if (hdrType == "NP")
                        {
                            Console.WriteLine("=========== Processing File ==========");
                            Console.WriteLine("{0}", Path.GetFileName(filePath));

                            var sequenceNumber = stream.ReadLEB128Unsigned();
                            Console.WriteLine("Sequence Number: {0}", sequenceNumber.ToString());

                            if (Path.GetFileNameWithoutExtension(filePath).EndsWith(".0") || Path.GetFileNameWithoutExtension(filePath).EndsWith(".1"))
                            {
                                CRC32Check c = new CRC32Check();
                                c.AddBytes(sequenceNumber);

                                var un = reader.ReadBytes(1);
                                c.AddBytes(un);
                                Console.WriteLine("Unknown bytes - un: {0}", BytestoString(un));

                                if (un[0] == 8) //Not saved
                                {
                                    var un1 = reader.ReadBytes(2); //TODO: This is variable.  Might be tied to file saved or not. Why/What?
                                    c.AddBytes(un1);
                                    Console.WriteLine("Unknown bytes - un1: {0}", BytestoString(un1));
                                }
                                else if (un[0] == 9) //Tab saved
                                {
                                    var un1 = reader.ReadBytes(3); //TODO: This is variable.  Might be tied to file saved or not. Why/What?
                                    c.AddBytes(un1);
                                    Console.WriteLine("Unknown bytes - un1: {0}", BytestoString(un1));
                                }

                                var selectionStartIndex = reader.BaseStream.ReadLEB128Unsigned();
                                c.AddBytes(selectionStartIndex);
                                var selectionEndIndex = reader.BaseStream.ReadLEB128Unsigned();
                                c.AddBytes(selectionEndIndex);
                                Console.WriteLine("Selection Index {0} to {1} on close", selectionStartIndex, selectionEndIndex);

                                var un2 = reader.ReadBytes(4);
                                c.AddBytes(un2);
                                Console.WriteLine("Unknown bytes - un2: {0}", BytestoString(un2));

                                Console.WriteLine("#.bin CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "!!!FAIL!!!");
                            }
                            else
                            {
                                var isFile = reader.ReadBytes(1); //Is this a boolean or some other? Assuming bool for now...

                                List<char> buffer = new List<char>(); //TODO: Use this for playback 

                                if (BitConverter.ToBoolean(isFile, 0)) //Saved file
                                {
                                    CRC32Check c = new CRC32Check();
                                    c.AddBytes(isFile);

                                    var fPathLength = stream.ReadLEB128Unsigned(); //Filepath string length
                                    c.AddBytes(fPathLength);

                                    var fPathBytes = reader.ReadBytes((int)fPathLength * 2);
                                    c.AddBytes(fPathBytes);

                                    var fPath = Encoding.Unicode.GetString(fPathBytes);
                                    Console.WriteLine("Original File Location: {0}", fPath);

                                    var fileContentLength = stream.ReadLEB128Unsigned(); //Original Filecontent length
                                    c.AddBytes(fileContentLength);

                                    //TODO: YUCK. There is something more going on here...
                                    //end delimiter appears to be 00 01 00 00 01 00 00 00 fileContentLength
                                    var un1 = reader.ReadBytes(43); //Unknown... This doesn't feel right???
                                    c.AddBytes(un1);
                                    Console.WriteLine("Unknown bytes - un1: {0}", BytestoString(un1));

                                    var delim1 = reader.ReadBytes(2); //Unknown maybe delimiter??? Appears to be 00 01 
                                    c.AddBytes(delim1);
                                    Console.WriteLine("Unknown bytes - delim1: {0}", BytestoString(delim1));

                                    var selectionStartIndex = reader.BaseStream.ReadLEB128Unsigned();
                                    c.AddBytes(selectionStartIndex);
                                    var selectionEndIndex = reader.BaseStream.ReadLEB128Unsigned();
                                    c.AddBytes(selectionEndIndex);
                                    Console.WriteLine("Selection Index {0} to {1} on save", selectionStartIndex, selectionEndIndex);

                                    var delim2 = reader.ReadBytes(4); //Unknown maybe delimiter??? Appears to be 01 00 00 00
                                    c.AddBytes(delim2);
                                    Console.WriteLine("Unknown bytes - delim2: {0}", BytestoString(delim2));
                                    //95 03 05 01 F8 E3 AC C5 87 E6 9B ED 01 ED E9 78 0A 41 0D 40 B2 F2 68 3B BF E8 BC B0 F8 27 84 08 38 C1 84 5C D4 1A BC AA 0E 87 F6 AB B1 00 01 00 00 01 00 00 00 95 03

                                    fileContentLength = reader.BaseStream.ReadLEB128Unsigned();
                                    c.AddBytes(fileContentLength);
                                    var originalContentBytes = reader.ReadBytes((int)fileContentLength * 2);
                                    c.AddBytes(originalContentBytes);

                                    var originalContent = Encoding.Unicode.GetChars(originalContentBytes);
                                    buffer.InsertRange(0, originalContent);

                                    Console.WriteLine("Original Content: {0}", new string(originalContent));

                                    var un2 = reader.ReadBytes(1); //TODO: Unknown 
                                    c.AddBytes(un2);
                                    Console.WriteLine("Unknown bytes - un2: {0}", BytestoString(un2));

                                    Console.WriteLine("Original Content CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "!!!FAIL!!!");

                                    if (selectionStartIndex != selectionEndIndex)
                                    {
                                        var segment = new ArraySegment<char>(originalContent, (int)selectionStartIndex, (int)selectionEndIndex - (int)selectionStartIndex);
                                        Console.WriteLine("Selected text: {0}", new string(segment.ToArray()));
                                    }

                                }
                                else if (!BitConverter.ToBoolean(isFile, 0)) //Unsaved Tab
                                {
                                    Console.WriteLine("Unsaved Tab: {0}", Path.GetFileName(filePath));

                                    CRC32Check c = new CRC32Check();
                                    c.AddBytes(isFile);

                                    //TODO: YUCK. There is something more going on here...
                                    var un1 = reader.ReadBytes(1); //TODO: Unknown 
                                    c.AddBytes(un1);
                                    Console.WriteLine("Unknown Bytes - un1: {0}", BytestoString(un1));

                                    var fileContentLength = stream.ReadLEB128Unsigned();
                                    c.AddBytes(fileContentLength);

                                    var delim = LEB128Converter.WriteLEB128Unsigned(fileContentLength);
                                    var numBytes = (delim.Length * 2) + 4; //Why is this different from above 2 vs 1?? Something isn't right... I'd expect the same for both
                                                                           //C2 02 C2 02 01 00 00 00 C2 02
                                    var un2 = reader.ReadBytes(numBytes);
                                    c.AddBytes(un2);
                                    Console.WriteLine("Unknown Bytes - un2: {0}", BytestoString(un2));

                                    var originalContentBytes = reader.ReadBytes((int)fileContentLength * 2);
                                    c.AddBytes(originalContentBytes);

                                    var originalContent = Encoding.Unicode.GetChars(originalContentBytes);
                                    buffer.InsertRange(0, originalContent);

                                    Console.WriteLine("Original Content: {0}", new string(originalContent));
                                    var un3 = reader.ReadBytes(1); //TODO: Unknown 
                                    c.AddBytes(un3);
                                    Console.WriteLine("Unknown Bytes - un3: {0}", BytestoString(un3));

                                    Console.WriteLine("Original Content CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "!!!FAIL!!!");
                                }
                                else
                                    Console.WriteLine("Uhh - Detected an unknown flag at 4th byte for isFile");


                                if (reader.BaseStream.Length > reader.BaseStream.Position)
                                    Console.WriteLine("Parsing Changes in File: {0} ", Path.GetFileName(filePath));

                                //Unsaved Buffer
                                while (reader.BaseStream.Length > reader.BaseStream.Position)
                                {
                                    CRC32Check c = new CRC32Check();

                                    var charPos = stream.ReadLEB128Unsigned();
                                    c.AddBytes(charPos);

                                    var charDeletion = stream.ReadLEB128Unsigned();
                                    c.AddBytes(charDeletion);

                                    var charAddition = stream.ReadLEB128Unsigned();
                                    c.AddBytes(charAddition);


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

                                            //Should we really make a differentiation between Insertion and Addition. It makes no difference to the buffer view
                                            Console.WriteLine("{0} at Position {1}: Character {2} | {3}", charDeletion > 0 ? "Insertion" : "Addition", ((int)charPos + p).ToString(), new string(str), BytestoString(bytesChar));
                                        }
                                    }

                                    Console.WriteLine("Chunk CRC Match: {0}", c.Check(reader.ReadBytes(4)) ? "PASS" : "!!!FAIL!!!");
                                    Console.WriteLine(String.Join("", buffer));
                                }

                                Console.WriteLine("End of Stream");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Invalid File");
                        }
                    }
                }
            }
        }

        private static string BytestoString(byte[] bytes)
        {
            string retVal = string.Empty;

            foreach (byte b in bytes)
            {
                retVal += String.Format("0x{0} ", b.ToString("X2"));
            }

            return retVal;
        }
    }
}
