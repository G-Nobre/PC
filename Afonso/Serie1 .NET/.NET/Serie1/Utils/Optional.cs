using System;

namespace Utils {
    public class Optional<TE> {
        private TE _value;


        private static readonly TE EMPTY = default;

        public static Optional<TE> Empty() => Of(EMPTY);

        private Optional() { }

        public static Optional<TE> Of(TE value) => new Optional<TE> {Value = value};

        public bool IsEmpty => typeof(TE).IsPrimitive ? _value.Equals(default(TE)) : ReferenceEquals(_value, default);

        public bool IsPresent => !IsEmpty;

        public TE Value {
            get => _value;
            private set => _value = value;
        }
    }
}