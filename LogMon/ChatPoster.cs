using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Web;
using System.Net.Security;

namespace LogMon
{
	public static class ChatPoster
	{
		public static event EventHandler<string> MessagePosted; 

		public static void PostMessage(string message)
		{
			try
			{
				NameValueCollection data = ParseMessage(message);
				//byte[] data = ParseMessage(message);
                data["InstanceId"] = Program.InstanceId.ToString();
				PostParsed(data);
			}
			catch {  }//do nothing, i dont care if it fails
		}

	//	static void PostParsed(byte[] data)
		static void PostParsed(NameValueCollection data)
		{
			if (data == null)
				return;

			var sslFailureCallback = new RemoteCertificateValidationCallback(delegate { return true; });
			using (var wb = new WebClient())
			{

				try
				{
					ServicePointManager.ServerCertificateValidationCallback += sslFailureCallback;
					//wb.UploadValuesCompleted += wb_UploadValuesCompleted;
                    var url = new Properties.Settings().ReportIntelUrl;
					string response = Encoding.UTF8.GetString(wb.UploadValues(url, "POST", data));
					//if (MessagePosted != null)
						//MessagePosted(wb, response);

					//var response = webClient.UploadData(Options.Address, "POST", Encoding.ASCII.GetBytes(Options.PostData));

				}
				catch (Exception err)
				{
					if (MessagePosted != null)
						MessagePosted(wb, err.Message);

				}
				finally
				{

					ServicePointManager.ServerCertificateValidationCallback -= sslFailureCallback;

				}

			}


			//if (WebAddress == null || data == null)
			//	return;
			//using (var wb = new WebClient())
			//{
			//	//var resp = wb.UploadValues(WebAddress, "POST", data);
			//	//var resp = wb.UploadData(WebAddress, "POST", data);
			//	//string responsefromserver = Encoding.UTF8.GetString(resp);
			//	wb.UploadValuesCompleted += wb_UploadValuesCompleted;
			//	wb.UploadValuesAsync(WebAddress, "POST", data);
			//}
		}

		static void wb_UploadValuesCompleted(object sender, UploadValuesCompletedEventArgs e)
		{
			string responsefromserver;
			try
			{
				if (e.Result != null)
					responsefromserver = Encoding.UTF8.GetString(e.Result);
				else
					responsefromserver = e.Error.Message;
			}
			catch (Exception ex)
			{
				responsefromserver = ex.InnerException != null ? ex.InnerException.Message : ex.Message;
			}
			if (MessagePosted != null)
				MessagePosted(sender, responsefromserver);
		}

		//static byte[] ParseMessage(string message)
		static NameValueCollection ParseMessage(string message)
		{
			NameValueCollection nvc = new NameValueCollection();
			//nvc["username"] = "myUser";
			//nvc["password"] = "myPassword";
			//Timestamp, ReportedBy, Message and Channel
/*
[Corp] ﻿[ 2013.06.15 21:19:05 ] BigBank Hank74 > no problem
[Corp] ﻿[ 2013.07.02 05:45:09 ] Stormerr > schadenfraude is good
*/
			int ns, ne;
			//chatfile
			ns = message.IndexOf('[');
			if (ns < 0)
				return null;
			ne = message.IndexOf(']',ns);
			if (ne < 0)
				return null;
			string item = message.Substring(++ns, ne - ns);
			////chatlabel
			//ns = item.LastIndexOf('_');
			//ns = item.LastIndexOf('_', --ns);
			//item = item.Substring(0, ns);
			nvc["Channel"] = item;

			//datetime
			//message = message.Substring(ne);//get remaining string
			ns = message.IndexOf('[',ne);
			if (ns < 0)
				return null;//no message
			ne = message.IndexOf(']',++ns);
			if (ne < 0)
				return null;
			item = message.Substring(ns, ne - ns);
			//DateTime time = DateTime.Parse(item);
			nvc["Timestamp"] = item.Trim();

			//character
			ns = ne;
			ne = message.IndexOf('>', ++ns);
			if (ne < 0)
				return null;
			item = message.Substring(ns, ne - ns);
			nvc["ReportedBy"] = item.Trim();

			//message
			message = message.Substring(++ne);
			nvc["Message"] = message.Trim();

#if DEBUG
			Dictionary<string, string> deb = new Dictionary<string, string>();
			for (int i = 0; i < nvc.Count; i++)
				deb.Add(nvc.AllKeys[i], nvc[i]);

			//string postData = "Channel=" + HttpUtility.UrlEncode(nvc["Channel"]) +
			//   "&Timestamp=" + HttpUtility.UrlEncode(nvc["Timestamp"]) +
			//   "&ReportedBy=" + HttpUtility.UrlEncode(nvc["ReportedBy"]) +
			//   "&Message=" + HttpUtility.UrlEncode(nvc["Message"]);
			//byte[] byteArray = Encoding.ASCII.GetBytes(postData);
			//return byteArray;
#endif
			return nvc;
		}

		//public static void PostMessage(string message)
		//{
		//	if (WebAddress == null)
		//		return;
		//	HttpWebRequest httpWReq = (HttpWebRequest)WebRequest.Create(WebAddress);

		//	ASCIIEncoding encoding = new ASCIIEncoding();
		//	//string postData = "username=user";
		//	//postData += "&password=pass";
		//	byte[] data = encoding.GetBytes(message);

		//	httpWReq.Method = "POST";
		//	httpWReq.ContentType = "application/x-www-form-urlencoded";
		//	httpWReq.ContentLength = data.Length;

		//	using (Stream stream = httpWReq.GetRequestStream())
		//	{
		//		stream.Write(data, 0, data.Length);
		//	}

		//	WebResponse response = httpWReq.GetResponse();
		//	response.Close();



		//}

        /// <summary>
        /// Gets a list of channel names to watch
        /// </summary>
        /// <returns>A list of the channels to watch</returns>
		internal static List<string> ReadLabels()
		{
            var url = new Properties.Settings().ChannelListUrl;
			var sslFailureCallback = new RemoteCertificateValidationCallback(delegate { return true; });
			List<string> labels = null;

			try
			{
				ServicePointManager.ServerCertificateValidationCallback += sslFailureCallback;


				var response = new WebClient().DownloadString(url);
				//var data = new WebClient().DownloadData(php);

                labels = XDocument.Parse(response.Trim()).Root
						  .Elements()
                          .Select(e => e.Value).ToList();


				return labels;
			}
			catch (Exception err)
			{
				if (MessagePosted != null)
					MessagePosted(null, err.Message);

			}
			finally
			{
				ServicePointManager.ServerCertificateValidationCallback -= sslFailureCallback;
			}

			return labels;
		}
	}
}
