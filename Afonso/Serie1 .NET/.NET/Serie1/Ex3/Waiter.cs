using System;
using System.Threading;

namespace Ex3 {
    public class Waiter<T> {
        private T _myData;
        private bool _isDone;
        private object _lock;

        public T MyData {
            get => _myData;
            set => _myData = value;
        }

        public bool IsDone {
            get => _isDone;
            set => _isDone = value;
        }

        public Waiter(T myData, object @lock) {
            _myData = myData;
            _lock = @lock;
        }

        public T Swap(T mydata) {
            T aux = _myData;
            _myData = mydata;
            return aux;
        }

        public object Lock => _lock;
    }
}