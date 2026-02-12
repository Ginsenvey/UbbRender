// DownloadEventArgs.cs
using System;

namespace MusicPlayerControl
{
    public class DownloadEventArgs : EventArgs
    {
        public string Source { get; }

        public DownloadEventArgs(string source)
        {
            Source = source;
        }
    }
}