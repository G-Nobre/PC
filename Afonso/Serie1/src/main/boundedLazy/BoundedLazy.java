package main.boundedLazy;

import utils.TimeoutHolder;

import java.util.Optional;
import java.util.function.Supplier;

public class BoundedLazy<E> {

    private final Object monitor;
    private final Supplier<E> supplier;
    private final int lives;

    private int currentLives = 0;
    private E value = null;
    private Exception exceptionOccurred = null;
    private State state;

    public BoundedLazy(Supplier<E> supplier, int lives) {
        monitor = new Object();
        this.supplier = supplier;

        if (lives < 0) {
            throw new IllegalArgumentException();
        }

        this.lives = lives;
        currentLives = this.lives;
        state = State.UNCOMPUTED;
    }

    public Optional<E> get(long timeout) throws Exception {
        synchronized (monitor) {
            /*
             * If exception has occurred, then every calls to get result
             * in that same exception getting thrown again
             */
            if (exceptionOccurred != null) {
                throw exceptionOccurred;
            }
            /*
             * If the value is already computed and the number of lives is greater than 0,
             * then the value is returned to caller right away.
             */
            if (currentLives > 0 && state == State.COMPUTED) {
                --currentLives;
                return Optional.of(value);
            }
            /*
              In any other case, either the value needs to be recalculated, or is already being
              computed by another thread. The current thread calculate the value, or wait,
              if the latter is the case.
             */
            if (state == State.COMPUTING) {
                TimeoutHolder timeoutHolder = new TimeoutHolder(timeout);
                do {
                    try {
                        if (timeoutHolder.isTimed()) {
                            if ((timeout = timeoutHolder.value()) < 0L) {
                                return Optional.empty();
                            }
                            monitor.wait(timeout);
                        } else {
                            monitor.wait();
                        }
                        if (state == State.COMPUTED && currentLives > 0) {
                            return decrementCheckAndReturn();
                        }
                    } catch (InterruptedException e) {
                        if (state == State.COMPUTED) {
                            Thread.currentThread().interrupt();
                            return decrementCheckAndReturn();
                        } else if (currentLives == 0 || state == State.UNCOMPUTED) {
                            throw e;
                        }
                    }
                } while (state == State.COMPUTING);
            }
            state = State.COMPUTING;
        }
        /*
          The current thread is going to calculate the new value, by invoking the supplier.
         */
        E v = null;
        try {
            v = supplier.get();
            currentLives = lives;
        } catch (Exception e) {
            exceptionOccurred = e;
        }
        /*
           In the end, if an unchecked exception has occurred, it shall be thrown, and State state
           shall be put to ERROR. Else, the value field is updated and returned.
         */
        synchronized (monitor) {
            monitor.notifyAll();
            if (exceptionOccurred != null) {
                state = State.ERROR;
                throw exceptionOccurred;
            } else {
                value = v;
                state = State.COMPUTED;
                return decrementCheckAndReturn();
            }
        }
    }

    private Optional<E> decrementCheckAndReturn(){
        --currentLives;
        if (currentLives == 0) {
            state = State.UNCOMPUTED;
        }
        return Optional.of(value);
    }

}
