// ✅ 다중 채널 시퀀스를 관리하는 콘솔 프로그램 예제 (Task 기반, 독립 채널 병렬 실행)
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

// 상태 정의
enum SequenceStatus
{
    Idle,
    Ready,
    Running,
    Finish,
    Error
}

// 채널별 Context
class ChannelContext
{
    public int ChannelId { get; }
    public SequenceStatus Status { get; set; } = SequenceStatus.Idle;
    public bool IsRunning { get; set; } = false;
    public bool IsFinish { get; set; } = false;
    public bool IsError { get; set; } = false; 
    public bool IsActive { get; set; } = true;

    public ChannelContext(int id) => ChannelId = id;

    public string GetStateText() => $"[CH{ChannelId}] 상태: {Status}";
}

// 상태 실행 인터페이스
interface IState
{
    void Execute(ChannelContext context);
}

// 각 상태 클래스
class IdleState : IState
{
    public void Execute(ChannelContext context)
    {
        context.Status = SequenceStatus.Ready;
    }
}

class ReadyState : IState
{
    public void Execute(ChannelContext context)
    {
        context.Status = SequenceStatus.Running;
        context.IsRunning = true;
    }
}

class RunningState : IState
{
    public void Execute(ChannelContext context)
    {
        if (context.IsError)
        {
            context.Status = SequenceStatus.Error;
            return;
        }

        if (!context.IsFinish)
        {
            Thread.Sleep(300); // 시험 시간 시뮬레이션
            context.IsFinish = true;
        }
        else
        {
            context.Status = SequenceStatus.Finish;
        }
    }
}

class FinishState : IState
{
    public void Execute(ChannelContext context)
    {
        context.IsRunning = false;
        context.IsFinish = false;
        context.Status = SequenceStatus.Idle;
        
        // 종료 요청된 경우 비활성화
        if (!context.IsActive)
        {
            context.Status = SequenceStatus.Finish;
        }

    }
}

class ErrorState : IState
{
    public void Execute(ChannelContext context)
    {
        context.IsError = false;
        context.Status = SequenceStatus.Idle;
    }
}

// 시퀀스 실행 클래스
class SequenceManager
{
    private readonly Dictionary<SequenceStatus, IState> _stateMap;
    private readonly List<ChannelContext> _channels = new();
    private readonly List<Task> _tasks = new();
    private readonly Dictionary<int, CancellationTokenSource> _ctsMap = new();

    public SequenceManager(int channelCount)
    {
        _stateMap = new Dictionary<SequenceStatus, IState>
        {
            { SequenceStatus.Idle, new IdleState() },
            { SequenceStatus.Ready, new ReadyState() },
            { SequenceStatus.Running, new RunningState() },
            { SequenceStatus.Finish, new FinishState() },
            { SequenceStatus.Error, new ErrorState() }
        };

        for (int i = 1; i <= channelCount; i++)
        {
            _channels.Add(new ChannelContext(i));
        }
    }

    public void RunAllChannelsIndependently()
    {
        foreach (var ch in _channels)
        {
            var localCh = ch;
            var cts = new CancellationTokenSource();
            _ctsMap[localCh.ChannelId] = cts;

            var task = Task.Run(async () =>
            {
                while (!cts.Token.IsCancellationRequested)
                {
                    _stateMap[localCh.Status].Execute(localCh);
                    lock (Console.Out)
                    {
                        Console.SetCursorPosition(0, localCh.ChannelId - 1);
                        Console.WriteLine(localCh.GetStateText().PadRight(Console.WindowWidth));
                    }
                    await Task.Delay(200);
                    
                    // Finish 상태 + 비활성화 설정 시 루프 탈출
                    if (localCh.Status == SequenceStatus.Finish && !localCh.IsActive)
                        break;
                }

            });

            _tasks.Add(task);
        }
    }

    public async Task AllStopAsync()
    {
        foreach (var ch in _channels)
        {
            ch.IsFinish = true;
            ch.IsActive = false;
        }

        foreach (var cts in _ctsMap.Values)
            cts.Cancel();
        await Task.WhenAll(_tasks);
    }

    public async Task OneSeqStopAsync(int ch)
    {
        if (_ctsMap.TryGetValue(ch, out var cts))
        {
            var stopCh = _channels.FirstOrDefault(x => x.ChannelId == ch);
            if (stopCh != default)
            {
                stopCh.IsFinish = true;
                stopCh.IsActive = false;

                // Finish 상태 도달까지 기다림
                while (stopCh.Status != SequenceStatus.Finish)
                    await Task.Delay(50);

                // 다음 루프에서 break 타이밍을 줄 수 있도록 약간 대기 후 cancel
                await Task.Delay(100);
                cts.Cancel();
            }
        }
    }

}

// 프로그램 진입점
class Program
{
    static async Task Main(string[] args)
    {
        int channelcout = 10;
        Console.Clear();
        Console.WriteLine("다중 채널 시퀀스 컨트롤 시작 (Enter 종료)");

        var sequenceManager = new SequenceManager(channelcout);
        sequenceManager.RunAllChannelsIndependently();

        for (int i = 0; i < channelcout +1; i++)
        {
            await Task.Delay(1000); // 테스트용 잠시 대기 후 1번만 멈춤
            await sequenceManager.OneSeqStopAsync(i);
        }
    
        Console.ReadLine();
        await sequenceManager.AllStopAsync();
    }
}
