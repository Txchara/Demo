using System;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("QuickStork: Start Running");

        int[] array = { 3, 8, 1, 4, 9, 2, 6, 7, 5 };

        //var bubble = SortHelper.BubbleSort(array);

        SortHelper helper = new SortHelper();

        //var bubble = SortHelper.BubbleSort(array);
        //Console.WriteLine(string.Join(",", bubble));

        //var Selection = SortHelper.SelectionSort(array);
        //Console.WriteLine(string.Join(",", Selection));

        //var Insertion = SortHelper.InsertionSort(array);
        //Console.WriteLine(string.Join(",", Insertion));

        var Insertion = SortHelper.QuickStork(array, 0, array.Length - 1);
        Console.WriteLine(string.Join(",", Insertion));

    }
}



public class SortHelper
{
    /// <summary>
    /// 冒泡
    /// </summary>
    /// <param name="Arry">需要传入Int数组</param>
    public static int[] BubbleSort(int[] Arry)
    {
        for (int i = 0; i < Arry.Length - 1; i++)
        {
            for (int j = 0; j < Arry.Length - 1; j++)
            {
                if (Arry[j] > Arry[j + 1])
                {
                    int Temp = Arry[j];
                    Arry[j] = Arry[j + 1];
                    Arry[j + 1] = Temp;
                }
            }
            Console.WriteLine(string.Join(",", Arry));
        }
        return Arry;
    }
    /// <summary>
    /// 选择
    /// </summary>
    /// <param name="Arry">需要传入Int数组</param>
    public static int[] SelectionSort(int[] Arry)
    {
        for (int i = 0; i < Arry.Length - 1; i++)
        {
            int MinIndex = i;
            for (int j = i; j < Arry.Length; j++)
            {
                if (Arry[j] < Arry[MinIndex])
                {
                    MinIndex = j;
                }
            }

            int Temp = Arry[i];
            Arry[i] = Arry[MinIndex];
            Arry[MinIndex] = Temp;
            Console.WriteLine(string.Join(",", Arry));
        }
        return Arry;
    }
    /// <summary>
    /// 插入
    /// </summary>
    /// <param name="Arry">需要传入Int数组</param>
    public static int[] InsertionSort(int[] Arry)
    {
        for (int i = 1; i < Arry.Length; i++)
        {
            int key = Arry[i];
            int j = i - 1;

            while (j >= 0 && Arry[j] > key)
            {
                Arry[j + 1] = Arry[j];
                j--;
            }

            Arry[j + 1] = key;
            Console.WriteLine(string.Join(",", Arry));
        }
        return Arry;
    }
    /// <summary>
    /// 快速排序
    /// </summary>
    /// <param name="Arry">需要传入Int数组</param>
    /// <param name="Left"></param>
    /// <param name="Right"></param>
    public static int[] QuickStork(int[] Arry, int Left, int Right)
    {
        if (Left >= Right)
            return Arry;

        int pivot = Arry[Left];
        int i = Left, j = Right;

        while (i < j)
        {
            while (i < j && Arry[j] >= pivot)
            {
                j--;
            }

            while (i < j && Arry[i] <= pivot)
            {
                i++;
            }

            if (i < j)
            {
                int Temp = Arry[i];
                Arry[i] = Arry[j];
                Arry[j] = Temp;
            }
            Console.WriteLine(string.Join(",", Arry));
        }

        // ⭐ pivot 归位（必须在 while 外）
        Arry[Left] = Arry[i];
        Arry[i] = pivot;

        // ⭐ 递归（也必须在 while 外）
        QuickStork(Arry, Left, i - 1);
        QuickStork(Arry, i + 1, Right);

        return Arry;
    }

    public static int[] QuickStorkParallel(int[] Arry, int Left, int Right, int depth = 0)
    {
        if (Left >= Right)
            return Arry;

        int pivot = Arry[Left];
        int i = Left, j = Right;

        while (i < j)
        {
            while (i < j && Arry[j] >= pivot) j--;

            while (i < j && Arry[i] <= pivot) i++;

            if (i < j)
            {
                (Arry[i], Arry[j]) = (Arry[j], Arry[i]);
            }
        }

        Arry[Left] = Arry[i];
        Arry[i] = pivot;

        Console.WriteLine(string.Join(",", Arry));

        // 👉 控制并行条件（非常关键）
        if (Right - Left > 1000 && depth < 4)
        {
            // 并行执行左右
            Parallel.Invoke(
                () => QuickStorkParallel(Arry, Left, i - 1, depth + 1),
                () => QuickStorkParallel(Arry, i + 1, Right, depth + 1)
            );
        }
        else
        {
            // 小数据走普通递归（更快）
            QuickStorkParallel(Arry, Left, i - 1, depth + 1);
            QuickStorkParallel(Arry, i + 1, Right, depth + 1);
        }

        return Arry;
    }
}


