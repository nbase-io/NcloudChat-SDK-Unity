using System;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour
{
    private static UnityMainThreadDispatcher _instance;
    private Queue<Action> _queue = new Queue<Action>();

    // 싱글턴 인스턴스
    public static UnityMainThreadDispatcher Instance
    {
        get
        {
            if (_instance == null)
            {
                throw new Exception("An instance of UnityMainThreadDispatcher does not exist in the scene.");
            }
            return _instance;
        }
    }

    void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(this.gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // 작업을 큐에 추가하는 메서드
    public void Enqueue(Action action)
    {
        lock (_queue)
        {
            _queue.Enqueue(action);
        }
    }

    // 큐에 있는 작업을 메인 스레드에서 실행
    void Update()
    {
        while (_queue.Count > 0)
        {
            Action action = null;
            lock (_queue)
            {
                if (_queue.Count > 0)
                {
                    action = _queue.Dequeue();
                }
            }
            action?.Invoke();
        }
    }
}