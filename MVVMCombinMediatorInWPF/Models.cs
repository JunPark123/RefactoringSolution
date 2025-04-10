using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MVVMCombinMediatorInWPF
{
    // 간단한 Model: 숫자 증가/감소 로직
    public class Counter
    {
        public int Value { get; private set; }

        public void Increment() => Value++;
        public void Decrement() => Value--;
    }
}
