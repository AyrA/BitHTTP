using System;
using System.Collections.Generic;
using System.Text;
using CookComputing.XmlRpc;
using Newtonsoft.Json;

namespace Client
{
    public interface BitAPI : IXmlRpcProxy
    {
        [XmlRpcMethod]
        string helloWorld(string a, string b);

        [XmlRpcMethod]
        string sendMessage(string ToAddr, string FromAddr, string Base64subject, string Base64message);

        [XmlRpcMethod]
        string getAllInboxMessages();

        [XmlRpcMethod]
        string getStatus(string ackData);

        [XmlRpcMethod]
        string getAllInboxMessageIds();

        [XmlRpcMethod]
        string getInboxMessageById(string ID);

        [XmlRpcMethod]
        void trashMessage(string ID);

        [XmlRpcMethod]
        string listAddressBookEntries();

        [XmlRpcMethod]
        void addAddressBookEntry(string Address,string Label);

        [XmlRpcMethod]
        void deleteAddressBookEntry(string Address);

        [XmlRpcMethod]
        string trashSentMessageByAckData(string AckData);

        [XmlRpcMethod]
        string listAddresses2();
    }

    public static class JsonConverter
    {
        /// <summary>
        /// Converts from/to Base64
        /// </summary>
        /// <param name="s">Autodetected Input string</param>
        /// <returns>Base64 Representation</returns>
        public static string B64enc(string s)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(s), Base64FormattingOptions.None);
        }

        /// <summary>
        /// Converts from/to Base64
        /// </summary>
        /// <param name="s">Autodetected Input string</param>
        /// <returns>Base64 Representation</returns>
        public static string B64dec(string s)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(s.Trim(new char[]{'\r','\n','\t','\0',' '})));
        }

        public static BitMsg[] getMessages(string JSON)
        {
            BitMsgs MSG;
            try
            {
                MSG = JsonConvert.DeserializeObject<BitMsgs>(JSON);
            }
            catch
            {
                MSG = new BitMsgs();
                MSG.inboxMessages = new BitMsg[0];
            }

            if (MSG.inboxMessages != null)
            {
                for (int i = 0; i < MSG.inboxMessages.Length; i++)
                {
                    MSG.inboxMessages[i].Decode();
                }
            }
            else
            {
                //Empty array
                MSG.inboxMessages = new BitMsg[0];
            }
            return MSG.inboxMessages;
        }

        public static msgID[] getIDs(string JSON)
        {
            inboxMessageIIDs MSG;
            try
            {
                MSG = JsonConvert.DeserializeObject<inboxMessageIIDs>(JSON);
            }
            catch
            {
                MSG = new inboxMessageIIDs();
                MSG.inboxMessageIds = new msgID[0];
            }
            return MSG.inboxMessageIds;
        }

        public static BitMsg getByID(string JSON)
        {
            InboxMessage MSG;
            try
            {
                MSG = JsonConvert.DeserializeObject<InboxMessage>(JSON);
            }
            catch
            {
                MSG = new InboxMessage();
                MSG.inboxMessage = new BitMsg[1];
            }
            if (!string.IsNullOrEmpty(MSG.inboxMessage[0].msgid))
            {
                MSG.inboxMessage[0].Decode();
            }
            return MSG.inboxMessage[0];
        }

        public static addrbookEntry[] getAddrBook(string JSON)
        {
            Addresses ADDR;
            try
            {
                ADDR = JsonConvert.DeserializeObject<Addresses>(JSON);
            }
            catch
            {
                ADDR = new Addresses();
                ADDR.addresses = new addrbookEntry[0];
            }
            for (int i = 0; i < ADDR.addresses.Length;i++ )
            {
                ADDR.addresses[i].Decode();
            }
            return ADDR.addresses;
        }

        public static Identity[] getAddresses(string JSON)
        {
            IdentityContainer ADDR;
            try
            {
                ADDR = JsonConvert.DeserializeObject<IdentityContainer>(JSON);
            }
            catch
            {
                ADDR = new IdentityContainer();
                ADDR.addresses = new Identity[0];
            }
            for (int i = 0; i < ADDR.addresses.Length; i++)
            {
                ADDR.addresses[i].Decode();
            }
            return ADDR.addresses;
        }
    }

    public struct IdentityContainer
    {
        public Identity[] addresses;
    }

    public struct Identity
    {
        public string label;
        public string address;
        public int stream;
        public bool enabled;
        public bool chan;

        /// <summary>
        /// Decodes the label after it has been set
        /// </summary>
        public void Decode()
        {
            label=JsonConverter.B64dec(label);
        }
    }

    public struct Addresses
    {
        public addrbookEntry[] addresses;
    }

    public struct addrbookEntry
    {
        public string label;
        public string address;

        /// <summary>
        /// Decodes Base64 data
        /// </summary>
        public void Decode()
        {
            label = JsonConverter.B64dec(label.Trim()).Trim();
        }
    }

    public struct InboxMessage
    {
        public BitMsg[] inboxMessage;
    }

    public struct inboxMessageIIDs
    {
        public msgID[] inboxMessageIds;
    }

    public struct msgID
    {
        public string msgid;
    }

    public struct BitMsgs
    {
        public BitMsg[] inboxMessages;
    }

    public struct BitMsg
    {
        public int encodingType;
        public string toAddress;
        public string msgid;
        public int receivedTime;
        public string message;
        public string fromAddress;
        public string subject;

        /// <summary>
        /// Decodes the base64 crap and transforms linebreaks
        /// </summary>
        public void Decode()
        {
            //Decode Base64
            message = JsonConverter.B64dec(message.Trim()).Trim();
            subject = JsonConverter.B64dec(subject.Trim()).Trim();
            //Convert line endings
            message = message
                .Replace("\r\n", "\n")
                .Replace("\r", "")
                .Replace("\n", "\r\n");
        }
    }
}
