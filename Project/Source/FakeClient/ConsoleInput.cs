// ConsoleInput.cs
// Created by xiaojl Aug/25/2022
// 控制台输入组件

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;

public class ConsoleInput : Component, IFrameDrived
{
    public void Start()
    {
        _running = true;
        _thread = new Thread(new ThreadStart(WorkingThread));
        _thread.Start();
    }

    public override void Init(Core core)
    {
        base.Init(core);

        OnCommand("leave", (ps) => {
            if (ps == null || ps.Length < 1)
                return string.Format("command warning: {0}, please input user id.", "leave");

            var userId = ps[0];
            var game = (Game)_core;
            return game.LetUserLeaveRoom(userId);
        });

        OnCommand("export", (ps) => {
            var da = GetCom<DataAnalysis>();
            da.Export();
            return "Waiting for export data";
        });
    }

    public void OnTimeElapsed(int elapsedTime)
    {
        Action cmd = null;
        while (_cmdQueue.TryDequeue(out cmd))
            cmd();
    }

    private void WorkingThread()
    {
        while (_running)
        {
            var cmd = "";
            cmd = Console.ReadLine();
            var arr = cmd.Split("".ToCharArray(), StringSplitOptions.RemoveEmptyEntries);
            cmd = arr.Length > 0 ? arr[0] : "";

            string[] ps = null;
            if (arr.Length > 1)
            {
                ps = new string[arr.Length - 1];
                for (int i = 0; i < arr.Length - 1; i++)
                    ps[i] = arr[i + 1];
            }
            
            lock(_cmdHandlers)
            {
                if (!_cmdHandlers.ContainsKey(cmd))
                    _cmdQueue.Enqueue(() => { Console.WriteLine("Unkonwn command: " + cmd); });
                else
                    _cmdQueue.Enqueue(() => { Console.WriteLine(_cmdHandlers[cmd](ps)); });
            }
        }
    }

    private void OnCommand(string cmd, Func<string[], string> func)
    {
        lock(_cmdHandlers)
            _cmdHandlers[cmd] = func;
    }

    private Thread _thread;
    private bool _running = false;
    private ConcurrentQueue<Action> _cmdQueue = new ConcurrentQueue<Action>();
    private Dictionary<string, Func<string[], string>> _cmdHandlers = new Dictionary<string, Func<string[], string>>();
}