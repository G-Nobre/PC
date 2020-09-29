import org.junit.jupiter.api.Test;

import java.util.HashSet;
import java.util.Optional;
import java.util.Set;
import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;
import java.util.function.Supplier;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertTrue;

public class SafeBoundedLazyTests {
    private final int NR_OF_THREADS = 5;
    Thread[] threads = new Thread[NR_OF_THREADS];

    private final Supplier<Long> supplier = () -> Thread.currentThread().getId();

    @Test
    void shouldReturnValueAlreadyCreated() {
        int lives = 3;
        SafeBoundedLazy<Long> boundedLazy = new SafeBoundedLazy<>(supplier, lives);
        Set<Long> values = new HashSet<>();
        createThreads(2, () -> {
            try {
                values.add(boundedLazy.get().get());
            } catch (Throwable ignored) { }
        });

        threads[0].start();
        threads[1].start();
        try {
            threads[0].join();
            threads[1].join();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        assertEquals(1,values.size());
        clearThreadArray();
    }

    @Test
    void shouldExceedNumberOfLivesAndComputeValueAgain() {
        SafeBoundedLazy<Long> safeBoundedLazy = new SafeBoundedLazy<>(supplier, 2);
        /**
         * Set doesn't allow duplicates, therefore threads who find the value already created
         * won't be accounted for. Hence, since with 2 lives and 3 threads there should be 3 values,
         * one of them will be duplicate from the first thread to calculate. Thus, set should be of
         * size = 2.
         */
        Set<Long> values = new HashSet<>();
        createThreads(3, () -> {
            try {
                values.add(safeBoundedLazy.get().get());
            } catch (Throwable ignored) {
            }
        });

        System.out.println(String.format("Thread[0] id = %d",threads[0].getId()));
        System.out.println(String.format("Thread[1] id = %d",threads[1].getId()));
        System.out.println(String.format("Thread[2] id = %d\n",threads[2].getId()));

        for (int i = 0; i < 3; i++) {
            threads[i].start();

        }

        System.out.println("All threads started\n");

        for (int i = 0; i < 3; i++) {
            try {
                threads[i].join();
            } catch (InterruptedException ignored) {
            }
        }
        System.out.println(values.size());
        assertEquals(2, values.size());
        clearThreadArray();
    }

    private void createThreads(int nr, Runnable runnable) {
        for (int i = 0; i < nr; i++) {
            threads[i] = new Thread(runnable);
            threads[i].setPriority(Thread.MAX_PRIORITY);
        }
    }

    private void clearThreadArray() {
        threads = new Thread[NR_OF_THREADS];
    }
}
