import java.util.Optional;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Supplier;

public class SafeBoundedLazy<E> {
    // Configuration arguments
    private final Supplier<E> supplier;
    private final int lives;

    /**
     * The possible states:
     * null: means UNCREATED
     * CREATING and ERROR: mean exactly that
     * != null && != ERROR && != CREATING: means CREATED
     */

    private final ValueHolder<E> ERROR = new ValueHolder<>();
    private final ValueHolder<E> CREATING = new ValueHolder<>();
    // When the synchronizer is in ERROR state, the exception is hold here
    volatile Throwable errorException;
    // The current state
    private AtomicReference<ValueHolder<E>> state = new AtomicReference<>(null);

    // Construct a BoundedLazy
    public SafeBoundedLazy(Supplier<E> supplier, int lives) {
        if (lives < 1) {
            throw new IllegalArgumentException();
        }
        this.supplier = supplier;
        this.lives = lives;
    }

    // Returns an instance of the underlying type

    public Optional<E> get() throws Throwable {
        while (true) {
            ValueHolder<E> currentState = state.get();
            if (currentState == ERROR) {
                throw errorException;
            }
            if (currentState == null) {
                state.compareAndSet(currentState, CREATING);
                try {
                    E value = supplier.get();
                    state.set(lives > 1 ? new ValueHolder<>(value, lives - 1) : null);
                    return Optional.of(value);

                } catch (Throwable ex) {
                    errorException = ex;
                    state.set(ERROR);
                    throw ex;
                }
            } else if (currentState == CREATING) {
                do {
                    Thread.yield();
                } while (state.get() == CREATING); // spin until state != CREATING
            } else { // state is CREATED: we have at least one life
                E currentValue = currentState.value;
                int currentLives = currentState.availableLives;

                if (!state.compareAndSet(currentState, (--currentLives) == 0 ? null :
                        new ValueHolder<>(currentValue, currentLives - 1))) {
                    continue;
                }

                return Optional.of(currentValue);
            }
        }
    }

    private static class ValueHolder<V> {

        V value;
        int availableLives;

        ValueHolder(V value, int lives) {
            this.value = value;
            availableLives = lives;
        }

        ValueHolder() {
        }

    }

    /**
     * DEBUG ONLY
     */

    static void createCurrentThreadLog(String message) {
        System.out.println(String.format("Thread(%d): %s",
                                         Thread.currentThread().getId(),
                                         message));
    }

    String getCurrentState() {
        ValueHolder<E> currentState = state.get();
        return currentState == CREATING ? "CREATING" :
                currentState == ERROR ? "ERROR" :
                        currentState == null ? "UNCREATED" :
                                "CREATED";
    }
}

