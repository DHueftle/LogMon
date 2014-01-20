using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogMon
{
	class LogWatcher
	{
		public LogWatcher(string dir, string filter, params string[] chatLabels)
		{
			if( chatLabels.Length > 0 )
				m_chatLabels= new List<string>(chatLabels);

			SetWatchDir(dir, filter);
		}

		public event EventHandler<string> FileWatched; 

		List<string> m_chatLabels = null;
		FileSystemWatcher DirWatch;
		Dictionary<string, Task> m_filesOpen = new Dictionary<string, Task>();

		public string WatchDir
		{
			get { return DirWatch.Path; }
		}
		public string LogFilter
		{
			get { return DirWatch.Filter; }
		}

		void SetWatchDir(string dir, string filter)
		{

			if (dir == null)
				dir = Path.GetDirectoryName(Application.ExecutablePath);
            dir = Environment.ExpandEnvironmentVariables(dir);
			DirWatch = new FileSystemWatcher(dir);
			DirWatch.IncludeSubdirectories = false;
			DirWatch.InternalBufferSize *= 2;//larger buffer cause people don't clean their logs folder
			if( filter != null )
				DirWatch.Filter = filter;
			DirWatch.Created += DirWatch_Created;
			DirWatch.Changed += DirWatch_Created;
			DirWatch.EnableRaisingEvents = true;
		}

		void DirWatch_Created(object sender, FileSystemEventArgs e)
		{
			string chatLabel = ChatLabel(e.Name);
			if (m_chatLabels == null || m_chatLabels.Contains(chatLabel) )
				//m_chatLabels.Find(str => e.Name.StartsWith(str)) != null)
				if (!m_filesOpen.ContainsKey(e.Name))
				{
					m_filesOpen[e.Name] = Task.Factory.StartNew(() => WatchFile(chatLabel, e.FullPath));
					if (FileWatched != null)
						FileWatched(this, e.Name);
				}
		}

		string ChatLabel(string filename)
		{
			int ns;
			//chatlabel
			ns = filename.LastIndexOf('_');
			if (ns < 0)
				return filename;
			ns = filename.LastIndexOf('_', --ns);
			if (ns < 0)
				return filename;
			return filename.Substring(0, ns);
		}

		void WatchFile(string chatLabel, string fileName)
		{
			using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
			{
				using (StreamReader sr = new StreamReader(fs,Encoding.Unicode))
				{
					fs.Seek(0, SeekOrigin.End);//start at the end of the file to avoid backlog and init messages
					while (true)
					{
						while (!sr.EndOfStream)
							ChatQueue.QueueMessage(chatLabel, sr.ReadLine());
						while (sr.EndOfStream)
							Thread.Sleep(100);
						//ChatQueue.QueueMessage(sr.ReadLine());
					}
				}
			}
		}


	}
}
