using System;
using System.Text.Json.Nodes;

class Program
{
    static void Main(string[] args)
    {
        MyListDemo.RunIndexOfDemo();

        //MyListDemo.Delegates();
    }

    public class MyList<T>
    {
        /// <summary>
        /// 容器
        /// </summary>
        private T[] _Items;

        /// <summary>
        /// 当前已使用数量
        /// </summary>
        private int _Count;

        /// <summary>
        /// 初始化（构造函数）
        /// </summary>
        public MyList()
        {
            _Items = new T[4];
            _Count = 0;
        }

        /// <summary>
        /// 内部数量（只读）
        /// </summary>
        public int Count
        {
            get
            {
                return _Count;
            }
        }

        /// <summary>
        /// 修改
        /// </summary>
        /// <param name="Index">需要修改的下标</param>
        /// <param name="value">需要修改的值</param>
        public void Set(int Index, T value)
        {
            if (Index < 0 || Index >= _Count)
                throw new("下标超出");

            _Items[Index] = value;
        }

        /// <summary>
        /// 新增数据，非插入
        /// </summary>
        /// <param name="Value">需要新增的数据</param>
        public void Add(T Value)
        {
            if (_Items.Length == _Count)
                ListExpansion();

            _Items[_Count] = Value;
            _Count++;
        }

        /// <summary>
        /// 获取全部数据【支持foreach】
        /// </summary>
        public IEnumerable<T> GetAll()
        {
            for (int i = 0; i < _Count; i++)
            {
                yield return _Items[i];
            }
        }

        /// <summary>
        /// 获取全部数据【不支持foreach】
        /// </summary>
        /// <returns></returns>
        public T[] ToArray()
        {
            T[] result = new T[_Count];

            for (int i = 0; i < _Count; i++)
            {
                result[i] = _Items[i];
            }

            return result;
        }

        /// <summary>
        /// 读取数据，【根据下标】，只读
        /// </summary>
        /// <param name="Index"></param>
        /// <returns></returns>
        public T this[int Index]
        {
            get
            {
                if (Index < 0 || Index >= _Count)
                    throw new("下标超出");

                return _Items[Index];
            }
        }

        /// <summary>
        /// 查找指定值第一次出现的位置
        /// </summary>
        /// <param name="Value">要查找的值</param>
        /// <returns>存在则返回下标，否则返回-1</returns>
        public int IndexOf(T Value)
        {
            for (int i = 0; i < _Count; i++)
            {
                if (Equals(_Items[i],Value))
                {
                    return i;
                }
            }

            return -1;
        }

        /// <summary>
        /// 按照下标删除对应值
        /// </summary>
        /// <param name="Index">要删除的下标</param>
        public void RemoveAt(int Index)
        {
            if (Index < 0 || Index >= _Count)            
                throw new("下标超出");
            
            //删除之后，需要把后面的元素整体往前挪
            for (int i = Index; i < _Count -1; i++)
            {
                _Items[i] = _Items[i + 1];
            }

            _Items[_Count - 1] = default!;
            _Count--;
        }

        /// <summary>
        /// 按值删除，删除第一个找到的值【顺序】
        /// </summary>
        /// <param name="Value">要删除的值</param>
        /// <returns></returns>
        public bool Remove(T Value)
        {
            int Index = IndexOf(Value);

            if (Index == -1)
            {
                return false;
            }

            RemoveAt(Index);
            return true;

            #region 原始逻辑
            // for (int i = 0; i < _Count; i++)
            // {
            //     if (Equals(_Items[i],Value))
            //     {
            //         RemoveAt(i);
            //         //删除之后会留空吗，后面的元素需要向前挪吗
            //         return true;
            //     }
            // }
            // return false;
            #endregion            
        }

        /// <summary>
        /// 插入数据
        /// </summary>
        /// <param name="Index"></param>
        /// <param name="Value"></param>
        public void Insert(int Index,T Value)
        {
            if (Index < 0 || Index > _Count)
                throw new ("下标超出");
            
            if (_Items.Length == _Count)
                ListExpansion();

            for (int i = _Count; i < Index; i--)
            {
                _Items[i] = _Items[i - 1];
            }

            _Items[Index] = Value;
            _Count++;
        }

        /// <summary>
        /// 挪动元素
        /// </summary>
        /// <param name="OldIndex">原下标</param>
        /// <param name="NewIndex">新下标</param>
        public void Move(int OldIndex,int NewIndex)
        {
            //判断是否越界
            if (OldIndex < 0 || OldIndex >= _Count || NewIndex < 0 || NewIndex >= _Count)
                throw new("下标超出范围");

            //两个下标不变则不移动
            if (OldIndex == NewIndex)
                return;

            //先把要移动的元素临时保存
            T Temp = _Items[OldIndex];

            //向前移动
            if (OldIndex < NewIndex)
            {
                for (int i = OldIndex; i < NewIndex; i++)
                {
                    _Items[i] = _Items[i + 1];
                }
            }

            //向后移动
            if (OldIndex > NewIndex)
            {
                for (int i = OldIndex; i > NewIndex; i--)
                {
                    _Items[i] = _Items[i - 1];
                }
            }

            //把临时保存的元素放到目标位置
            _Items[NewIndex] = Temp;
        }

        #region List自动扩容
        /// <summary>
        /// 扩容
        /// </summary>
        private void ListExpansion()
        {
            //定义长度
            int NewCapacity = 0;

            //长度为0，就给一个初始值
            if (_Items.Length == 0)
                NewCapacity = 4;
            //不为零，就改为现在长度 * 2
            else
                NewCapacity = _Items.Length * 2;

            T[] newArray = new T[NewCapacity];

            //List内数据搬运======_Items.Length和_Count区别：只要有效数据，内部数据不一定满
            for (int i = 0; i < _Count; i++)
            {
                newArray[i] = _Items[i];
            }

            //数组是“引用类型”,要让新的长度（NewCapacity）指向新的数组
            _Items = newArray;
        }
        #endregion
    }
}
