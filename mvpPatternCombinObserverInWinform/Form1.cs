using System.Windows.Forms;

namespace mvpPatternCombinObserverInWinform
{
    public partial class Form1 : Form,IMainView
    {
        enum eDesign { Mediator,Observer}
        // [MVP ����] View ����ü (WinForms Form)
        private MainPresenter _presenter;

        public Form1()
        {
            InitializeComponent();
        }

        private async void MainForm_Load(object sender, EventArgs e)
        {
            // Model, Presenter ����
            var model = new ChannelManager(channelCount: 5);
            _presenter = new MainPresenter(this, model);

            // UI �ʱ� ǥ��
            for (int i = 1; i <= 5; i++)
            {
                listBox1.Items.Add($"CH{i} ����: {model.GetState(i)}");
            }

            // ���� ���� ���� �ùķ��̼�
            // ���� Task�� ���� ���� ��ȯ
            await model.RandomStateChange();
        }

        // IMainView ����
        public void UpdateChannelState(int channelId, string state)
        {
            // UI �����忡�� �����ϰ� ����
            if (InvokeRequired)
            {
                Invoke(new Action(() => UpdateChannelState(channelId, state)));
                return;
            }

            // listBox1���� �ش� ä�� �� ���� (���� ����)
            int index = channelId - 1;
            if (index < listBox1.Items.Count)
            {
                listBox1.Items[index] = $"CH{channelId} ����: {state}";
            }
        }

        private void btnChangeState_Click(object sender, EventArgs e)
        {
            // ��: ��ư���� CH1�� Running���� ��ȯ
            _presenter.ChangeChannelState(1, "Running");
        }
    }


    #region Model

    // Model: ä���� ���¸� ���� + ���� ���� �� �̺�Ʈ �˸� (Observer)
    public class ChannelManager
    {
        // ä�� ���� ����(���� ����)
        private Dictionary<int, string> channelStates = new Dictionary<int, string>();

        // [Observer ����] ä�� ���°� �ٲ� �� �˸��� �̺�Ʈ
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
                // ���� ������ ��� ������(Presenter)���� �˸�
                ChannelStateChanged?.Invoke(channelId, newState);
            }
        }

        public string GetState(int channelId)
        {
            return channelStates.ContainsKey(channelId) ? channelStates[channelId] : "Unknown";
        }

        // �׽�Ʈ��: ���Ƿ� ���¸� �ٲٴ� �Լ�
        public async Task RandomStateChange()
        {
            var rnd = new Random();
            while (true)
            {
                // ���� ä��, ���� ����
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
    // Presenter: View�� Model ������ �߰��� (MVP)
    public class MainPresenter
    {
        private readonly IMainView _view;
        private readonly ChannelManager _model;

        public MainPresenter(IMainView view, ChannelManager model)
        {
            _view = view;
            _model = model;

            // [Observer ����] Model�� �̺�Ʈ�� Presenter�� ����
            _model.ChannelStateChanged += OnChannelStateChanged;
        }

        private void OnChannelStateChanged(int channelId, string state)
        {
            // �̺�Ʈ �߻� ��, View�� �����Ͽ� UI ����
            _view.UpdateChannelState(channelId, state);
        }

        // �ʿ� �� View���� ȣ���� �� �ִ� �޼���
        public void ChangeChannelState(int channelId, string newState)
        {
            _model.SetState(channelId, newState);
        }
    }

    #endregion

}
