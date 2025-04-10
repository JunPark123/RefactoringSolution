using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;


namespace MVVMCombinMediatorInWPF
{
    public class MainViewModel : ViewModelBase
    {
        public FirstViewModel Vm1 { get; }
        public SecondViewModel Vm2 { get; }

        public MainViewModel()
        {
            var mediator = new ConcreteMediator();

            // 두 ViewModel 생성
            Vm1 = new FirstViewModel();
            Vm2 = new SecondViewModel();

            // Mediator 등록
            mediator.Register(Vm1);
            mediator.Register(Vm2);

            // 각 VM에 Mediator 주입
            Vm1.Mediator = mediator;
            Vm2.Mediator = mediator;
        }
    }

    public class FirstViewModel : ViewModelBase, IColleague, IColleagueReceiver
    {
        private Counter _counter = new Counter();

        public IMediator Mediator { get; set; }

        private int _number;
        public int Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(); }
        }

        // 커맨드: Increment 버튼
        public ICommand IncrementCommand { get; }

        public FirstViewModel()
        {
            // IncrementCommand 실행 시, Counter 값 증가 → Mediator에 알림
            IncrementCommand = new RelayCommand(_ =>
            {
                _counter.Increment();
                Number = _counter.Value;
                // Mediator 패턴 활용: 다른 ViewModel 알리기
                Mediator?.Notify(this, "Incremented", _counter.Value);
            });
        }

        // Mediator가 전송한 메시지 수신
        public void OnMediatorNotified(string message, object data)
        {
            if (message == "Decremented" && data is int val)
            {
                // 다른 ViewModel(SecondViewModel)에서 Decrement 한 결과
                // 이 ViewModel도 UI 갱신 가능
                Number = val;
            }
        }
    }

    public class SecondViewModel : ViewModelBase, IColleague, IColleagueReceiver
    {
        private Counter _counter = new Counter();
        public IMediator Mediator { get; set; }

        private int _number;
        public int Number
        {
            get => _number;
            set { _number = value; OnPropertyChanged(); }
        }

        public ICommand DecrementCommand { get; }

        public SecondViewModel()
        {
            DecrementCommand = new RelayCommand(_ =>
            {
                _counter.Decrement();
                Number = _counter.Value;
                Mediator?.Notify(this, "Decremented", _counter.Value);
            });
        }

        public void OnMediatorNotified(string message, object data)
        {
            if (message == "Incremented" && data is int val)
            {
                Number = val;
            }
        }
    }
}
