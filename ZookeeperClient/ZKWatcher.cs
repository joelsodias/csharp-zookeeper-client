using org.apache.zookeeper;

public class ZkWatcher : Watcher
{
    public override Task process(WatchedEvent @event)
    {
        Console.WriteLine($"ZooKeeper Event: {@event.get_Type()}");
        return Task.CompletedTask;
    }
}