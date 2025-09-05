using System;

namespace MoarUtils.models.commands {
  public class DownloadFile : IDisposable {
    public byte[] bytes;
    public string fileName;
    public string htmlContentType;

    public void Dispose() {
      bytes = null;
      fileName = null;
      htmlContentType = null;

      GC.SuppressFinalize(this);
    }

    ~DownloadFile() {
      Dispose();
    }
  }
}


