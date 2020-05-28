using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using System.Linq;
using System.Collections.Generic;

public static class Utility
{
    public static T DeepClone<T>(this T obj)
    {
        using (var ms = new MemoryStream())
        {
            var formatter = new BinaryFormatter();
            formatter.Serialize(ms, obj);
            ms.Position = 0;

            return (T)formatter.Deserialize(ms);
        }
    }

    // Based on https://en.wikipedia.org/wiki/Heap%27s_algorithm
    // https://stackoverflow.com/questions/31762223/implementation-of-heaps-algorithm
    // https://gist.github.com/fdeitelhoff/5052484
    public static IEnumerable<IList<T>> Permutations<T>(IList<T> list)
    {
        ICollection<IList<T>> result = new List<IList<T>>();
        Permutations(list, list.Count, result);
        return result;
    }

    private static void Permutations<T>(IList<T> list, int n, ICollection<IList<T>> result)
    {
        if (n == 1)
        {
            result.Add(new List<T>(list));
        }
        else
        {
            for (int i = 0; i < n; i++)
            {
                Swap(list, i % 2 == 0 ? i : 0, n - 1);

                Permutations(list, n - 1, result);
            }
        }
    }

    private static void Swap<T>(IList<T> list, int i, int j)
    {
        T temp = list[i];
        list[i] = list[j];
        list[j] = temp;
    }
}
