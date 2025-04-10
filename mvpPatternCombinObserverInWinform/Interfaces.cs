using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace mvpPatternCombinObserverInWinform
{
    public interface IMainView
    {
        // 전체 채널 상태를 갱신하는 메서드
        void UpdateChannelState(int channelId, string state);
    }
}
