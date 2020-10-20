## [Orleans](http://dotnet.github.io/orleans/)
Orleans是一个用于快速开发分布式应用的跨平台的.NET开源框架。

名词解释：
- Grain - 谷物，即单个任务
- Silo - 谷仓，即单个执行器，其中可以运行多个Grain
- Cluster - 集群，由多个Silo组成

## 功能特点

### [持久化](https://dotnet.github.io/orleans/Documentation/grains/grain_persistence/index.html)
Grain的状态和数据支持持久化到不同的数据对象。状态在Grain被激活时加载，以保证Grain被调用时，状态是可用的、一致的。

### [分布式事务ACID](https://dotnet.github.io/orleans/Documentation/grains/transactions.html)
ACID解释：
- 原子性 Atomicity
- 一致性 Consistency
- 隔离性 Isolation
- 持久性 Durability

## 简介
除非通过消息传递，否则不要在谷物实例之间共享数据。

Orleans运行时为每一个谷物提供单线程的执行保证。
	
典型的谷物封装了单个实体的状态和行为（例如特定的用户、设备或会话）。

谷物的生命周期如下：
1. 另一种谷物或客户对谷物的方法进行调用（通过谷物引用）
2. 谷物被激活（如果它还没有在集群中的某个地方被激活）和一个谷物类的实例，称为谷物激活，就会被创建
    1. 如果适用的话，谷物的构造函数将利用依赖注入来执行
    2. 如果使用声明性持久性，则从存储器中读取谷物状态
    3. 如果被重写，OnActivateAsync被调用
3. 谷物处理传入的请求
4. 谷物保持闲置一段时间
4. 谷仓运行时决定关闭谷物
4. 谷仓运行时调用OnDeactivateAsync，如果被重写
4. 谷仓运行时从内存中移除谷物
	
服务器执行流程如下：
1. 收到一个web请求
1. 执行必要的身份验证和授权验证
1. 决定哪一种谷物应该处理该请求
1. 使用谷物客户端调用谷物的一个或多个方法
1. 处理谷物调用的成功完成或失败以及返回的值
1. 发送web请求的响应
	

通常，您有一个带有setter和getter的属性，但是在定义谷物接口时，您需要避免完全使用属性，只支持方法。例如：
```
public interface IEmployee : IGrainWithGuidKey
{
    Task<int> GetLevel();
    Task Promote(int newLevel);

    Task<IManager> GetManager();
    Task SetManager(IManager manager);
}
```

Nuget Packages:
```
Install-Package Microsoft.Orleans.OrleansCodeGenerator.Build -version 1.4.0
Install-Package Microsoft.Orleans.Core
Install-Package Microsoft.Orleans.Server
Install-Package Microsoft.Orleans.Client
```	
	
当奥尔良向另一个谷物发送信息时，它会创建一个对象的深层副本，并将副本提供给第二种谷物，而不是存储在第一粒谷物中的对象。

深层拷贝的代价高昂，可以通过[Immutable]特性标记对象，表示对象不会被修改，使得奥尔良不进行深拷贝。

## 任务调度上下文
- `Task.Factory.StartNew`, `Task.ContinuewWith`, `Task.WhenAny`, `Task.WhenAll`, `Task.Delay` 执行的任务会运行在当前 Grain 调度上下文。
- `Task.Run` 会使用 .Net 线程池的调度上线文，即 `TaskScheduler.Default` 。

### [如何在 Grain 中的非 Grain 上下文中调用 Grain](http://dotnet.github.io/orleans/Documentation/grains/external_tasks_and_grains.html#advanced-example---making-a-grain-call-from-code-that-runs-on-a-thread-pool)
实例代码如下：
```
public async Task MyGrainMethod()
{
    // Grab the Orleans task scheduler
    var orleansTs = TaskScheduler.Current;
    Task<int> t1 = Task.Run(async () =>
    {
         // This code runs on the thread pool scheduler, not on Orleans task scheduler
         Assert.AreNotEqual(orleansTS, TaskScheduler.Current);
         // You can do whatever you need to do here. Now let's say you need to make a grain call.
         Task<Task<int>> t2 = Task.Factory.StartNew(() =>
         {
            // This code runs on the Orleans task scheduler since we specified the scheduler: orleansTs.
            Assert.AreEqual(orleansTS, TaskScheduler.Current);
            return GrainFactory.GetGrain<IFooGrain>(0).MakeGrainCall();
         }, CancellationToken.None, TaskCreationOptions.None, scheduler: orleansTs);

         int res = await (await t2); // double await, unrelated to Orleans, just part of TPL APIs.
         // This code runs back on the thread pool scheduler, not on the Orleans task scheduler
         Assert.AreNotEqual(orleansTS, TaskScheduler.Current);
         return res;
    } );

    int result = await t1;
    // We are back to the Orleans task scheduler.
    // Since await was executed in the Orleans task scheduler context, we are now back to that context.
    Assert.AreEqual(orleansTS, TaskScheduler.Current);
}
```

## [Grain 调用过滤器](http://dotnet.github.io/orleans/Documentation/grains/interceptors.html)
使用场景：
- 认证
- 日志记录
- 异常处理

包括 `IIncomingGrainCallFilter` 和 `IOutgoingGrainCallFilter`，分别在接收调用和调用结束后被调用。

可以被注册成 Silo 级和 Grain 级的过滤器。

## [无状态的工作者 Grain](http://dotnet.github.io/orleans/Documentation/grains/stateless_worker_grains.html)
在默认情况下，奥尔良运行时在集群中只创建一个谷物的激活。

可以通过[StatelessWorker]特性标记一个无状态工作者谷物，特点：
1. 奥尔良运行时将在集群的不同 silo 中创建无状态工人谷物的多个激活。
2. 对无状态工作者谷物的请求总是在本地执行，即在请求产生的同一个谷仓上，要么是在谷仓上运行的谷物，要么是由谷仓的客户端网关接收的。因此，从其他谷物或客户端网关调用无状态工人谷物，不会产生远程消息。
3. 如果已经存在的激活处于繁忙状态，奥尔良运行时自动地创建无状态工人谷物的额外激活。运行时在每个竖井中创建的无状态工作者谷物的最大激活数量在默认情况下是由机器上的CPU核心数量所限制的，除非由可选的maxlocalworker参数明确指定。
4. 由于2和3，无状态的工人谷物激活不是单独可寻址的。对无状态工人谷物的两个后续请求可以通过不同的激活方式处理。
	
打印出内存占用：
	Console.WriteLine("Total memory: {0:###,###,###,##0} bytes", GC.GetTotalMemory(true));	

## 为 Grain 指定周期性行为
### Timer
通过 `Grain.RegisterTimer` 方法注册计时器 。将返回一个 ` IDisposable` 对象，可通过调用该对象的 `Dispose`方法取消计时器。
```
public IDisposable RegisterTimer(
       Func<object, Task> asyncCallback, // function invoked when the timer ticks
       object state,                     // object tp pass to asyncCallback
       TimeSpan dueTime,                 // time to wait before the first timer tick
       TimeSpan period)                  // the period of the timer
```

### [Reminder](http://dotnet.github.io/orleans/Documentation/grains/timers_and_reminders.html#reminder-usage)
Reminder 和 Timer 的不同：
- 当提醒触发时，会重新激活 Grain。
- 提醒不应该用于高频计时器，它们的周期应该以分钟、小时或天为单位度量。

## [Observers](http://dotnet.github.io/orleans/Documentation/grains/observers.html)
用于订阅消息。

## [Stream](http://dotnet.github.io/orleans/Documentation/streaming/streams_quick_start.html)
用于订阅和发布通知。


### [可重入 Grain](http://dotnet.github.io/orleans/Documentation/grains/reentrancy.html)
默认情况，Grain 激活是单线程的。
- 通过在 Grain 上添加 `[Reentrant]` 标记，表示 Grain 是可重入的，可在不同请求间自由交叉执行。
- 通过在方法上添加 `[AlwaysInterleave] `标记，表示该方法总是可交叉运行。
- 调用链的可重入性默认是启用的，可以通过  `siloHostBuilder.Configure<SchedulingOptions>(options => options.AllowCallChainReentrancy = false);` 禁用。

## 实现 silo 启动时，自动执行任务
### 方法一：注册委托
```
siloHostBuilder.AddStartupTask(
  async (IServiceProvider services, CancellationToken cancellation) =>
  {
    // Use the service provider to get the grain factory.
    var grainFactory = services.GetRequiredService<IGrainFactory>();

    // Get a reference to a grain and call a method on it.
    var grain = grainFactory.GetGrain<IMyGrain>(0);
    await grain.Initialize();
});
````
### 方法二：注册 `IStartupTask` 的实现
```
public class CallGrainStartupTask : IStartupTask
{
    private readonly IGrainFactory grainFactory;

    public CallGrainStartupTask(IGrainFactory grainFactory)
    {
        this.grainFactory = grainFactory;
    }

    public async Task Execute(CancellationToken cancellationToken)
    {
        var grain = this.grainFactory.GetGrain<IMyGrain>(0);
        await grain.Initialize();
    }
}
```
```
siloHostBuilder.AddStartupTask<CallGrainStartupTask>();
```

## 优雅的关闭 silo
[Graceful shutdown - Console app](http://dotnet.github.io/orleans/Documentation/clusters_and_clients/configuration_guide/shutting_down_orleans.html)

## [单元测试](http://dotnet.github.io/orleans/Documentation/implementation/testing.html)

## 参考资料	
- [orleans - github](https://github.com/dotnet/orleans)
- [orleans 官网](http://dotnet.github.io/orleans/)	
- [Orleans测试Demo - github](https://github.com/tauruscch/Orleans-Demos)
