using System;
using System.Collections.Generic;
using System.Text;
using CookComputing.XmlRpc;
using System.Threading;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace Client
{
    class Program
    {
        /// <summary>
        /// Sender address
        /// </summary>
        private const string LOCAL = "BM-NBLbwaue2inXTAoenHu6ULUG6Z58FKHB";

        private static BitAPI BA;
        private static HTTPServer s;
        private static List<HTTPConnection> cc;
        private static Dictionary<string, HTTPConnection> Req;
        private static addrbookEntry[] DNS;
        private static Thread MsgListener;
        private static int i = 0;

        static void Main(string[] args)
        {
            string API_ADDR, API_NAME, API_PASS;

            if (!QuickSettings.Has("API-ADDR") || !QuickSettings.Has("API-NAME") || !QuickSettings.Has("API-PASS"))
            {
                Console.WriteLine("API settings missing");
                Console.Write("Host: (IP:Port): ");
                API_ADDR = Console.ReadLine();
                Console.Write("Username       : ");
                API_NAME = Console.ReadLine();
                Console.Write("Password       : ");
                API_PASS = Console.ReadLine();
                QuickSettings.Set("API-ADDR", API_ADDR);
                QuickSettings.Set("API-NAME", API_NAME);
                QuickSettings.Set("API-PASS", API_PASS);
            }
            BA = (BitAPI)XmlRpcProxyGen.Create(typeof(BitAPI));
            BA.Url = string.Format("http://{0}/", QuickSettings.Get("API-ADDR"));
            BA.Headers.Add("Authorization", "Basic " + JsonConverter.B64enc(string.Format("{0}:{1}", QuickSettings.Get("API-NAME"), QuickSettings.Get("API-PASS"))));

            DNS = JsonConverter.getAddrBook(BA.listAddressBookEntries());
            cc = new List<HTTPConnection>();
            Req = new Dictionary<string, HTTPConnection>();

            MsgListener = new Thread(new ThreadStart(msgListen));
            MsgListener.IsBackground = true;
            MsgListener.Start();

            s = new HTTPServer();
            s.NewConnection += new NewConnectionHandler(s_NewConnection);
            s.Start();
            
            Console.WriteLine("Ready");
            while(!Console.KeyAvailable)
            {
                Thread.Sleep(500);
            }
            
            Console.WriteLine("Shutting down...");
            s.Stop();
            s = null;
            MsgListener.Join();
        }

        private static void msgListen()
        {
            while (s != null)
            {
                BitMsg[] Messages=JsonConverter.getMessages(BA.getAllInboxMessages());
                foreach (BitMsg m in Messages)
                {
                    if(Req.ContainsKey(m.subject) && m.message.StartsWith("HTTP"))
                    {
                        string[] Parts = m.message.Split(new string[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
                        if (Parts.Length == 2)
                        {
                            //Decode content, if present
                            byte[] content = null;
                            if (Parts[1].Length > 0)
                            {
                                Ascii85 A5 = new Ascii85();
                                content = A5.Decode(Parts[1]);
                            }
                            BA.trashMessage(m.msgid);
                            Req[m.subject].Send(Parts[0], content);
                            //Remove connection and request
                            cc.Remove(Req[m.subject]);
                            Req.Remove(m.subject);
                        }
                    }
                }
                Thread.Sleep(2000);
            }
        }

        private static void s_NewConnection(HTTPConnection c)
        {
            c.Request += new RequestHandler(c_Request);
            cc.Add(c);
            c.Start();
        }

        private static void c_Request(HTTPConnection Sender, string Address, string Headers, byte[] Content)
        {
            string ID = getID();
            string Enc = string.Empty;
            Address = parseAddress(Address);
            if (string.IsNullOrEmpty(Address))
            {
                Sender.Send(Base.HTTP_NODNS, null);
            }
            else
            {
                if (Req.ContainsKey(ID))
                {
                    Req[ID] = Sender;
                }
                else
                {
                    Req.Add(ID, Sender);
                }

                if (Content != null && Content.Length > 0)
                {
                    Ascii85 A5 = new Ascii85();
                    Enc = A5.Encode(Content);
                }
                BA.sendMessage(Address, LOCAL, JsonConverter.B64enc(ID), JsonConverter.B64enc(Headers + "\r\n\r\n" + Enc));
            }
        }

        private static string parseAddress(string Address)
        {
            if (!Address.ToUpper().EndsWith(".BM"))
            {
                return null;
            }
            if (!Address.StartsWith("BM-"))
            {
                foreach (addrbookEntry a in DNS)
                {
                    if (a.label.ToLower() == Address.ToLower())
                    {
                        Address = a.address;
                        break;
                    }
                }
            }
            return Address.StartsWith("BM-") ? Address : null;
        }

        private static string getID()
        {
            //SHA1 S = SHA1.Create();
            //return Hex(S.ComputeHash(BitConverter.GetBytes(DateTime.Now.Ticks)));
            i++;
            return i.ToString();
        }

        private static string Hex(byte[] data)
        {
            StringBuilder SB = new StringBuilder(data.Length * 2 + data.Length / 78);

            for (int i = 0; i < data.Length; i++)
            {
                SB.Append(data[i].ToString("X2"));
                if (i % 78 == 0 && i > 0)
                {
                    SB.Append("\n");
                }
            }

            return SB.ToString();
        }
    }
}
