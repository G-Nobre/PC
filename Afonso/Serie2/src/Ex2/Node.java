package Ex2;

import java.util.concurrent.atomic.AtomicReference;

class Node<V> {
    final AtomicReference<Node<V>> next;
    final V data;

    Node(V data) {
        next = new AtomicReference<Node<V>>(null);
        this.data = data;
    }
}

