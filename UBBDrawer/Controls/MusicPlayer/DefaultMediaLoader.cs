using Microsoft.UI.Xaml.Media;
using System;
using Windows.Media.Core;

namespace MusicPlayerControl
{
    public class DefaultMediaLoader : IMediaLoader
    {
        public MediaSource? LoadMedia(string src)
        {
            try
            {
                if (Uri.TryCreate(src, UriKind.Absolute, out var uri))
                {
                    if (uri.Scheme == "http" || uri.Scheme == "https")
                    {
                        return MediaSource.CreateFromUri(uri);
                    }
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