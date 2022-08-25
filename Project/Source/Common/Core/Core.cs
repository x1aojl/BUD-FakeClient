public class Core
{
    public virtual void Initialize()
    {
        foreach (var c in _cc.All)
            c.Init(this);
    }

    public virtual void RunOneFrame(int elapsedTime)
    {
        Component[] arr = _cc.All;
        for (int i = 0; i < arr.Length; i++)
        {
            Component c = arr[i];

            if (c is IFrameDrived)
                (c as IFrameDrived).OnTimeElapsed(elapsedTime);
        }
    }

    public T Get<T>() where T : class
    {
        return _cc.Get<T>();
    }

    public Component GetByName(string name)
    {
        return _cc.GetByName(name);
    }

    public void Add(string name, Component c)
    {
        _cc.Add(name, c);
    }

    public void Remove(string name)
    {
        _cc.Remove(name);
    }

    private ComponentContainer _cc = new ComponentContainer();
}
