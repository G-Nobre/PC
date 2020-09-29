namespace Ex4 {
    public class Request<TE> {
        private TE _message;
        private bool _done;

        public TE Message { get; set; }

        public bool IsBusy => !_done;

        public bool Done { get; set; }
    }
}