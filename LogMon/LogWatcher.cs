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
            m_chatLabels = new List<string>(chatLabels);

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
            if (filter != null)
                DirWatch.Filter = filter;
            DirWatch.Created += DirWatch_Created;
            DirWatch.Changed += DirWatch_Created;
            DirWatch.EnableRaisingEvents = true;

            foreach (var channel in m_chatLabels)
            {
                int c = 5;
                var files = Directory.GetFiles(dir, channel + @"*");
                if (files.Length < c)
                {
                    c = files.Length;
                }
                    
                while (c > 0){
                    var file = files[files.Length - c];
                    WatchFile(file);
                    c--;
                }
            }
        }

        void DirWatch_Created(object sender, FileSystemEventArgs e)
        {
            WatchFile(e.FullPath);
        }

        private void WatchFile(string fullPath)
        {
            var fileName = Path.GetFileName(fullPath);
            var chatLabel = ChatLabel(fileName);
            if (m_chatLabels == null || m_chatLabels.Contains(chatLabel, StringComparer.InvariantCultureIgnoreCase))
            {
                if (!m_filesOpen.ContainsKey(fileName))
                {
                    System.Diagnostics.Debug.WriteLine("Opening file: " + fullPath);
                    var w = new Watcher(chatLabel, fullPath);
                    m_filesOpen[fileName] = Task.Factory.StartNew(() => w.WatchFile());
                    if (FileWatched != null)
                        FileWatched(this, fileName);
                }
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

        class Watcher
        {
            private string chatLabel;
            private string fileName;
            private DateTime lastMessageTime = DateTime.Now;
            private bool running = true;

            public Watcher(string chatLabel, string fileName)
            {
                this.chatLabel = chatLabel;
                this.fileName = fileName;
            }

            public void Stop()
            {
                running = false;
            }

            public bool IsStopped()
            {
                return running;
            }

            public void WatchFile()
            {
                using (FileStream fs = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                {
                    using (StreamReader sr = new StreamReader(fs, Encoding.Unicode))
                    {
                        fs.Seek(0, SeekOrigin.End);//start at the end of the file to avoid backlog and init messages
                        while (running)
                        {
                            while (!sr.EndOfStream)
                            {
                                ChatQueue.QueueMessage(chatLabel, sr.ReadLine());
                                lastMessageTime = DateTime.Now;
                            }

                            while (sr.EndOfStream)
                            {
                                Thread.Sleep(100); //Increase?
                                if (lastMessageTime < DateTime.Now.AddDays(-1))
                                {
                                    Stop();
                                }
                            }
                            //ChatQueue.QueueMessage(sr.ReadLine());
                        }
                    }
                }
            }
        }


    }
}
