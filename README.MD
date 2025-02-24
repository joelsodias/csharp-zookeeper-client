 # ZooKeeper Client Demo
 
 This project demonstrates ZooKeeper client operations using C# and ZooKeeperNetEx.
 
 ## Prerequisites
 
 - .NET 8.0 SDK
 - Docker and Docker Compose
 
 ## Project Setup
 
 Create a new C# project and add ZooKeeper package:
 
 ```bash
 dotnet new console -n ZookeeperClient
 cd ZookeeperClient
 dotnet add package ZooKeeperNetEx
 ```
 
 ## Docker Setup
 
 1. Create a `docker-compose.yml` file:
 
 ```yaml
 version: '3.8'
 services:
   zookeeper:
     image: zookeeper:3.9
     restart: always
     ports:
       - "2181:2181"
     environment:
       ZOO_MY_ID: 1
       ZOO_SERVERS: server.1=0.0.0.0:2888:3888
     volumes:
       - ./zoo.cfg:/conf/zoo.cfg
 ```
 
 2. Create a `zoo.cfg` file:
 
 ```properties
 dataDir=/data
 dataLogDir=/datalog
 tickTime=2000
 autopurge.snapRetainCount=3
 autopurge.purgeInterval=0
 maxClientCnxns=60
 standaloneEnabled=true
 admin.enableServer=true
 server.1=zookeeper:2888:3888
 initLimit=10
 syncLimit=5
 clientPort=2181
 ```
 
 ## Starting ZooKeeper
 
 Start the ZooKeeper container:
 
 ```bash
 docker-compose up -d
 ```
 
 Verify ZooKeeper is running:
 
 ```bash
 docker-compose ps
 ```
 
 ## Running the Client
 
 Build and run the client:
 
 ```bash
 dotnet build
 dotnet run
 ```
 
 ## Features Demonstrated
 
 - Basic ZooKeeper connection
 - Node creation and management
 - Data reading and writing
 - ACL management
 - Leader election
 - Distributed locks
 - Health monitoring
 
 ## Stopping ZooKeeper
 
 Stop and remove the container:
 
 ```bash
 docker-compose down
 ```
