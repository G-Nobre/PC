namespace Ex2 {
    public class RequestMessage<E> {
        private E message;
        private bool busy;

        public RequestMessage(bool busy) {
            this.busy = busy;
        }

        public E Message {
            get => message;
            set => message = value;
        }

        public bool Busy {
            get => busy;
            set => busy = value;
        }
    }
}