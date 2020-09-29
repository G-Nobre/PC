package Ex2;

import java.util.concurrent.TimeUnit;
import java.util.concurrent.locks.Condition;
import java.util.concurrent.locks.ReentrantLock;

public class SafeTransferQueue<E> {

    private MichaelScottQueue<E> messagesQueue = new MichaelScottQueue<>();

    private ReentrantLock lock = new ReentrantLock();
    private Condition condition = lock.newCondition();

    private volatile int waiters;

    /**
     * Put will try to enqueue the message, and then, if there are waiting threads,
     * it will signal one of them
     */
    public void put(E message) {
        messagesQueue.tryEnqueue(message);
        if (waiters > 0) {
            lock.lock();
            try {
                if (waiters > 0) {
                    condition.signal();
                }
            } finally {
                lock.unlock();
            }
        }
    }

    public E take(long timeout) throws InterruptedException {
        Node<E> messageNode;

        if ((messageNode = messagesQueue.tryDequeue()) != null) return messageNode.data;

        boolean isTimed = timeout > 0;
        long nanosTimeout = isTimed ? TimeUnit.MILLISECONDS.toNanos(timeout) : 0L;
        lock.lock();
        try {
            waiters++;
            try {
                do {
                    if ((messageNode = messagesQueue.tryDequeue()) != null) return messageNode.data;

                    if (isTimed && nanosTimeout <= 0) {
                        return null;
                    }

                    if (isTimed) {
                        nanosTimeout = condition.awaitNanos(nanosTimeout);
                    } else {
                        condition.await();
                    }
                } while (true);
            } finally {
                --waiters;
            }
        } finally {
            lock.unlock();
        }
    }
}

