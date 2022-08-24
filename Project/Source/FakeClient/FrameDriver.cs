using System;
using System.Threading;

public class FrameDriver
{
    public static FrameDriver Instance = new FrameDriver();

    public Action<int> RunOneFrame;

    // 逻辑帧间隔时长（毫秒）
    private int _interval = 50;

    private bool _running = false;

    public void Start()
    {
        _running = true;

        long time = DateTime.Now.Ticks / 10000;

        while (_running)
        {
            long now = DateTime.Now.Ticks / 10000;
            int deltaTime = (int)(now - time);

            RunOneFrame?.Invoke(deltaTime);

            time = now;
            int sleepTime = _interval > deltaTime ? _interval - deltaTime : 0;

            Thread.Sleep(sleepTime);
        }
    }

    public void Stop()
    {
        _running = false;
    }
}