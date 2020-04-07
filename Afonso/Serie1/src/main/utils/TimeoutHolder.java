package utils;

import java.util.concurrent.TimeUnit;

public class TimeoutHolder {
    private final long deadline;		// -1L when timeout is infinite

    public TimeoutHolder(long millis) {
        deadline = millis >= 0L ? System.currentTimeMillis() + millis: -1L;
    }

    public TimeoutHolder(long time, TimeUnit unit) {
        deadline = time >= 0L ? System.currentTimeMillis() + unit.toMillis(time) : -1L;
    }

    // returns true if timeout exists
    public boolean isTimed() { return deadline >= 0L; }

    public long value() {
        if (deadline == -1L)
            return Long.MAX_VALUE;	// ensure that timeout does not expire!
        long remainder = deadline - System.currentTimeMillis();
        return Math.max(remainder, 0L);
    }
}
