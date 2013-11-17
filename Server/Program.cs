using System;
using System.Collections.Generic;
using System.Text;
using CookComputing.XmlRpc;
using System.Threading;

namespace Server
{
    class Program
    {
        private static BitAPI BA;
        private static Thread MsgListener;
        private static bool cont;

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

            MsgListener = new Thread(new ThreadStart(msgListen));
            MsgListener.IsBackground = true;
            MsgListener.Start();

            Console.WriteLine("Ready");
            while (!Console.KeyAvailable)
            {
                Thread.Sleep(500);
            }
            Console.WriteLine("Shutting down...");
            cont = false;
            MsgListener.Join();
        }

        private static void msgListen()
        {
            cont = true;
            while (cont)
            {
                BitMsg[] Messages = JsonConverter.getMessages(BA.getAllInboxMessages());
                foreach (BitMsg m in Messages)
                {
                    if (m.message.StartsWith("GET ") || m.message.StartsWith("POST "))
                    {
                        string[] Parts = m.message.Split(new string[] { "\r\n\r\n" }, 2, StringSplitOptions.None);
                        //Decode content, if present
                        byte[] content = null;
                        if (Parts.Length==2 && Parts[1].Length > 0)
                        {
                            Ascii85 A5 = new Ascii85();
                            content = A5.Decode(Parts[1]);
                        }
                        Thread t = new Thread(new ParameterizedThreadStart(sr));
                        t.IsBackground = true;
                        t.Start(new object[] { m.fromAddress, m.toAddress, m.subject, Parts[0], content });
                        BA.trashMessage(m.msgid);
                    }
                }
                Thread.Sleep(2000);
            }
        }

        private static void sr(object o)
        {
            object[] oo = (object[])o;
            string from, to, id,header;
            byte[] content;

            from = oo[0].ToString();
            to = oo[1].ToString();
            id = oo[2].ToString();
            header = oo[3].ToString();
            content = (byte[])oo[4];

            sendReply(from, to, id, header, content);
        }

        private static void sendReply(string Dest, string From, string ID, string Headers, byte[] Content)
        {
            string[] Lines = Headers.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);

            Lines[0] = filterRequest(Lines[0]);

            string Reply = HTTP.getAnswer(Headers, Content);
            BA.sendMessage(Dest, From, JsonConverter.B64enc(ID), JsonConverter.B64enc(Reply));
        }

        private static string filterRequest(string p)
        {
            string[] Parts = p.Split(' ');
            if (Parts[1].ToLower().StartsWith("http://"))
            {
                Parts[1] = Parts[1].Substring(7);
                Parts[1] = Parts[1].Substring(Parts[1].IndexOf('/'));
            }
            return string.Join(" ", Parts);
        }
    }
}
