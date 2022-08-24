// QueuedThread.cs
// Created by xiaojl Aug/24/2022
// FIFO的队列线程池

using System;
using System.Threading;

public class ActionNode
{
    public Action Action;
    public ActionNode Next;

    public ActionNode(Action action)
    {
        this.Action = action;
        this.Next = null;
    }
}

public class QueuedThread
{
    private readonly object _syncRoot = new object();

    private ActionNode _head;
    private ActionNode _tail;
    private Semaphore _semaphore;
    private Thread[] _threads;

    public QueuedThread(int maxThreadNum)
    {
        this._head = null;
        this._tail = null;
        this._semaphore = new Semaphore(0, 0x7FFFFFFF);
        this._threads = new Thread[maxThreadNum];

        for (int i = 0; i < maxThreadNum; i++)
        {
            var thread = new Thread(ThreadRun);
            thread.IsBackground = true;
            this._threads[i] = thread;
        }

        for (int i = 0; i < maxThreadNum; i++)
            this._threads[i].Start(i);
    }

    public void Dispatch(Action action)
    {
        ActionNode node = new ActionNode(action);
        lock (_syncRoot)
        {
            if (_tail != null)
            {
                _tail.Next = node;
                _tail = node;
            }
            else
            {
                _head = _tail = node;
            }
        }
        _semaphore.Release();
    }

    private void ThreadRun(object state)
    {
        while (true)
        {
            if (_semaphore.WaitOne())
            {
                Action action;
                lock (_syncRoot)
                {
                    action = _head.Action;
                    _head = _head.Next;
                    if (_head == null)
                        _tail = null;
                }

                if (action != null)
                {
                    try
                    {
                        action();
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }
    }
}