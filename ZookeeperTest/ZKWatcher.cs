using org.apache.zookeeper;

public class ZkWatcher : Watcher
{
    public override Task process(WatchedEvent @event)
    {
        Console.WriteLine($"Evento do ZooKeeper: {@event.get_Type()}");
        return Task.CompletedTask;
    }
}