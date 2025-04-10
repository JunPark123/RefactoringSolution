using System.Windows.Forms;

namespace mvpPatternCombinObserverInWinform
{
    public partial class Form1 : Form,IMainView
    {
        enum eDesign { Mediator,Observer}
        // [MVP 패턴] View 구현체 (WinForms Form)
        private MainPresenter _presenter;

        public Form1()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Model, Presenter 생성
            var model = new ChannelManager(channelCount: 5);
            _presenter = new MainPresenter(this, model);

            // UI 초기 표시
            for (int i = 1; i <= 5; i++)
            {
                listBox1.Items.Add($"CH{i} 상태: {model.GetState(i)}");
            }

            // 임의 상태 변경 시뮬레이션
            // 별도 Task로 랜덤 상태 전환
            await model.RandomStateChange();
        }

        // IMainView 구현
        public void UpdateChannelState(int channelId, string state)
        {
            // UI 스레드에서 안전하게 갱신
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateChannelState(channelId, state)));
                return;
            }

            // listBox1에서 해당 채널 줄 갱신 (간단 예시)
            int index = channelId - 1;
            if (index < listBox1.Items.Count)
            {
                listBox1.Items[index] = $"CH{channelId} 상태: {state}";
            }
        }

        private void btnChangeState_Click(object sender, EventArgs e)
        {
            // 예: 버튼으로 CH1을 Running으로 전환
            _presenter.ChangeChannelState(1, "Running");
        }
    }


    #region Model

    // Model: 채널의 상태를 관리 + 상태 변경 시 이벤트 알림 (Observer)
    public class ChannelManager
    {
        // 채널 상태 정보(간단 예시)
        private Dictionary<int, string> channelStates = new Dictionary<int, string>();

        // [Observer 패턴] 채널 상태가 바뀔 때 알리는 이벤트
        public event Action<int, string> ChannelStateChanged;

        public ChannelManager(int channelCount)
        {
            for (int i = 1; i <= channelCount; i++)
            {
                channelStates[i] = "Idle";
            }
        }

        public void SetState(int channelId, string newState)
        {
            if (channelStates.ContainsKey(channelId))
            {
                channelStates[channelId] = newState;
                // 상태 변경을 모든 구독자(Presenter)에게 알림
                ChannelStateChanged?.Invoke(channelId, newState);
            }
        }

        public string GetState(int channelId)
        {
            return channelStates.ContainsKey(channelId) ? channelStates[channelId] : "Unknown";
        }

        // 테스트용: 임의로 상태를 바꾸는 함수
        public async Task RandomStateChange()
        {
            var rnd = new Random();
            while (true)
            {
                // 랜덤 채널, 랜덤 상태
                int ch = rnd.Next(1, channelStates.Count + 1);
                string[] possibleStates = { "Idle", "Ready", "Running", "Finish", "Error" };
                string newState = possibleStates[rnd.Next(possibleStates.Length)];

                SetState(ch, newState);
                await Task.Delay(1000);
            }
        }
    }
    #endregion

    #region Presenter
    // Presenter: View와 Model 사이의 중간자 (MVP)
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly ChannelManager _model;

        public MainPresenter(IMainView view, ChannelManager model)
        {
            _view = view;
            _model = model;

            // [Observer 패턴] Model의 이벤트를 Presenter가 구독
            _model.ChannelStateChanged += OnChannelStateChanged;
        }

        private void OnChannelStateChanged(int channelId, string state)
        {
            // 이벤트 발생 시, View에 전달하여 UI 갱신
            _view.UpdateChannelState(channelId, state);
        }

        // 필요 시 View에서 호출할 수 있는 메서드
        public void ChangeChannelState(int channelId, string newState)
        {
            _model.SetState(channelId, newState);
        }
    }

    #endregion

}
