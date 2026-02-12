// DefaultVideoLoader.cs
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.Media.Core;

namespace VideoPlayerControl
{
    public class DefaultVideoLoader : IVideoLoader
    {
        public MediaSource? LoadVideo(string src)
        {
            try
            {
                if (Uri.TryCreate(src, UriKind.Absolute, out var uri))
                {
                    // 支持 HTTP/HTTPS 流媒体
                    if (uri.Scheme == "http" || uri.Scheme == "https")
                    {
                        return MediaSource.CreateFromUri(uri);
                    }
                    // 支持本地应用资源
                    else if (uri.Scheme == "ms-appx" || uri.Scheme == "ms-appdata")
                    {
                        return MediaSource.CreateFromUri(uri);
                    }
                }

                // 尝试作为本地文件
                return MediaSource.CreateFromStorageFile(
                    Windows.Storage.StorageFile.GetFileFromPathAsync(src).AsTask().Result);
            }
            catch
            {
                return null;
            }
        }
    }
}

