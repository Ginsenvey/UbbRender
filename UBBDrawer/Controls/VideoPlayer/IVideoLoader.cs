using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace VideoPlayerControl
{
    public interface IVideoLoader
    {
        MediaSource? LoadVideo(string src);
    }
}
