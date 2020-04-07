import main.boundedLazy.BoundedLazy;
import org.junit.jupiter.api.Test;

import java.util.Optional;
import java.util.function.Supplier;

import static org.junit.jupiter.api.Assertions.assertEquals;

class BoundedLazyTest {

    private long TIMEOUT = 2000L;

    private final int NR_OF_THREADS = 5;
    Thread[] threads = new Thread[NR_OF_THREADS];

    private final Supplier<Long> supplier = () -> Thread.currentThread().getId();

    @Test
    void shouldReturnValueAlreadyCreated() {
        int lives = 3;
        BoundedLazy<Long> boundedLazy = new BoundedLazy<>(supplier, lives);
        Optional[] value = {Optional.of(Long.MAX_VALUE)};

        createThreads(2, () -> {
            try {
                value[0] = boundedLazy.get(TIMEOUT);
            } catch (Exception ignored) {
            }
        });

        threads[0].start();
        try {
            threads[0].join();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        threads[1].start();
        try {
            threads[1].join();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        assertEquals(threads[0].getId(), (Long) value[0].get());
        clearThreadArray();
    }

    @Test
    void shouldExceedNumberOfLivesAndComputeValueAgain() {
        BoundedLazy<Long> boundedLazy = new BoundedLazy<>(supplier, 2);
        Optional[] values = {
                Optional.of(Long.MAX_VALUE),
                Optional.of(Long.MAX_VALUE),
                Optional.of(Long.MAX_VALUE)
        };
        int [] idx = {0};
        createThreads(3, () -> {
            try {
                values[idx[0]++] = boundedLazy.get(1000);
            } catch (Exception ignored) {
            }
        });

        for (int i = 0; i < 3; i++) {
            threads[i].start();
            try {
                threads[i].join();
            } catch (InterruptedException ignored) {
            }
        }

        assertEquals(threads[0].getId(), (Long) values[0].get());
        assertEquals(threads[0].getId(), (Long) values[1].get());
        assertEquals(threads[2].getId(), (Long) values[2].get());
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

    void soutThreadRunningTime(int threadIdx, long totalTime) {
        System.out.println("threads[" + threadIdx + "] total time running: " +
                                   (totalTime > 1000000000 ?
                                           (float) (totalTime / 1000000000) + " seconds" :
                                           totalTime > 1000000 ?
                                                   (float) (totalTime / 1000000) + " miliseconds" :
                                                   (totalTime / 1000) + " microsenconds"
                                   ));
    }
}