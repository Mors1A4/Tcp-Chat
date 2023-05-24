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
            // Gets the IP and port that the user wants to connect to
            Console.WriteLine("Enter port to run the server on.");
            Console.Write(":>");
            int port = Int32.Parse(Console.ReadLine());
            Console.Clear();
            Console.WriteLine("launching TCP listener");
            
            // Starts the listener on the IP and port to get new connections
            listener = new TcpListener(IPAddress.Any, port);
            listener.Start();

            // This will handle any new incoming connections, working in the background
            Console.WriteLine("launching Connection Handler");
            ConnectionHandlerThread = new Thread(new ThreadStart(ConnectionHandler));
            ConnectionHandlerThread.IsBackground= true;
            ConnectionHandlerThread.Start();

            // This will detect any incoming messages from any clients which are connected and send them out to other clients
            Console.WriteLine("launching Message Handler");
            MessageHandlingThread = new Thread(new ThreadStart(MessageHandling));
            MessageHandlingThread.IsBackground= true;
            MessageHandlingThread.Start();

            // This will ensure that if a client disconnects then they will be removed from the list
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
                            //i have decided to use the ` character to split up messages between the user and server
                            string[] args = data.Split('`');
                            // if the message is join then it will add them to the list using their name passed as arg[1]
                            if (args[0] == "JOIN")
                            {
                                // this boolean is used to check whether the users name is already in use by someone else
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
                            // if there is data available then it will read all of the data as a string
                            string data = ReadToEnd(clients[i].client.GetStream()) ;
                            string[] args = data.Split('`');
                            //if the data is a message then it will send it out to every client
                            if (args[0] == "MSG")
                            {
                                
                                for (int j = 0; j < clients.Count; j++)
                                {
                                    try
                                    {
                                        write(clients[j].stream, data);
                                    }
                                    catch(Exception){ }
                                }
                            }
                        }
                    }
                    catch(Exception ){ }
                }
            }
        }
        // this is simply used so that i do not have to write as much
        public static void write(NetworkStream stream, string data)
        {
            stream.Write(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetBytes(data).Length);
        }

        // this will read all bytes with a buffer of 1024 untill theres none available and then remove any blank spaces
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

        //this will check wether the socket is connected
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
