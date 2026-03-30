using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;

unsafe class DelegateDemo
{
    /// <summary>
    /// 指针
    /// </summary>
    private delegate*<int, int, int> _func;

    public DelegateDemo(delegate*<int, int, int> func)
    {
        _func = func;   
    }

    public int Invoke(int a, int b)
    {
        return _func(a,b);
    }
}

unsafe class MyDelegate
{
    //指向对象实例（this）
    private void* _target;

    //指针，第一个是this，用void*表示
    private delegate*<void*, int, int, int> _func;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="target"></param>
    /// <param name="func"></param>
    public MyDelegate(void* target, delegate*<void*, int, int, int> func)
    {
        _target = target;
        _func = func;
    }

    /// <summary>
    /// 调用
    /// </summary>
    /// <param name="a"></param>
    /// <param name="b"></param>
    /// <returns></returns>
    public int Invoke(int a, int b)
    {
        //把target作为第一个参数传进去
        return _func(_target, a, b);
    }
}

unsafe struct DelegateTest
{
    public int Base;

    public int Add(int a, int b)
    {
        return a + b + Base;
    }
}

unsafe static class Warpper
{
    public static int AddWarpper(void* obj, int a, int b)
    {
        //
        DelegateTest* t = (DelegateTest*)obj;

        //
        return t->Add(a,b);
    }
}
