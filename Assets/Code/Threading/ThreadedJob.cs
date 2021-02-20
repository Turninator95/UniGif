using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Threading;

public class ThreadedJob
{
    private bool completed = false;
    private object handle = new object();
    private Thread thread = null;

    //Makes sure the variable is not changed by another thread while accessing it
    public bool Completed
    {
        get
        {
            bool completionStatus;
            lock (handle)
            {
                completionStatus = completed;
            }
            return completionStatus;
        }
        set
        {
            lock (handle)
            {
                completed = value;
            }
        }
    }

    public virtual void Start()
    {
        thread = new Thread(Run);
        thread.Start();
    }
    public virtual void Abort()
    {
        thread.Abort();
    }

    /// <summary>
    /// Contains the logic executed in the new thread
    /// </summary>
    protected virtual void ThreadFunction() { }

    /// <summary>
    /// Is called on thread completion
    /// </summary>
    protected virtual void OnFinished() { }

    public virtual bool Update()
    {
        if (Completed)
        {
            OnFinished();
            return true;
        }
        return false;
    }
    public IEnumerator WaitFor()
    {
        while (!Update())
        {
            yield return null;
        }
    }
    private void Run()
    {
        ThreadFunction();
        Completed = true;
    }
}