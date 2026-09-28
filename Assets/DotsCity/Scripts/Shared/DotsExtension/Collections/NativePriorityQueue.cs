using System;
using Unity.Collections;

namespace Spirit604.Extensions
{
    public struct NativePriorityQueue<T> : IDisposable where T : unmanaged, IEquatable<T>
    {
        private NativeArray<T> _elements;
        private NativeArray<float> _fScores;
        private int _size;

        public NativePriorityQueue(int capacity, Allocator allocator)
        {
            _elements = new NativeArray<T>(capacity, allocator);
            _fScores = new NativeArray<float>(capacity, allocator);
            _size = 0;
        }

        public void Enqueue(T element, float fScore)
        {
            _elements[_size] = element;
            _fScores[_size] = fScore;
            _size++;
            HeapifyUp(_size - 1);
        }

        public void UpdateFScore(T element, float newFScore)
        {
            for (int i = 0; i < _size; i++)
            {
                if (_elements[i].Equals(element))
                {
                    _fScores[i] = newFScore;
                    HeapifyUp(i);
                    HeapifyDown(i);
                    break;
                }
            }
        }

        public T Dequeue()
        {
            var element = _elements[0];
            _elements[0] = _elements[_size - 1];
            _fScores[0] = _fScores[_size - 1];
            _size--;
            HeapifyDown(0);
            return element;
        }

        public bool IsEmpty => _size == 0;

        public bool Contains(T element)
        {
            for (int i = 0; i < _size; i++)
            {
                if (_elements[i].Equals(element))
                {
                    return true;
                }
            }
            return false;
        }

        private void HeapifyUp(int index)
        {
            while (index > 0)
            {
                int parentIndex = (index - 1) / 2;
                if (_fScores[index] >= _fScores[parentIndex])
                    break;

                Swap(index, parentIndex);
                index = parentIndex;
            }
        }

        private void HeapifyDown(int index)
        {
            while (index * 2 + 1 < _size)
            {
                int leftChildIndex = index * 2 + 1;
                int rightChildIndex = index * 2 + 2;
                int smallestChildIndex = leftChildIndex;

                if (rightChildIndex < _size && _fScores[rightChildIndex] < _fScores[leftChildIndex])
                {
                    smallestChildIndex = rightChildIndex;
                }

                if (_fScores[index] <= _fScores[smallestChildIndex])
                    break;

                Swap(index, smallestChildIndex);
                index = smallestChildIndex;
            }
        }

        private void Swap(int indexA, int indexB)
        {
            T tempElement = _elements[indexA];
            _elements[indexA] = _elements[indexB];
            _elements[indexB] = tempElement;

            float tempFScore = _fScores[indexA];
            _fScores[indexA] = _fScores[indexB];
            _fScores[indexB] = tempFScore;
        }

        public void Dispose()
        {
            _elements.Dispose();
            _fScores.Dispose();
        }

        //[BurstDiscard]
        public void Clear()
        {
            for (int i = 0; i < _size; i++)
            {
                _fScores[i] = 0;
            }

            for (int i = 0; i < _size; i++)
            {
                _elements[i] = default(T);
            }

            _size = 0;
        }
    }
}