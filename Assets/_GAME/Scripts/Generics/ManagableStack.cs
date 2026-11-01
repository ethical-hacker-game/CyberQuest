using System;
using System.Collections;
using System.Collections.Generic;

public class ManagableStack<T>
{
    private List<T> items = new List<T>();

    public void Push(T item)
    {
        items.Add(item);
    }

    public T Pop()
    {
        if (items.Count > 0)
        {
            T temp = items[items.Count - 1];
            items.RemoveAt(items.Count - 1);
            return temp;
        }

        throw new InvalidOperationException();
    }

    public bool TryPop(out T item)
    {
        try
        {
            item = Pop();
            return true;
        }
        catch
        {
            item = default(T);
            return false;
        }
    }

    public T Peek()
    {
        if (items.Count > 0)
        {
            T temp = items[items.Count - 1];
            return temp;
        }

        throw new InvalidOperationException();
    }

    public bool TryPeek(out T item)
    {
        try
        {
            item = Peek();
            return true;
        }
        catch
        {
            item = default(T);
            return false;
        }
    }

    public void RemoveElement(T item)
    {
        items.Remove(item);
    }

    public void RemoveElementAt(int index)
    {
        items.RemoveAt(index);
    }

    public bool Contains(T item)
    {
        return items.Contains(item);
    }
}
