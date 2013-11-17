using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Text;
using System.Threading;

namespace Server
{
    public static class HTTP
    {
        private const string CRLF = "\r\n";

        public static string getAnswer(string Headers, byte[] Content)
        {
            string Line = string.Empty;
            string ResHeader = string.Empty;
            int len = -1;

            TcpClient C = new TcpClient();
            C.Connect(new IPEndPoint(IPAddress.Parse("192.168.1.13"), 80));
            NetworkStream NS = new NetworkStream(C.Client, false);

            NS.Write(s2b(Headers.Trim() + CRLF + CRLF), 0, s2b(Headers.Trim() + CRLF + CRLF).Length);

            if (Content != null && Content.Length > 0)
            {
                NS.Write(Content, 0, Content.Length);
                NS.Flush();
            }

            //reads all headers
            while (!string.IsNullOrEmpty(Line = rl(NS)))
            {
                ResHeader += Line + CRLF;
                if (Line.ToLower().StartsWith("content-length:"))
                {
                    try
                    {
                        len = int.Parse(Line.Split(':')[1].Trim());
                    }
                    catch
                    {
                        len = 0;
                    }
                }
            }

            //check if content of defined length is there to read
            if (len > 0)
            {
                byte[] data = new byte[100];
                MemoryStream MS = new MemoryStream(len);
                while (MS.Position < len)
                {
                    MS.Write(data, 0, NS.Read(data, 0, data.Length));
                }
                if (MS.Length > 0)
                {
                    Ascii85 A5 = new Ascii85();
                    ResHeader += CRLF + A5.Encode(MS.ToArray());
                }
                MS.Close();
                MS.Dispose();
            }
            //content of undefined length probably.
            //In this case, read everything until disconnect.
            if (len == -1)
            {
                byte[] data = new byte[100];
                MemoryStream MS = new MemoryStream();
                while (C.Connected)
                {
                    try
                    {
                        MS.Write(data, 0, NS.Read(data, 0, data.Length));
                    }
                    catch
                    {
                        //Connection closed
                    }
                }
                if (MS.Length > 0)
                {
                    Ascii85 A5 = new Ascii85();
                    ResHeader += CRLF + A5.Encode(MS.ToArray());
                }
                MS.Close();
                MS.Dispose();
            }
            NS.Close();
            NS.Dispose();

            try
            {
                C.Client.Disconnect(true);
            }
            finally
            {
                C.Close();
                C = null;
            }

            return ResHeader;
        }

        private static string rl(Stream s)
        {
            string temp = string.Empty;
            while (!temp.EndsWith("\n"))
            {
                int i = s.ReadByte();
                if (i >= 0)
                {
                    temp += (char)i;
                }
                else
                {
                    Thread.Sleep(100);
                }
            }
            //we trim the end,
            //since headers do not end in spaces,
            //this is safe to do
            return temp.TrimEnd();
        }

        private static byte[] s2b(string s)
        {
            return Encoding.UTF8.GetBytes(s);
        }

        private static string b2s(byte[] b)
        {
            return Encoding.UTF8.GetString(b);
        }
    }
}
