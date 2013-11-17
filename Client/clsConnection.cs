using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Net;
using System;

namespace Client
{
    public delegate void RequestHandler(HTTPConnection Sender, string Address, string Headers, byte[] Content);

    public class HTTPConnection
    {
        private const string CRLF = "\r\n";

        public event RequestHandler Request;

        private TcpClient c;
        private NetworkStream NS;
        private StreamReader SR;
        private StreamWriter SW;
        private Thread T;

        private string Temp;

        public IPEndPoint Remote
        {
            get
            {
                return (IPEndPoint)c.Client.RemoteEndPoint;
            }
        }

        public HTTPConnection(TcpClient C)
        {
            Request += new RequestHandler(HTTPConnection_Request);
            c = C;
            NS = new NetworkStream(c.Client, false);
            Temp = string.Empty;
        }

        public void Start()
        {
            T = new Thread(new ThreadStart(read));
            T.IsBackground = true;
            T.Start();
        }

        public void Send(string Headers, byte[] content)
        {
            SW = new StreamWriter(NS);
            SW.WriteLine(Headers.Trim() + CRLF);
            SW.Flush();
            if (content != null && content.Length > 0)
            {
                NS.Write(content, 0, content.Length);
                NS.Flush();
            }
            NS.Close();
            NS.Dispose();
            SW.Close();
            SW.Dispose();
            c.Client.Disconnect(false);
            c.Close();
            c=null;
        }

        private void HTTPConnection_Request(HTTPConnection Sender, string Address, string Headers,byte[] Content)
        {
            //TODO: Log
            Console.WriteLine("Request for {0}", Address);
        }

        private void read()
        {
            SR = new StreamReader(NS);
            bool cont = true;
            string Line=string.Empty;
            string Host=string.Empty;
            int len = 0;
            byte[] Content = null;

            while (cont)
            {
                Line = SR.ReadLine();
                if (string.IsNullOrEmpty(Line))
                {
                    cont = false;
                }
                else if(Line.Length>0)
                {
                    if (Line.ToLower().StartsWith("content-length:"))
                    {
                        try
                        {
                            len = int.Parse(Line.Split(':')[1].Trim());
                        }
                        catch
                        {
                            //NOOP
                        }
                    }
                    else if (Line.ToLower().StartsWith("host:"))
                    {
                        Host = Line.Split(':')[1].Trim();
                    }
                    Temp += Line + CRLF;
                }
            }

            //There is content to read (POST data for example)
            if (len > 0)
            {
                MemoryStream MS = new MemoryStream(len);
                byte[] temp = new byte[100];
                while (MS.Position < len)
                {
                    MS.Write(temp, 0, NS.Read(temp, 0, len));
                }
                Content = MS.ToArray();
                MS.Close();
            }
            Request(this, Host, Temp.Trim(), Content);
        }
    }
}
