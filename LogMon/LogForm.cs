using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.IO;
using System.Threading;

namespace LogMon
{
	public partial class LogForm : Form
	{
		public LogForm()
		{
			InitializeComponent();
			Text = "LogMon " + Version;
			this.Resize += LogForm_Resize;
			try
			{
				ChatLabels = ChatPoster.ReadLabels();
			}
			catch(Exception e)
			{
				ChatLabels = new List<string>() { "Corp", "Alliance" };
			}

			foreach (string s in ChatLabels)
				watch_MessageRead(null, s);
			ChatQueue.StartQueueThreads();
			ChatQueue.MessageRead += watch_MessageRead;
			ChatPoster.MessagePosted += watch_MessageRead;

		}
		LogWatcher m_watcher;
		List<string> ChatLabels; 

		private void LogForm_Load(object sender, EventArgs e)
		{
			//need to get chat channel list from server to initialize watcher correctly
            m_watcher = new LogWatcher(new Properties.Settings().LogFolder, "*.txt", ChatLabels.ToArray());
#if DEBUG
			//m_watcher = new LogWatcher(@"C:\Users\Mikker\Documents\GitHub\Warps\Warps\bin\Debug\Logs", "*.log");
			//m_watcher = new LogWatcher(@"C:\Users\Mikker\Documents\EVE\logs\Chatlogs", "*.txt", ChatLabels.ToArray());
#endif
			m_watcher.FileWatched += watch_MessageRead;
		}
		
		public string Version { get { return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString(); } }
	
		#region TextBox

		//for debugging
		public delegate void AppendTextDelegate(string line);
		void watch_MessageRead(object sender, string e)
		{
			AppendText(e);
		}
		void AppendText(string line)
		{
			if (richTextBox1.InvokeRequired)
				richTextBox1.Invoke(new AppendTextDelegate(AppendText), line);
			else
			{
				richTextBox1.AppendText(line);
				richTextBox1.AppendText("\n");
				richTextBox1.Refresh();
			}

		}
		
		#endregion	

		#region NotifyIcon

		void LogForm_Resize(object sender, EventArgs e)
		{
			//hide the form instead of minimizing it, this simulates a minimize-to-tray
			if (FormWindowState.Minimized == this.WindowState)
			{
				WindowState = FormWindowState.Normal;//keep the window unminimized so it appears on the next show()
				Visible = false;// Hide();//hide it in place
			}
		}
		
		private void LogForm_Shown(object sender, EventArgs e)
		{
			//hide the form when it first starts up, causes a flicker but oh fucking well.
            Hide();
		}

		private void notifyIcon1_DoubleClick(object sender, EventArgs e)
		{
			//toggle visibilty
			Visible = !Visible;
		}

		private void exitToolStripMenuItem_Click(object sender, EventArgs e)
		{
			this.Close();
		} 

		#endregion
	}
}
