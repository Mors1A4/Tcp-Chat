using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Remoting.Lifetime;

namespace Eternal_Chat
{
    public class Client
    {
        public string name;
        public Socket socket;
        public NetworkStream stream;
        public TcpClient client;
        public Client(string name, TcpClient client)
        {
            this.name = name;
            socket = client.Client;
            stream = client.GetStream();
            this.client = client;
            
        }

    }
    internal class Program
    {
        public static TcpListener listener;
        public static List<Client> clients = new List<Client>();

        public static Thread ConnectionHandlerThread;
        public static Thread MessageHandlingThread;
        public static Thread CheckConnectionThread;
        static void Main()
        {
            Console.WriteLine("Enter port to run the server on.");
            Console.Write(":>");
            int port = Int32.Parse(Console.ReadLine());
            Console.Clear();
            Console.WriteLine("launching TCP listener");
            
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            Console.WriteLine("launching Connection Handler");
            ConnectionHandlerThread = new Thread(new ThreadStart(ConnectionHandler));
            ConnectionHandlerThread.IsBackground= true;
            ConnectionHandlerThread.Start();

            Console.WriteLine("launching Message Handler");
            MessageHandlingThread = new Thread(new ThreadStart(MessageHandling));
            MessageHandlingThread.IsBackground= true;
            MessageHandlingThread.Start();

            CheckConnectionThread = new Thread(new ThreadStart(CheckConnection));
            CheckConnectionThread.IsBackground= true;
            CheckConnectionThread.Start();

            while(true)
            {

            }
        }
        public static void CheckConnection()
        {
            while(true)
            {
                for (int i = 0; i < clients.Count; i++)
                {
                    if (!SocketConnected(clients[i].socket))
                    {
                        clients.RemoveAt(i);
                        break;
                    }
                }
                Thread.Sleep(10);
            }
        }
        public static void ConnectionHandler()
        {
            
            while (true)
            {
                Console.WriteLine("Looking for incoming connections...");
                TcpClient client = listener.AcceptTcpClient();
                Task.Run(() =>
                {
                    while (client.Connected)
                    {
                        if (client.GetStream().DataAvailable)
                        {
                            string data = ReadToEnd(client.GetStream());
                            string[] args = data.Split('`');
                            if (args[0] == "JOIN")
                            {
                                bool exists = false;
                                foreach(Client c in clients)
                                {
                                    try
                                    {
                                        if(c.name == args[1])
                                            exists= true;
                                    }
                                    catch(Exception) { Console.WriteLine("E001 : There was a trip up when checking name! No worries though, has been handled?"); }
                                } 
                                if(!exists)
                                {
                                    clients.Add(new Client(args[1], client));
                                    write(client.GetStream(), "RESP`SUCCESS");
                                    Console.WriteLine(args[1] + " has connected");
                                    break;
                                     
                                }
                                else
                                {
                                    write(client.GetStream(), "RESP`EXISTS");
                                }
                            }
                        }
                    }
                });
            }
        }

        public static void MessageHandling()
        {
            
            while (true)
            {
                for(int i = 0; i < clients.Count; i++)
                {
                    try
                    {
                        
                        if (clients[i].client.GetStream().DataAvailable)
                        {
                            Console.WriteLine("DATA");
                            string data = ReadToEnd(clients[i].client.GetStream()) ;
                            string[] args = data.Split('`');
                            Console.WriteLine(data);
                            if (args[0] == "MSG")
                            {
                                
                                for (int j = 0; j < clients.Count; j++)
                                {
                                    try
                                    {
                                        write(clients[j].stream, data);
                                    }
                                    catch(Exception) { Console.WriteLine("hey wussupp errorrr"); }
                                }
                            }
                        }
                    }
                    catch(Exception ){ }
                }
            }
        }

        public static void write(NetworkStream stream, string data)
        {
            stream.Write(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetBytes(data).Length);
        }
        public static string ReadToEnd(NetworkStream stream)
        {
            List<byte> recievedbytes = new List<byte>();
            while (stream.DataAvailable)
            {
                byte[] buffer = new byte[1024];
                stream.Read(buffer, 0, buffer.Length);
                recievedbytes.AddRange(buffer);
            }
            recievedbytes.RemoveAll(b => b == 0);
            return Encoding.UTF8.GetString(recievedbytes.ToArray());
        }
        public static bool SocketConnected(Socket s)
        {
            bool part1 = s.Poll(1000, SelectMode.SelectRead);
            bool part2 = (s.Available == 0);
            if (part1 && part2)
                return false;
            else
                return true;
        }

    }
}
