using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;
using Ionic.Zip;
using MoarUtils.commands.logging;
using MoarUtils.models.gis;

namespace MoarUtils.commands.gis {
  public class Kml {
    public const decimal DEFAULT_ICON_SCALE = (decimal)0.5;
    public const decimal DEFAULT_LABEL_SCALE = (decimal)0.5;

    private static List<string> CreateStyleCollection(List<Coordinate> coordinates) {
      List<string> los = new List<string>();

      foreach (var c in coordinates) {
        c.iconUrl = !string.IsNullOrWhiteSpace(c.iconUrl) ? c.iconUrl : "http://maps.google.com/mapfiles/kml/paddle/red-circle.png";
        if (!los.Contains(c.iconUrl)) {
          los.Add(c.iconUrl);
        }
      }

      return los;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="kml"></param>
    /// <param name="kmzFileNameWithoutExtension"></param>
    /// <param name="outPath">full out path, should end in .kmz</param>
    public static void SaveKmz(List<Coordinate> coordinates, string outPath) {
      try {
        using (ZipFile zf = new ZipFile()) {
          using (MemoryStream kml = SaveKmz(coordinates, true, true, true)) {
            ZipEntry oZipEntry = zf.AddEntry("doc.kml", kml);
            zf.Save(outPath);
          }
        }
      } catch (Exception ex) {
        LogIt.E(ex);
        throw;
      }
    }

    public static MemoryStream SaveKmz(List<Coordinate> coordinates, bool includeNewlines = true, bool includeTitle = false, bool includeDesc = false, decimal labelScale = DEFAULT_LABEL_SCALE, decimal iconScale = DEFAULT_ICON_SCALE) {
      MemoryStream ms = new MemoryStream();
      XmlTextWriter xtw = new XmlTextWriter(ms, System.Text.Encoding.UTF8);
      xtw.WriteStartDocument();
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
      xtw.WriteStartElement("kml");
      xtw.WriteAttributeString("xmlns", "http://earth.google.com/kml/2.0");
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
      xtw.WriteStartElement("Document");
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);

      #region Style Definitions
      /*
      <!--Style Definitions (START)-->
      <Style id="83">
        <IconStyle>
          <scale>0.5</scale>
          <Icon>
            <href>http://xxxx/icon.png</href>
          </Icon>
        </IconStyle>
        <LabelStyle>
          <scale>0.5</scale>
        </LabelStyle>
      </Style>
      <!-- Style Definitions (END) -->
      */
      xtw.WriteComment("Style Definitions (START)");
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);

      List<string> los = CreateStyleCollection(coordinates);
      foreach (var s in los) {
        xtw.WriteRaw("<Style id=\"" + los.IndexOf(s).ToString() + "\">" + Environment.NewLine);
        xtw.WriteRaw("<IconStyle>" + Environment.NewLine);
        xtw.WriteRaw("<scale>" + iconScale.ToString() + "</scale>" + Environment.NewLine);
        xtw.WriteRaw("<Icon>" + Environment.NewLine);
        xtw.WriteRaw("<href>" + s.Trim().Replace(" ", "%20") + "</href>" + Environment.NewLine);
        xtw.WriteRaw("</Icon>" + Environment.NewLine);
        xtw.WriteRaw("</IconStyle>" + Environment.NewLine);
        xtw.WriteRaw("<LabelStyle>" + Environment.NewLine);
        xtw.WriteRaw("<scale>" + labelScale.ToString() + "</scale>" + Environment.NewLine);
        xtw.WriteRaw("</LabelStyle>" + Environment.NewLine);
        xtw.WriteRaw("</Style>" + Environment.NewLine);
      }

      xtw.WriteComment("Style Definitions (END)");
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
      #endregion

      foreach (var c in coordinates) {
        xtw.WriteStartElement("Placemark");
        if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);

        #region title
        if (includeTitle) {
          //xtw.WriteElementString("name", oMapData.sTitle);
          //if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
          xtw.WriteStartElement("name");
          if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
          xtw.WriteCData(c.title);
          if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
          xtw.WriteEndElement();
          if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
        }
        #endregion
        #region Description
        if (includeDesc) {
          xtw.WriteStartElement("description");
          if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
          xtw.WriteCData(c.desc);
          if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
          xtw.WriteEndElement();
          if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
        }
        #endregion

        #region Style
        xtw.WriteElementString("styleUrl", "#" + los.IndexOf(c.iconUrl.Trim().Replace(" ", "%20")).ToString()); //do the trim&replace elsewhere, like when icon url is set in kmlpoint
        #endregion

        xtw.WriteStartElement("Point");
        if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
        xtw.WriteElementString("coordinates", c.lng.ToString() + "," + c.lat.ToString() + ",0");
        if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
        xtw.WriteEndElement();
        if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
        xtw.WriteEndElement();
        if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
      }

      xtw.WriteEndElement();
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
      xtw.WriteEndElement();
      if (includeNewlines) xtw.WriteWhitespace(Environment.NewLine);
      xtw.WriteEndDocument();
      xtw.Flush();

      ms.Position = 0;
      return ms;
    }

  }
}
