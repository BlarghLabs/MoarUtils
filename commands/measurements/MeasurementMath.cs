using System;

namespace MoarUtils.Utils.measurements {
  public class MeasurementMath {
    #region volume  
    //http://www.formulaconversion.com/formulaconversioncalculator.php?convert=cubicfeet_to_cubicmeters
    public static Decimal? CubicFeetToCubicMeters(Decimal? cubicFeet) {
      if (!cubicFeet.HasValue) {
        return null;
      }
      //was: var result = cubicFeet.Value / 35.31466672M;
      //http://www.formulaconversion.com/formulaconversioncalculator.php?convert=cubicfeet_to_cubicmeters
      //http://cashmancuneo.net/paper/metriconv.pdf
      var result = cubicFeet.Value * 0.02831684659M;
      return result;
    }

    public static Decimal? CubicMetersToCubicFeet(
      Decimal? cubicMeters,
      bool roundToZeroDecimals = false
    ) {
      if (!cubicMeters.HasValue) {
        return null;
      }

      //was: var result = cubicMeters.Value * 35.31466672M;
      var result = cubicMeters.Value * 35.314670111696704M;

      //https://books.google.rs/books?id=RAxmewUGvf4C&pg=PA170&lpg=PA170&dq=0.02831684659&source=bl&ots=lc-4fbSxmt&sig=eR8-fpHHo646c1kf8JVmN6NRiws&hl=en&sa=X&ved=0ahUKEwiM6diaye_VAhUJ6xQKHRL1AbMQ6AEINjAE#v=onepage&q=0.028316846590.02831684659&f=false
      //http://www.formulaconversion.com/formulaconversioncalculator.php?convert=cubicfeet_to_cubicmeters
      //http://cashmancuneo.net/paper/metriconv.pdf
      //OR: var result = cubicMeters.Value / 0.02831684659M;

      return !roundToZeroDecimals ? result : Math.Round(result, 0);
    }
    #endregion

    #region weight
    public static Decimal? KilogramsToPounds(
      Decimal? kg,
      bool roundToZeroDecimals = false
    ) {
      if (!kg.HasValue) {
        return null;
      }
      var result = kg.Value * 2.20462262M;
      return !roundToZeroDecimals ? result : Math.Round(result, 0);
    }

    public static Decimal? PoundsToKilograms(Decimal? lb) {
      if (!lb.HasValue) {
        return null;
      }
      var result = lb.Value / 2.20462262M;
      return result;
    }
    #endregion

    #region length 
    public static Decimal? CentimetersToInches(
      Decimal? cm,
      bool roundToZeroDecimals = false
    ) {
      if (!cm.HasValue) {
        return null;
      }
      var result = cm.Value / 2.54M;
      return !roundToZeroDecimals ? result : Math.Round(result, 0);
    }

    public static Decimal? InchesToCentimeters(
      Decimal? inches
    ) {
      if (!inches.HasValue) {
        return null;
      }
      var result = inches.Value * 2.54M;
      return result;
    }
    #endregion
  }
}
