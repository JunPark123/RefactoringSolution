using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVMCombinMediatorInWPF
{
    // [Mediator 패턴] 중재자 인터페이스
    public interface IMediator
    {
        void Notify(object sender, string message, object data = null);
    }
    
    // [옵션] ViewModel들이 구현할 수 있는 인터페이스
    // Mediator에 등록하기 위한 역할
    public interface IColleague
    {
        IMediator Mediator { get; set; }
    }

    // [Mediator 패턴] 실제 중재자 구현
    public class ConcreteMediator : IMediator
    {
        private List<IColleague> colleagues = new List<IColleague>();

        public void Register(IColleague colleague)
        {
            if (!colleagues.Contains(colleague))
                colleagues.Add(colleague);
        }

        // ViewModel에서 Notify하면, Mediator가 다른 ViewModel에 전달
        public void Notify(object sender, string message, object data = null)
        {
            // 예: message == "Increment" → 다른 ViewModel에 알림
            // 또는 Sender를 제외한 나머지에게 알림 등 로직 가능
            foreach (var col in colleagues)
            {
                if (col != sender) // 자기 자신 제외 (원하면 수정 가능)
                {
                    // Colleague에 캐스팅 후 특정 메서드 호출 or 이벤트 발생
                    // 예: ViewModel에 OnMediatorNotified(...) 같은 메서드
                    if (col is IColleagueReceiver receiver)
                    {
                        receiver.OnMediatorNotified(message, data);
                    }
                }
            }
        }
    }

    // 추가 인터페이스: Mediator 메시지를 수신하는 쪽
    public interface IColleagueReceiver
    {
        void OnMediatorNotified(string message, object data);
    }
}
