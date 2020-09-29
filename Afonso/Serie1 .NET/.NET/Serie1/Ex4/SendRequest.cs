namespace Ex4 {
    public class SendRequest<TE> {
        public SendRequest(TE message, bool isBlocking) {
            Message = message;
            IsBlocking = isBlocking;
        }

        public TE Message { get; }

        public bool IsBlocking { get; }

        public bool Done { get; set; }
    }
}