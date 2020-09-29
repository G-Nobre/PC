package Ex2;

import java.util.concurrent.atomic.AtomicReference;

public class MichaelScottQueue<E> {
    //queue for requests
    final AtomicReference<Node<E>> head;
    final AtomicReference<Node<E>> tail;

    //queue for send requests


    public MichaelScottQueue() {
        Node<E> sentinel = new Node<>(null);
        head = new AtomicReference<>(sentinel);
        tail = new AtomicReference<>(sentinel);
    }

    public void tryEnqueue(E data) {
        Node<E> newNode = new Node<>(data);

        while (true) {
            Node<E> observedTail = tail.get();
            Node<E> observedTailNext = observedTail.next.get();
            if (observedTail == tail.get()) {
                // confirm that we have a good tail, to prevent CAS failures
                if (observedTailNext != null) {
                    // queue in intermediate state, so advance tail for some other thread
                    tail.compareAndSet(observedTail, observedTailNext);
                } else {
                    // queue in quiescent state, try inserting new node
                    if (observedTail.next.compareAndSet(null, newNode)) {
                        // advance the tail
                        tail.compareAndSet(observedTail, newNode);
                        return;
                    }
                }
            }
        }
    }

    public Node<E> tryDequeue() {
        while (true) {
            Node<E> observedHead = head.get();
            Node<E> newSentinel;
            Node<E> observedTail = tail.get();
            if (observedHead == head.get() && observedTail == tail.get()) {
                newSentinel = observedHead.next.get();
                if (newSentinel != null) {
                    if (observedHead == observedTail) { //intermediate
                        tail.compareAndSet(observedTail, newSentinel);
                    } else {
                        if (head.compareAndSet(observedHead, newSentinel)) {
                            return newSentinel;
                        }
                    }
                } else {
                    return null;
                }
            }
        }
    }

}
