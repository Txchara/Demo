using System;
using System.Threading;

namespace SqlDemo.Infrastructure
{
    /// <summary>
    /// 雪花 ID 生成器。
    /// </summary>
    /// <remarks>
    /// 设计目标：
    /// 1. 在当前项目内提供一个“全局任何地方都能直接用”的 long 型唯一 ID 生成能力。
    /// 2. 不依赖数据库，不依赖第三方包，单文件即可使用。
    /// 3. 兼顾“按时间递增”和“同一毫秒内高并发唯一性”。
    /// 
    /// 位分配说明（总计 64 位，最高符号位固定为 0，所以结果始终为正数 long）：
    /// 1. 时间戳：41 位
    ///    存储的是“当前时间 - 自定义起始时间”的毫秒差值。
    /// 2. 数据中心编号：5 位
    ///    取值范围 0~31。
    /// 3. 工作机器编号：5 位
    ///    取值范围 0~31。
    /// 4. 毫秒内序列号：12 位
    ///    取值范围 0~4095，表示同一毫秒内最多可生成 4096 个 ID。
    /// 
    /// 这意味着：
    /// 1. 单实例每毫秒最多生成 4096 个 ID。
    /// 2. 只要 workerId + datacenterId 的组合不重复，多节点也能安全发号。
    /// 3. 结果类型是 long，适合直接映射到数据库 bigint。
    /// 
    /// 使用建议：
    /// 1. 如果你当前只是本地 Demo、单进程、单机器使用，直接用 Default 或 NewId() 即可。
    /// 2. 如果未来拆成多个服务或多台机器，请为每个节点配置不同的 datacenterId / workerId。
    /// 3. 不要每次生成 ID 时都 new 一个新的生成器实例，应该复用同一个实例；
    ///    否则在同一毫秒内，不同实例的序列号都从 0 开始，可能出现重复 ID。
    /// </remarks>
    public sealed class SnowflakeIdGenerator
    {
        /// <summary>
        /// 默认起始时间。
        /// 这里自定义为 2024-01-01 00:00:00 UTC。
        /// 之所以不直接用 Unix Epoch，是为了减少时间戳占用值，让生成出来的 ID 更紧凑。
        /// </summary>
        private static readonly DateTimeOffset DefaultEpoch =
            new DateTimeOffset(2024, 1, 1, 0, 0, 0, TimeSpan.Zero);

        /// <summary>
        /// 工作机器编号占用位数。
        /// </summary>
        private const int WorkerIdBits = 5;

        /// <summary>
        /// 数据中心编号占用位数。
        /// </summary>
        private const int DatacenterIdBits = 5;

        /// <summary>
        /// 同一毫秒内序列号占用位数。
        /// </summary>
        private const int SequenceBits = 12;

        /// <summary>
        /// 工作机器编号的最大值。
        /// 5 位二进制的最大值为 31。
        /// </summary>
        private const long MaxWorkerId = -1L ^ (-1L << WorkerIdBits);

        /// <summary>
        /// 数据中心编号的最大值。
        /// 5 位二进制的最大值为 31。
        /// </summary>
        private const long MaxDatacenterId = -1L ^ (-1L << DatacenterIdBits);

        /// <summary>
        /// 毫秒内序列号的最大值。
        /// 12 位二进制的最大值为 4095。
        /// </summary>
        private const long SequenceMask = -1L ^ (-1L << SequenceBits);

        /// <summary>
        /// 工作机器编号左移位数。
        /// 因为最低位先放序列号，所以 workerId 要左移 12 位。
        /// </summary>
        private const int WorkerIdShift = SequenceBits;

        /// <summary>
        /// 数据中心编号左移位数。
        /// 因为它要放在 workerId 前面，所以左移 12 + 5 位。
        /// </summary>
        private const int DatacenterIdShift = SequenceBits + WorkerIdBits;

        /// <summary>
        /// 时间戳左移位数。
        /// 因为时间戳要放在最高位区域，所以左移 12 + 5 + 5 位。
        /// </summary>
        private const int TimestampLeftShift = SequenceBits + WorkerIdBits + DatacenterIdBits;

        /// <summary>
        /// 用于保证并发安全的锁对象。
        /// 雪花算法的关键状态（lastTimestamp、sequence）必须串行更新。
        /// </summary>
        private readonly object _syncRoot = new object();

        /// <summary>
        /// 当前实例绑定的工作机器编号。
        /// </summary>
        private readonly long _workerId;

        /// <summary>
        /// 当前实例绑定的数据中心编号。
        /// </summary>
        private readonly long _datacenterId;

        /// <summary>
        /// 当前实例使用的起始时间。
        /// </summary>
        private readonly DateTimeOffset _epoch;

        /// <summary>
        /// 记录上一次发号时使用的“逻辑毫秒值”。
        /// 初始值为 -1，表示还没有生成过任何 ID。
        /// </summary>
        private long _lastTimestamp = -1L;

        /// <summary>
        /// 当前毫秒内已经使用到的序列号。
        /// 只有在“同一毫秒内重复发号”时才会递增。
        /// </summary>
        private long _sequence;

        /// <summary>
        /// 项目内默认的全局实例。
        /// 单机 Demo 场景下，你可以直接用这个实例，不需要自己 new。
        /// </summary>
        public static SnowflakeIdGenerator Default { get; } = new SnowflakeIdGenerator(workerId: 1, datacenterId: 1);

        /// <summary>
        /// 当前实例的工作机器编号。
        /// 主要用于排查问题或日志输出。
        /// </summary>
        public long WorkerId => _workerId;

        /// <summary>
        /// 当前实例的数据中心编号。
        /// 主要用于排查问题或日志输出。
        /// </summary>
        public long DatacenterId => _datacenterId;

        /// <summary>
        /// 使用默认起始时间创建一个雪花 ID 生成器实例。
        /// </summary>
        /// <param name="workerId">工作机器编号，范围 0~31。</param>
        /// <param name="datacenterId">数据中心编号，范围 0~31。</param>
        public SnowflakeIdGenerator(long workerId, long datacenterId)
            : this(workerId, datacenterId, DefaultEpoch)
        {
        }

        /// <summary>
        /// 使用指定起始时间创建一个雪花 ID 生成器实例。
        /// </summary>
        /// <param name="workerId">工作机器编号，范围 0~31。</param>
        /// <param name="datacenterId">数据中心编号，范围 0~31。</param>
        /// <param name="epoch">自定义起始时间，建议使用 UTC 时间。</param>
        /// <exception cref="ArgumentOutOfRangeException">当编号超出允许范围时抛出。</exception>
        public SnowflakeIdGenerator(long workerId, long datacenterId, DateTimeOffset epoch)
        {
            if (workerId < 0 || workerId > MaxWorkerId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(workerId),
                    $"workerId 的取值范围必须是 0 ~ {MaxWorkerId}。");
            }

            if (datacenterId < 0 || datacenterId > MaxDatacenterId)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(datacenterId),
                    $"datacenterId 的取值范围必须是 0 ~ {MaxDatacenterId}。");
            }

            var utcEpoch = epoch.ToUniversalTime();

            if (utcEpoch > DateTimeOffset.UtcNow)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(epoch),
                    "epoch 不能是未来时间，否则生成出来的时间戳会出现负值。");
            }

            _workerId = workerId;
            _datacenterId = datacenterId;
            _epoch = utcEpoch;
        }

        /// <summary>
        /// 使用项目内默认全局实例，直接生成一个新的雪花 ID。
        /// </summary>
        /// <returns>正数 long 类型 ID。</returns>
        /// <remarks>
        /// 这是“全局任何地方直接拿来用”的最简单入口。
        /// 例如：
        /// long userId = SnowflakeIdGenerator.NewId();
        /// </remarks>
        public static long NewId()
        {
            return Default.NextId();
        }

        /// <summary>
        /// 使用当前实例生成下一个雪花 ID。
        /// </summary>
        /// <returns>正数 long 类型 ID。</returns>
        public long NextId()
        {
            lock (_syncRoot)
            {
                // 取当前毫秒时间戳。
                // 这里返回的是“相对于自定义起始时间”的毫秒差，而不是 Unix 毫秒。
                var currentTimestamp = GetCurrentTimestamp();

                // 处理时钟回拨。
                // 某些机器在手动改时间、NTP 校时、虚拟机迁移时，系统时间可能短暂回退。
                //
                // 这里不直接抛异常，而是采用“逻辑时间不倒退”的策略：
                // 如果当前时间小于上次发号时间，就临时沿用上一次时间戳继续发号。
                // 这样做对业务更友好，避免因为几毫秒的时间回拨直接导致系统报错。
                if (currentTimestamp < _lastTimestamp)
                {
                    currentTimestamp = _lastTimestamp;
                }

                // 如果还处于同一毫秒内，说明需要靠序列号区分多个 ID。
                if (currentTimestamp == _lastTimestamp)
                {
                    // 序列号自增，并用掩码保证它永远只保留低 12 位。
                    _sequence = (_sequence + 1) & SequenceMask;

                    // 如果序列号回到了 0，说明当前毫秒的 4096 个序列号已经发完。
                    // 这时必须等到下一毫秒，再继续发号，否则会发生重复。
                    if (_sequence == 0)
                    {
                        currentTimestamp = WaitUntilNextMillisecond(_lastTimestamp);
                    }
                }
                else
                {
                    // 进入了新的毫秒，序列号从 0 重新开始。
                    _sequence = 0;
                }

                // 更新“上一次发号使用的时间戳”。
                _lastTimestamp = currentTimestamp;

                // 把各个部分拼装成最终的 64 位 long 值。
                //
                // 结构如下：
                // [ 时间戳(41位) | 数据中心(5位) | 工作机器(5位) | 序列号(12位) ]
                var id =
                    (currentTimestamp << TimestampLeftShift) |
                    (_datacenterId << DatacenterIdShift) |
                    (_workerId << WorkerIdShift) |
                    _sequence;

                return id;
            }
        }

        /// <summary>
        /// 获取当前时间相对于自定义起始时间的毫秒差值。
        /// </summary>
        /// <returns>逻辑时间戳（毫秒）。</returns>
        private long GetCurrentTimestamp()
        {
            return (long)(DateTimeOffset.UtcNow - _epoch).TotalMilliseconds;
        }

        /// <summary>
        /// 一直等待，直到进入下一毫秒。
        /// </summary>
        /// <param name="lastTimestamp">上一次成功发号时使用的毫秒值。</param>
        /// <returns>大于 lastTimestamp 的最新毫秒值。</returns>
        /// <remarks>
        /// 这个方法只会在“同一毫秒内序列号用完”的极端情况下触发。
        /// 正常业务里一般不会频繁进入这里。
        /// </remarks>
        private long WaitUntilNextMillisecond(long lastTimestamp)
        {
            var timestamp = GetCurrentTimestamp();

            while (timestamp <= lastTimestamp)
            {
                // 短暂自旋等待，避免空转导致 CPU 占用过高。
                Thread.SpinWait(1);
                timestamp = GetCurrentTimestamp();
            }

            return timestamp;
        }
    }
}
