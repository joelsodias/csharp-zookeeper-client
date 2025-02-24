using System;
using System.Text;
using System.Threading.Tasks;
using org.apache.zookeeper;
using org.apache.zookeeper.data;
using static org.apache.zookeeper.ZooDefs;

class Program
{
    private static ZooKeeper? _zooKeeper;
    private static readonly string connectionString = "localhost:2181";
    private static int sessionTimeout = 15000; // 15 seconds
    private static int reconnectAttempts = 5; // Reconnection attempts

    static async Task Main(string[] args)
    {
        await ConnectToZooKeeper();

        string path = "/myNode";
        byte[] data = Encoding.UTF8.GetBytes("My first node!");

        // Create or check if node exists
        await CreateOrCheckNode(path, data);

        // Read node data
        await ReadNodeData(path);

        // Update the node
        byte[] newData = Encoding.UTF8.GetBytes("Updated Information!");
        await UpdateNode(path, newData);

        // Read updated node data
        await ReadNodeData(path);

        // Modify ACL of a node (Example: make it world-readable)
        await ChangeNodeAcl(path);

        // Delete the node
        await DeleteNode(path);

        // List children of a node
        await ListChildren("/");

        // Define election path
        string electionPath = "/leader-election";

        // Attempt to elect a leader
        var leaderNode = await ElectLeader(electionPath);

        if (leaderNode != null)
        {
            Console.WriteLine($"I am the leader! Leader node: {leaderNode}");
            // Perform leader-specific tasks here, e.g., manage resources, handle special jobs, etc.
        }
        else
        {
            Console.WriteLine("I am not the leader, waiting for leader to be elected.");
        }

        // Close the connection
        await _zooKeeper.closeAsync();
    }

    private static async Task ConnectToZooKeeper()
    {
        for (int i = 0; i < reconnectAttempts; i++)
        {
            try
            {
                Console.WriteLine("Attempting to connect...");
                _zooKeeper = new ZooKeeper(connectionString, sessionTimeout, new ZkWatcher());

                await Task.Delay(sessionTimeout);

                if (_zooKeeper.getState() == ZooKeeper.States.CONNECTED)
                {
                    Console.WriteLine("Connected!");
                    return;
                }

                Console.WriteLine($"Reconnection attempt {i + 1} failed.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error trying to connect: {ex.Message}");
            }

            await Task.Delay(2000);
        }

        Console.WriteLine("Failed to connect to ZooKeeper after multiple attempts.");
        Environment.Exit(1);
    }

    private static async Task CreateOrCheckNode(string path, byte[] data)
    {
        try
        {
            var result = await _zooKeeper.existsAsync(path, false);
            if (result == null)
            {
                await _zooKeeper.createAsync(path, data, Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                Console.WriteLine($"Node created: {path}");
            }
            else
            {
                Console.WriteLine($"Node already exists: {path}");
            }
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }

    private static async Task ReadNodeData(string path)
    {
        try
        {
            var dataResult = await _zooKeeper.getDataAsync(path);
            if (dataResult?.Data != null)
            {
                Console.WriteLine($"Node data {path}: {Encoding.UTF8.GetString(dataResult.Data)}");
            }
            else
            {
                Console.WriteLine($"Error: Node data {path} not found or is empty.");
            }
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }

    private static async Task UpdateNode(string path, byte[] newData)
    {
        try
        {
            await _zooKeeper.setDataAsync(path, newData);
            Console.WriteLine($"Node updated: {path}");
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }

    private static async Task DeleteNode(string path)
    {
        try
        {
            await _zooKeeper.deleteAsync(path);
            Console.WriteLine($"Node deleted: {path}");
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }

    private static async Task ListChildren(string path)
    {
        try
        {
            var children = await _zooKeeper.getChildrenAsync(path);
            Console.WriteLine($"Children of node {path}: {string.Join(", ", children)}");
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }

    private static async Task ChangeNodeAcl(string path)
    {
        try
        {
            // Check if the node exists first
            var nodeExists = await _zooKeeper.existsAsync(path, false);
            if (nodeExists == null)
            {
                Console.WriteLine($"Error: Node {path} does not exist, cannot change ACL.");
                return;
            }

            // Set the ACL if the node exists
            var acl = new ACL((int)Perms.ALL, new Id("world", "anyone"));
            await _zooKeeper.setACLAsync(path, new List<ACL> { acl });
            Console.WriteLine($"ACL changed for node {path}");
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }


    private static async Task<string> ElectLeader(string electionPath)
    {
        try
        {
            // Ensure that the election path exists
            var existsResult = await _zooKeeper.existsAsync(electionPath);
            if (existsResult == null)
            {
                // Create the election path if it doesn't exist
                await _zooKeeper.createAsync(electionPath, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.PERSISTENT);
                Console.WriteLine($"Election path {electionPath} created.");
            }

            string leaderPath = electionPath + "/candidate-";
            var newZnode = await _zooKeeper.createAsync(leaderPath, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
            Console.WriteLine($"New candidate created: {newZnode}");

            // Get all candidates
            var childrenResult = await _zooKeeper.getChildrenAsync(electionPath);
            var sortedChildren = childrenResult.Children.OrderBy(c => c).ToList();

            // Check if this node is the leader (smallest znode)
            if (newZnode.EndsWith(sortedChildren.First()))
            {
                Console.WriteLine("I am the leader!");
                return newZnode;
            }
            else
            {
                Console.WriteLine($"I am not the leader, waiting for leader to expire: {newZnode}");
                // Watch for changes to check when the leader node is removed
                await WatchForLeaderChange(electionPath, sortedChildren.First());
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error in leader election: {ex.Message}");
            return null;
        }
    }


    private static async Task WatchForLeaderChange(string electionPath, string leaderNode)
    {
        await _zooKeeper.existsAsync(electionPath + "/" + leaderNode, new ZkWatcher());
        Console.WriteLine($"Watching for leader change: {leaderNode}");
    }

    private static async Task WatchConfigurationChanges(string configPath)
    {
        try
        {
            // Read current configuration
            var configData = await _zooKeeper.getDataAsync(configPath);
            Console.WriteLine($"Current config data: {Encoding.UTF8.GetString(configData.Data)}");

            // Watch for changes to the configuration
            await _zooKeeper.existsAsync(configPath, new ZkWatcher());
        }
        catch (KeeperException e)
        {
            Console.WriteLine($"ZooKeeper Error: {e.Message}");
        }
    }

    private static async Task<bool> AcquireLock(string lockPath)
    {
        try
        {
            string lockNode = lockPath + "/lock-";
            var lockZnode = await _zooKeeper.createAsync(lockNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL_SEQUENTIAL);
            Console.WriteLine($"Lock created: {lockZnode}");

            // Check if this is the first lock (smallest node)
            var childrenResult = await _zooKeeper.getChildrenAsync(lockPath);
            var sortedChildren = childrenResult.Children.OrderBy(c => c).ToList();

            if (lockZnode.EndsWith(sortedChildren.First()))
            {
                Console.WriteLine("Lock acquired successfully.");
                return true;
            }
            else
            {
                Console.WriteLine("Waiting for lock...");
                await WatchForLockRelease(lockPath, sortedChildren.First());
                return false;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error acquiring lock: {ex.Message}");
            return false;
        }
    }

    private static async Task WatchForLockRelease(string lockPath, string lockNode)
    {
        await _zooKeeper.existsAsync(lockPath + "/" + lockNode, new ZkWatcher());
        Console.WriteLine($"Watching for lock release: {lockNode}");
    }


    private static async Task RegisterServerHealth(string healthPath, string serverId)
    {
        try
        {
            string serverNode = healthPath + "/" + serverId;
            await _zooKeeper.createAsync(serverNode, new byte[0], Ids.OPEN_ACL_UNSAFE, CreateMode.EPHEMERAL);

            // Simulate periodic health check
            while (true)
            {
                await Task.Delay(5000); // Heartbeat interval
                Console.WriteLine($"Sending heartbeat for server: {serverId}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error registering server health: {ex.Message}");
        }
    }





}


