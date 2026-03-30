using System;

/// <summary>
/// unsafe 【不安全】
/// </summary>
unsafe public class MyListDemo
{
    /// <summary>
    /// Main函数中调用
    /// </summary>
    public static void RunIndexOfDemo()
    {
        var list = new Program.MyList<int>();

        for (int i = 1; i <= 10; i++)
        {
            list.Add(i * 10);
            //Console.WriteLine($"{i}: {list[i - 1]}");
        }

        


        foreach (var item in list.GetAll())
        {
            //Console.WriteLine(item);
        }
    }

    static void Delegates()
    {
        DelegateTest t = new DelegateTest();
        t.Base = 10;

        DelegateTest* p = &t;

        MyDelegate d = new MyDelegate(
                p,
                &Warpper.AddWarpper
            );

        int result = d.Invoke(56, 35);

        Console.WriteLine(result);

        //防止GC移动指针（对象）
        //fixed (DelegateTest* p = &t)
        //{

        //}
    }
}
