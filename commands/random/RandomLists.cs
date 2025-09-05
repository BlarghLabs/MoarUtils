using System;
using System.Collections.Generic;
using System.Linq;

namespace MoarUtils.Utils.random {
  public static class RandomLists {
    // The Random object this method uses.
    private static Random Rand = null;

    // Return num_items random values.
    public static List<T> PickRandom<T>(
        this List<T> values, int num_values) {
      // Create the Random object if it doesn't exist.
      if (Rand == null) Rand = new Random();

      // Don't exceed the array's length.
      if (num_values >= values.Count)
        num_values = values.Count - 1;

      // Make an array of indexes 0 through values.Length - 1.
      int[] indexes =
          Enumerable.Range(0, values.Count).ToArray();

      // Build the return list.
      List<T> results = new List<T>();

      // Randomize the first num_values indexes.
      for (int i = 0; i < num_values; i++) {
        // Pick a random entry between i and values.Length - 1.
        int j = Rand.Next(i, values.Count);

        // Swap the values.
        int temp = indexes[i];
        indexes[i] = indexes[j];
        indexes[j] = temp;

        // Save the ith value.
        results.Add(values[indexes[i]]);
      }

      // Return the selected items.
      return results;
    }


    //// Return num_items random values.
    //private static List<survey_review_adjective> PickRandom<survey_review_adjective>(this List<survey_review_adjective> values, int num_values) {
    //  // Create the Random object if it doesn't exist.
    //  Random Rand = null;
    //  if (Rand == null) Rand = new Random();

    //  // Don't exceed the array's length.
    //  if (num_values >= values.Count)
    //    num_values = values.Count - 1;

    //  // Make an array of indexes 0 through values.Length - 1.
    //  int[] indexes =
    //      Enumerable.Range(0, values.Count).ToArray();

    //  // Build the return list.
    //  List<survey_review_adjective> results = new List<survey_review_adjective>();

    //  // Randomize the first num_values indexes.
    //  for (int i = 0; i < num_values; i++) {
    //    // Pick a random entry between i and values.Length - 1.
    //    int j = Rand.Next(i, values.Count);

    //    // Swap the values.
    //    int temp = indexes[i];
    //    indexes[i] = indexes[j];
    //    indexes[j] = temp;

    //    // Save the ith value.
    //    results.Add(values[indexes[i]]);
    //  }

    //  // Return the selected items.
    //  return results;
    //}
  }
}
