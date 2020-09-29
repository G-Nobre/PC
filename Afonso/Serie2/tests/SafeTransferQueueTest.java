import Ex2.SafeTransferQueue;
import org.junit.jupiter.api.Test;

import java.util.concurrent.atomic.AtomicInteger;
import java.util.concurrent.atomic.AtomicReference;

import static org.junit.jupiter.api.Assertions.assertEquals;
import static org.junit.jupiter.api.Assertions.assertNull;

public class SafeTransferQueueTest {
    @Test
    public void shouldPutAndTakeSuccessfully() throws InterruptedException {
        SafeTransferQueue<Integer> transferQueue = new SafeTransferQueue<>();
        int numberOfThreads = 2;
        long timeout = 50;
        Integer message = 35;
        Thread[] threads = new Thread[numberOfThreads];

        threads[0] = new Thread(() -> transferQueue.put(message));

        AtomicInteger result = new AtomicInteger();
        threads[1] = new Thread(() -> {
            try {
                Thread.sleep(10);
                result.set(transferQueue.take(timeout));
            } catch (InterruptedException e) {
                System.out.println("Error : take exception");
                e.printStackTrace();
            }
        });

        threads[0].start(); // put message
        Thread.sleep(1);
        threads[1].start(); //put message
        Thread.sleep(1);

        for (int i = 0; i < numberOfThreads; i++) {
            threads[i].join();
        }

        assertEquals(message, result.get());
    }

    @Test
    public void shouldTakeAndPutSuccessfully() {
        SafeTransferQueue<Integer> transferQueue = new SafeTransferQueue<>();
        int numberOfThreads = 2;
        long timeout = 50;
        Integer message = 35;
        Thread[] threads = new Thread[numberOfThreads];
        AtomicInteger result = new AtomicInteger();
        threads[0] = new Thread(() -> {
            try {
                result.set(transferQueue.take(200));
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        threads[1] = new Thread(() -> {
            transferQueue.put(message);
        });

        threads[0].start();
        threads[1].start();
        try {
            threads[0].join();
            threads[1].join();
        } catch (InterruptedException e) {
            e.printStackTrace();
        }

        assertEquals(message, result.get());
    }

    @Test
    public void shouldTimeoutAndReturnNullForTakeAndFalseForTransfer() throws InterruptedException {
        SafeTransferQueue<Integer> transferQueue = new SafeTransferQueue<>();
        int numberOfThreads = 3;
        long timeout = 10;
        Integer message = 35;
        Thread[] threads = new Thread[numberOfThreads];

        threads[0] = new Thread(() -> {
            try {
                Thread.sleep(100);
                transferQueue.put(message);
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        });

        AtomicReference<Integer> takeIntegerResult = new AtomicReference();
        threads[1] = new Thread(() -> {
            try {
                takeIntegerResult.set(transferQueue.take(timeout));
            } catch (InterruptedException e) {
                e.printStackTrace();
            }
        }
        );

        for (int i = 0; i < numberOfThreads - 1; i++) {
            threads[i].start();
        }

        for (int i = 0; i < numberOfThreads - 1; i++) {
            threads[i].join();
        }

        assertNull(takeIntegerResult.get());
    }
}
