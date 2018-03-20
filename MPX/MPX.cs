using System;
using System.Collections.Generic;
using System.Configuration;
using System.Drawing;
using System.IO;

namespace MPX
{
    class MPX
    {
        static void Main(string[] args)
        {
            bool simpleMode = Array.IndexOf(args, "/a") == -1;
            bool createLog = Array.IndexOf(args, "/l") != -1;
            bool extractVideo = Array.IndexOf(args, "/v") != -1;
            if (Array.IndexOf(args, "/?") != -1 || Array.IndexOf(args, "/help") != -1)
            {
                Console.WriteLine("Parameters:");
                Console.WriteLine();
                Console.WriteLine("/a: Advanced Mode. Extracts the main image from every jpg in the folder using jpg headers and saves them with the suffix '_still'. Might produce weird results, but should be stable by now.");
                Console.WriteLine("If /a is not present it will start in Simple Mode:");
                Console.WriteLine("Grabs the main image of every jpg in the folder and saves it with the suffix '_still'. Uses a safer method for image handling");
                Console.WriteLine();
                Console.WriteLine("/v also extracts the videos. Only works in advanced mode.");
                Console.WriteLine();
                Console.WriteLine("/l: Logs to log.txt. Not available in Simple Mode.");
            }
            string[] fileEntries = Directory.GetFiles(Directory.GetCurrentDirectory(), "*.jpg");
            Dictionary<int, string> events = new Dictionary<int, string>();
            foreach(string image in fileEntries)
            {
                events.Clear();
                if(simpleMode)
                {
                    Image img = Image.FromFile(image);
                    img.Save(Path.Combine(Path.GetDirectoryName(image), Path.GetFileNameWithoutExtension(image) + "_still" + Path.GetExtension(image)));
                    continue;
                }
                int start = 0;
                string filename = Path.GetFileName(image);
                events.Add(-2, "");
                events.Add(-1, filename);
                StreamReader sr = new StreamReader(filename);
                var bytes = default(byte[]);
                int len = 0;
                using (var memstream = new MemoryStream())
                {
                    sr.BaseStream.CopyTo(memstream);
                    bytes = memstream.ToArray();
                }
                len = bytes.Length;
                bool hasDRI = false;
                for (int i = 0; i < len; i++)
                {
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xD8)
                    {
                        start = i;
                        events.Add(i, "Start of image");
                        
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xE0)
                    {
                        int app0size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        int t_size = bytes[i + 16] * bytes[i + 17] * 3;
                        if (!(bytes[i + 4] == 0x4A && bytes[i + 5] == 0x46 && bytes[i + 6] == 0x49 && bytes[i + 7] == 0x46 && bytes[i + 8] == 0x00))
                        {
                            continue;
                        }
                        events.Add(i, "JFIF-APP0. Size " + ReadableSize(app0size));
                        events.Add(i + 4, "JFIF-APP0-Identifier");
                        events.Add(i + 9, "JFIF Version: " + bytes[i + 9] + "." + bytes[i + 10]);
                        events.Add(i + 11, "Density Units: " + bytes[i + 11]);
                        events.Add(i + 12, "X-Density: " + (bytes[i + 12] + bytes[i + 13]));
                        events.Add(i + 14, "Y-Density: " + (bytes[i + 14] + bytes[i + 15]));
                        events.Add(i + 16, "Thumbnail Width: " + bytes[i + 16]);
                        events.Add(i + 17, "Thumbnail Height: " + bytes[i + 17] + ". Resulting size: " + ReadableSize(t_size));
                        i += app0size + t_size + 1;
                        if (events.ContainsKey(i))
                        {
                            events[i] += " - JFIF-APP0 End";
                        } else
                        {
                            events.Add(i, "JFIF-APP0 End");
                        }
                        continue;
                    }

                    if (i >= len)
                    {
                        break;
                    }

                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xDB)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i, "Quantization table. Size: " + ReadableSize(t_size));
                        i += t_size + 1;
                        events.Add(i, "Quantization table end");
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xC0)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i, "Start of Frame (baseline DCT). Size: " + ReadableSize(t_size));
                        i += t_size + 1;
                        events.Add(i, "End of baseline DCT");
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xC0)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i, "Start of Frame (progressive DCT). Size: " + ReadableSize(t_size));
                        i += t_size + 1;
                        events.Add(i, "End of progressive DCT");
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xC4)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i,"Huffman Table at. Size: " + ReadableSize(t_size));
                        i += t_size + 1;
                        events.Add(i, "Huffman Table End");
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xDD)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i, "Restart Interval at. Size: " + ReadableSize(t_size));
                        i += t_size + 1;
                        events.Add(i, "Restart Interval End");
                        hasDRI = true;
                        continue;
                    }
                    if (bytes[i] == 0xFF && hasDRI && (bytes[i + 1] == 0xD0 || bytes[i + 1] == 0xD1 || bytes[i + 1] == 0xD2 || bytes[i + 1] == 0xD3 || bytes[i + 1] == 0xD4 || bytes[i + 1] == 0xD5 || bytes[i + 1] == 0xD6 || bytes[i + 1] == 0xD7))
                    {
                        events.Add(i, "Restart " + (bytes[i + 1] & 0x0f));
                        continue;
                    }
                    if (bytes[i] == 0xFF && (bytes[i + 1] == 0xE1 || bytes[i + 1] == 0xE2 || bytes[i + 1] == 0xE3 || bytes[i + 1] == 0xE4 || bytes[i + 1] == 0xE5 || bytes[i + 1] == 0xE6 || bytes[i + 1] == 0xE7))
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        int tmp = 4;
                        List<byte> identifier = new List<byte>();
                        while (bytes[i + tmp] != 0x00)
                        {
                            identifier.Add(bytes[i + tmp]);
                            tmp++;
                        }
                        string identifierString = System.Text.Encoding.Default.GetString(identifier.ToArray());
                        if (identifierString == "Exif")
                        {
                            events.Add(i, "Exif marker. Size: " + ReadableSize(t_size));
                        }
                        else if(identifierString.Contains("adobe"))
                        {
                            events.Add(i, "Adobe XMP marker. Size: " + ReadableSize(t_size));
                        }
                        else if (bytes[i + 1] == 0xE2)
                        {
                            events.Add(i, "App2 marker. Size: " + ReadableSize(t_size));
                        }
                        else
                        {
                            events.Add(i, "App" + (bytes[i + 1] & 0x0f) + " marker. Size: " + ReadableSize(t_size));
                        }
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xDA)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i, "Start of scan. Size: " + ReadableSize(t_size));
                        events.Add(i+t_size+1, "End of scan");
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xFE)
                    {
                        int t_size = NumDataBytes(bytes[i + 2], bytes[i + 3]);
                        events.Add(i, "Comment. Size: " + ReadableSize(t_size));
                        i += t_size + 1;
                        events.Add(i, "Comment end");
                        continue;
                    }
                    if (bytes[i] == 0xFF && bytes[i + 1] == 0xD9 && bytes[i + 2] == 0x00 && bytes[i + 3] == 0x00 && bytes[i + 4] == 0x00)
                    {
                        int size = (i + 2) - start;
                        events.Add(i, "End of image. Total size: " + ReadableSize(size));
                        byte[] b = new byte[size];
                        Array.Copy(bytes, 0, b, 0, size);
                        string newFileName = Path.Combine(Path.GetDirectoryName(image), Path.GetFileNameWithoutExtension(image) + "_still" + Path.GetExtension(image));
                        StreamWriter w = new StreamWriter(newFileName);
                        w.BaseStream.Write(b, 0, b.Length);
                        w.Close();
                        if(extractVideo)
                        {
                            //18 66 74 79 70 6D 70 34
                            if(bytes[i+5] == 0x18 && bytes[i+6] == 0x66 && bytes[i + 7] == 0x74 && bytes[i + 8] == 0x79 && bytes[i+9] == 0x70 && bytes[i + 10] == 0x6D && bytes[i + 11] == 0x70 && bytes[i + 12] == 0x34)
                            {
                                size = len - (i + 2);
                                events.Add(i+2, "Start of video. Size: " + ReadableSize(size));
                                b = new byte[size];
                                Array.Copy(bytes, i+2, b, 0, size);
                                newFileName = Path.Combine(Path.GetDirectoryName(image), Path.GetFileNameWithoutExtension(image) + "_vid.mp4");
                                w = new StreamWriter(newFileName);
                                w.BaseStream.Write(b, 0, b.Length);
                                w.Close();
                            } else
                            {
                                events.Add(i + 2, "No valid video file found at the endo f the image");
                            }
                            
                        }
                        break;
                    }
                }
                if(createLog)
                {
                    StreamWriter log = new StreamWriter("log.txt", true);
                    foreach (var eventstring in events)
                    {
                        log.WriteLine((eventstring.Key >= 0 ? (eventstring.Key.ToString("D10") + ": ") : "") + eventstring.Value);
                    }
                    log.Close();
                }
                
            }
        }

        protected static int NumDataBytes(byte s1, byte s2)
        {
            return 256 * s1 + s2;
        }

        protected static string ReadableSize(int numBytes)
        {
            string[] prefixes = new string[] {"", "K", "M", "G"};
            int idx = 0;
            while (numBytes > 1024 && idx < prefixes.Length)
            {
                numBytes /= 1024;
                idx++;
            }
            return numBytes + prefixes[idx] + "B";
        }
    }
}
