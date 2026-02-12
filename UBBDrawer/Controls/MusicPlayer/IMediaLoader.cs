using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media.Core;

namespace MusicPlayerControl;

public interface IMediaLoader
{
    MediaSource? LoadMedia(string src);
}

