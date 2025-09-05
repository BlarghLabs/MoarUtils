using System.Collections.Generic;

namespace MoarUtils.models.gis {
  public class Polygon {
    public List<Coordinate> coordinates = new List<Coordinate>();

    public List<Polygon> exclusionaryPolygons = new List<Polygon>();
  }
}
