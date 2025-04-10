// ✅ 다중 채널 시퀀스를 관리하는 콘솔 프로그램 예제
using System;
using System.Collections.Generic;
using System.Threading;
/*
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

// 시퀀스 루프 실행 클래스
class SequenceManager
{
    private readonly Dictionary<SequenceStatus, IState> _stateMap;
    private readonly List<ChannelContext> _channels = new();
    private bool _isRunning = true;

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

    public void Run()
    {
        while (_isRunning)
        {
            foreach (var ch in _channels)
            {
                _stateMap[ch.Status].Execute(ch);
            }

            // 상태 출력 2줄만
            Console.Clear();
            foreach (var ch in _channels)
            {
                Console.WriteLine(ch.GetStateText());
            }

            Thread.Sleep(200); // 반복 간격
        }
    }

    public void Stop() => _isRunning = false;
}

// 프로그램 진입점

*/