using System.Net;
using System.Net.Sockets;
using System;

namespace Client
{
    public delegate void NewConnectionHandler(HTTPConnection c);

    public class HTTPServer
    {
        public event NewConnectionHandler NewConnection;

        private TcpListener S;

        public HTTPServer()
        {
            NewConnection += new NewConnectionHandler(HTTPServer_NewConnection);
            S = new TcpListener(new IPEndPoint(IPAddress.Loopback, 8080));
        }

        ~HTTPServer()
        {
            if (S != null)
            {
                S.Stop();
                S = null;
            }
        }

        public void Start()
        {
            S.Start();
            S.BeginAcceptTcpClient(new AsyncCallback(con), null);
        }

        public void Stop()
        {
            S.Stop();
        }

        void HTTPServer_NewConnection(HTTPConnection c)
        {
            //TODO: Log
            Console.WriteLine("Connection from {0}", c.Remote);
        }

        private void con(IAsyncResult ar)
        {
            TcpClient C=S.EndAcceptTcpClient(ar);
            if (C != null)
            {
                NewConnection(new HTTPConnection(C));
                S.BeginAcceptTcpClient(new AsyncCallback(con), null);
            }
        }
    }
}
