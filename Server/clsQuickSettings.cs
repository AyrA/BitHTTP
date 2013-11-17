using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.IO;

namespace Server
{
    public static class QuickSettings
    {
        /// <summary>
        /// predefined Password
        /// </summary>
        private const string PWD = "A.PΦú╘PìEαPΦ";
        /// <summary>
        /// File name
        /// </summary>
        public const string FILE = "settings.bin";

        /// <summary>
        /// Gets a value from the settings
        /// </summary>
        /// <param name="name">value name</param>
        /// <returns>value, or null fi not present</returns>
        public static string Get(string name)
        {
            if (Has(name))
            {
                return All()[name];
            }
            return null;
        }

        /// <summary>
        /// Creates or sets a value
        /// </summary>
        /// <param name="name">value name</param>
        /// <param name="value">value itself</param>
        public static void Set(string name, string value)
        {
            if (Has(name))
            {
                Del(name);
            }
            NameValueCollection nvc = All();
            nvc.Add(name, value);
            save(nvc);
            nvc.Clear();
        }

        /// <summary>
        /// Deletes a value (if it exists)
        /// </summary>
        /// <param name="name">value name</param>
        public static void Del(string name)
        {
            NameValueCollection nvc = All();
            if (Has(name))
            {
                nvc.Remove(name);
                save(nvc);
            }
            nvc.Clear();
        }

        /// <summary>
        /// checks, if a value exists
        /// </summary>
        /// <param name="name">value name</param>
        /// <returns>true if existant</returns>
        public static bool Has(string name)
        {
            string[] Keys=All().AllKeys;
            foreach (string Key in Keys)
            {
                if (Key == name)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// List of all names and values
        /// </summary>
        /// <returns>List of all names and values</returns>
        public static NameValueCollection All()
        {
            NameValueCollection nvc = new NameValueCollection();
            if (File.Exists(FILE))
            {
                string[] Lines = Encoding.UTF8.GetString(Crypt.Decrypt(File.ReadAllBytes(FILE), PWD)).Split('#');
                foreach (string Line in Lines)
                {
                    if (Line.Length > 0)
                    {
                        nvc.Add(Line.Split('|')[0], JsonConverter.B64dec(Line.Split('|')[1]));
                    }
                }
            }
            return nvc;
        }

        private static void save(NameValueCollection nvc)
        {
            string content = string.Empty;

            foreach (string k in nvc.AllKeys)
            {
                content += string.Format("{0}|{1}#", k, JsonConverter.B64enc(nvc[k]));
            }
            if (File.Exists(FILE))
            {
                File.Delete(FILE);
            }
            if (nvc.HasKeys())
            {
                File.WriteAllBytes(FILE, Crypt.Encrypt(Encoding.UTF8.GetBytes(content.Trim(new char[] { '#' })), PWD));
            }
        }
    }
}
