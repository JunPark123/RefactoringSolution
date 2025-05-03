using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace ConsoleApp1
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var services = new ServiceCollection();

            // 1) 장비 I/O
            services.AddSingleton<IDmmPort, FakeDmm>()
                    .AddSingleton<IDaqPort, FakeDaq>();

            // 2) Job Manager
            services.AddSingleton<IJobManager, AutoCalJobManager>();

            // 3) MachineContext ‼️ 빠졌던 부분
            services.AddSingleton<MachineContext>();

            // 4) MachineController
            services.AddSingleton<MachineController>();

            // 5) 로거
            services.AddLogging(b =>
                b.AddSimpleConsole(o => o.UseUtcTimestamp = true));

            using var sp = services.BuildServiceProvider();

            var controller = sp.GetRequiredService<MachineController>();
            using var cts = new CancellationTokenSource();

            Console.CancelKeyPress += (_, e) =>
            {
                e.Cancel = true;
                cts.Cancel();
            };

            await controller.RunAsync(cts.Token);
        }
    }

    // ─── 도메인 열거형 ────────────────────────────────────────────────────────────
    enum SystemMode { Auto, Manual }
    enum SystemState { Idle, Ready, Running, Finish, Error }

    enum SensorType { Voltage, NTC }      // 간단화
    enum CalStep { Ready, Measure, Write, Completed, Error }

    // ─── 전략 I/O 인터페이스 & Mock 구현 ────────────────────────────────────────
    interface IDmmPort { Task<double> ReadAsync(SensorType type, CancellationToken ct); }
    interface IDaqPort { Task WriteAsync(int board, int ch, double v, CancellationToken ct); }

    class FakeDmm : IDmmPort
    {
        readonly Random _rnd = new();
        public Task<double> ReadAsync(SensorType _, CancellationToken __)
            => Task.FromResult(Math.Round(_rnd.NextDouble() * 5 + 4, 3));   // 4~9 V
    }
    class FakeDaq : IDaqPort
    {
        public Task WriteAsync(int board, int ch, double v, CancellationToken ct)
        {
            Console.WriteLine($"▶ DAQ[{board}:{ch}] ← {v:F3} V");
            return Task.CompletedTask;
        }
    }

    // ─── Measurement / Sequence / Job 엔티티 ────────────────────────────────────
    record CalibrationSequence(int Board, int Channel, SensorType Type)
    {
        public CalStep Step { get; set; } = CalStep.Ready;
        public double Dmm { get; set; }
        public double Daq { get; set; }
    }

    class CalibrationJob
    {
        public string Name { get; }
        public List<CalibrationSequence> Sequences { get; }
        public CalibrationJob(string name, IEnumerable<CalibrationSequence> seq)
            => (Name, Sequences) = (name, seq.ToList());
    }


    // ─── Job Manager 인터페이스 & 구현 ──────────────────────────────────────────
    interface IJobManager
    {
        Task RunJobAsync(CalibrationJob job, CancellationToken ct);
    }

    class AutoCalJobManager : IJobManager
    {
        readonly IDmmPort _dmm; readonly IDaqPort _daq; readonly ILogger _log;
        public AutoCalJobManager(IDmmPort dmm, IDaqPort daq, ILogger<AutoCalJobManager> log)
            => (_dmm, _daq, _log) = (dmm, daq, log);

        public async Task RunJobAsync(CalibrationJob job, CancellationToken ct)
        {
            _log.LogInformation("Job {name} 시작, {cnt} step", job.Name, job.Sequences.Count);

            foreach (var seq in job.Sequences)
            {
                ct.ThrowIfCancellationRequested();
                _log.LogInformation("Step Board{b}/CH{ch}", seq.Board, seq.Channel);

                seq.Step = CalStep.Measure;
                seq.Dmm = await _dmm.ReadAsync(seq.Type, ct);

                seq.Step = CalStep.Write;
                await _daq.WriteAsync(seq.Board, seq.Channel, seq.Dmm, ct);

                seq.Daq = seq.Dmm;               // mock
                seq.Step = CalStep.Completed;
            }

            _log.LogInformation("Job {name} 완료", job.Name);
        }
    }
    // ─── 장비 상태(State) 패턴 ‑ 컨텍스트 & 개별 State ──────────────────────────
    class MachineContext
    {
        public SystemState State { get; set; } = SystemState.Idle;
        public SystemMode Mode { get; set; } = SystemMode.Auto;

        public readonly IJobManager JobMgr;
        readonly ILogger _log;

        // 간단 예제를 위해 Job 1개 미리 생성
        public readonly CalibrationJob Job = new(
            "Board1‑Volt", Enumerable.Range(1, 4)
                                    .Select(ch => new CalibrationSequence(1, ch, SensorType.Voltage)));

        public MachineContext(IJobManager jm, ILogger<MachineContext> log)
            => (JobMgr, _log) = (jm, log);

        public void To(SystemState s)
        {
            if (State != s) _log.LogInformation("State {old} → {new}", State, s);
            State = s;
        }
    }

    interface IState { Task HandleAsync(MachineContext ctx, CancellationToken ct); }

    // ── Idle
    class IdleState : IState
    {
        public Task HandleAsync(MachineContext ctx, CancellationToken ct)
        {
            if (ctx.Mode == SystemMode.Auto) ctx.To(SystemState.Ready);
            return Task.CompletedTask;
        }
    }

    // ── Ready
    class ReadyState : IState
    {
        public Task HandleAsync(MachineContext ctx, CancellationToken ct)
        {
            ctx.To(SystemState.Running);
            return Task.CompletedTask;
        }
    }

    // ── Running
    class RunningState : IState
    {
        public async Task HandleAsync(MachineContext ctx, CancellationToken ct)
        {
            try
            {
                await ctx.JobMgr.RunJobAsync(ctx.Job, ct);
                ctx.To(SystemState.Finish);
            }
            catch (OperationCanceledException)
            {
                ctx.To(SystemState.Error);
            }
        }
    }

    // ── Finish
    class FinishState : IState
    {
        public Task HandleAsync(MachineContext ctx, CancellationToken ct)
        {
            ctx.To(SystemState.Idle);
            return Task.CompletedTask;
        }
    }

    // ── Error
    class ErrorState : IState
    {
        public Task HandleAsync(MachineContext ctx, CancellationToken ct)
        {
            ctx.To(SystemState.Idle);
            return Task.CompletedTask;
        }
    }


    // ─── 상태 매핑 & 컨트롤러 루프 ──────────────────────────────────────────────
    class MachineController
    {
        readonly MachineContext _ctx;
        readonly Dictionary<SystemState, IState> _map;
        readonly ILogger _log;

        public MachineController(MachineContext ctx, ILogger<MachineController> log)
        {
            _ctx = ctx; _log = log;
            _map = new()
            {
                [SystemState.Idle] = new IdleState(),
                [SystemState.Ready] = new ReadyState(),
                [SystemState.Running] = new RunningState(),
                [SystemState.Finish] = new FinishState(),
                [SystemState.Error] = new ErrorState()
            };
        }

        public async Task RunAsync(CancellationToken ct)
        {
            _log.LogInformation("MachineController 시작 (Ctrl+C to exit)");

            try
            {
                while (!ct.IsCancellationRequested)
                {
                    await _map[_ctx.State].HandleAsync(_ctx, ct);
                    await Task.Delay(50, ct);               // <- 여기서 예외 발생
                }

                ct.ThrowIfCancellationRequested();          // 루프를 빠져나올 때도 안전
            }
            catch (OperationCanceledException)
            {
                _log.LogInformation("취소 요청 수신 ‑ 정상 종료");
            }
        }
    }
}
