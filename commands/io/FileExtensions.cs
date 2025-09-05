using System;
using System.IO;
using System.Linq;

namespace MoarUtils.Utils.IO {
  public class FileExtensions {
    public static bool isVideo(string path) {
      string[] mediaExtensions = {
        ".webm",
        ".mkv",
        ".flv",
        ".vob",
        ".ogv",
        ".ogg",
        ".drc",
        ".gif",
        ".gifv",
        ".mng",
        ".avi",
        ".mov",
        ".qt",
        ".wmv",
        ".yuv",
        ".rm",
        ".rmvb",
        ".asf",
        ".mp4",
        ".m4p",
        ".m4v",
        ".mpg",
        ".mp2",
        ".mpeg",
        ".mpe",
        ".mpv",
        ".mpg",
        ".mpeg",
        ".m2v",
        ".svi",
        ".3gp",
        ".3g2",
        ".mxf",
        ".roq",
        ".nsv",
        ".flv",
        ".f4v",
        ".f4p",
        ".f4a",
        ".f4b"
      };
      var isVideo = mediaExtensions.Contains(Path.GetExtension(path), StringComparer.CurrentCultureIgnoreCase);
      return isVideo;
    }

    public static bool isImage(string path) {
      string[] mediaExtensions = {
        ".PNG", ".JPG", ".JPEG", ".BMP", ".GIF", //etc
      };
      var isImage = mediaExtensions.Contains(Path.GetExtension(path), StringComparer.CurrentCultureIgnoreCase);
      return isImage;
    }

  }
}


