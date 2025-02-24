using System;
using System.Threading.Tasks;
using org.apache.zookeeper;

class Program
{
    static async Task Main()
    {
        string connectionString = "localhost:2181";
        int sessionTimeout = 10000;

        Console.WriteLine("Tentando conectar...");
        var zk = new ZooKeeper(connectionString, sessionTimeout, new ZkWatcher());

        await Task.Delay(2000);

        if (zk.getState() == ZooKeeper.States.CONNECTED)
        {
            Console.WriteLine("Conectado!");
        }
        else
        {
            Console.WriteLine("Falha na conexão.");
        }

        await zk.closeAsync(); // Fechando a conexão corretamente
    }
}