using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Net;
using System.IO;
using System.Windows.Forms;

namespace Hawt35.Tools
{
   public class AccountCheck
    {
       public static bool CheckAccount(string URL, string username, string password, out string message)
       {
           string Sha1Pass = CalculateSHA1(password, Encoding.ASCII).ToLower();
           message = "";
           string request = String.Format("{0}?username={1}&password={2}",URL, username, Sha1Pass);
           try
           {
               //MessageBox.Show("URL :"+URL+"\r\n Username : "+username+"\r\nPassword Sha1 : "+Sha1Pass+"\r\n Full Message reuturn : "+request);
               string lol = GetString(request);
               bool success = (lol.Substring(0, 1) == "1") ? true : false;
               if(!success) message = lol.Substring(2, lol.Length - 2);
               return success;
           }
           catch (WebException ex)
           {
               throw ex;
           }
       }

       /// <summary>
       /// Calculates SHA1 hash
       /// </summary>
       /// <param name="text">input string</param>
       /// <param name="enc">Character encoding</param>
       /// <returns>SHA1 hash</returns>
       private static string CalculateSHA1(string text, Encoding enc)
       {
           byte[] buffer = enc.GetBytes(text);
           SHA1CryptoServiceProvider cryptoTransformSHA1 =
           new SHA1CryptoServiceProvider();
           string hash = BitConverter.ToString(
               cryptoTransformSHA1.ComputeHash(buffer)).Replace("-", "");

           return hash;
       }

       private static string GetString(string URL)
       {
           WebRequest request = (WebRequest)
                    WebRequest.Create(URL);

           request.Proxy = null;

           WebResponse myResponse = request.GetResponse();
           StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.ASCII);
           string result = sr.ReadToEnd();
           sr.Close();
           myResponse.Close();
           return result;
       }
    }
}
