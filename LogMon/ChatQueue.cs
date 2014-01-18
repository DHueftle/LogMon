using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using LogMon;

static class ChatQueue
{
	static private readonly int _waitTime = 500;
	static Queue<string> uploadBuffer = new Queue<string>();

	public static event EventHandler<string> MessageRead; 

	public static void StartQueueThreads()
	{
		Thread upload = new Thread(() =>
		{
			while (true)
			{
				lock (uploadBuffer)
				{
					while (uploadBuffer.Count > 0)
					{
						string msg = uploadBuffer.Dequeue();
						ChatPoster.PostMessage(msg);
//#if DEBUG
						if (MessageRead != null)
							MessageRead(null, msg);
//#endif
					}
				}

				Thread.Sleep(_waitTime);
			}
		});

		upload.IsBackground = true;
		upload.Start();
	}

	public static void QueueMessage(string log, string message)
	{
		lock (uploadBuffer)
		{
			if (message.Length > 0)
			{
				string msg = String.Format("[{0}] {1}", log, message);
				if (!uploadBuffer.Contains(msg))
					uploadBuffer.Enqueue(msg);
			}
		}
	}
}