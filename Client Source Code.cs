using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Threading;
namespace Eternal_Chat_Client
{
    internal class Program
    {
        public static bool there = false;
        public static string message;
        public static TcpClient client = new TcpClient();
        public static bool Connected = false;
        public static List<string[]> chat = new List<string[]>();
        public static string name;

        public static Thread SendMessageThread;
        public static Thread ReceiveMessageThread;
        static void Main(string[] args)
        {
            while(true)
            {
                if(!Connected)
                {
                    ConnectProtocol();
                }
                else
                {
                    break;

                }
            }

            ReceiveMessageThread = new Thread(new ThreadStart(ReceiveMessageProtocol));
            ReceiveMessageThread.IsBackground= true;
            ReceiveMessageThread.Start();

            SendMessageThread = new Thread(new ThreadStart(SendMessageProtocol));
            SendMessageThread.IsBackground = true;
            SendMessageThread.Start();
            while(true)
            {

            }    

        }

        public static void SendMessageProtocol()
        {
            if(there)
            {
                Console.Clear();
                foreach (string[] line in chat)
                {
                    Console.WriteLine(line[0] + ": " + line[1]);
                }
                Console.Write(">:");
                there = false;
            }
            else
            {
                Console.Clear();
                foreach (string[] line in chat)
                {
                    Console.WriteLine(line[0] + ": " + line[1]);
                }
                Console.Write(">:");
                there = true;
                string message = Console.ReadLine();
                there = false;
                write("MSG`" + name + "`" + message);
            }
            
        }
        public static void ReceiveMessageProtocol()
        {
            while(true)
            {
                if(client.GetStream().DataAvailable)
                {
                    
                    string data = ReadToEnd(client.GetStream());
                    string[] args = data.Split('`');
                    if (args[0] == "MSG")
                    {
                        chat.Add(new string[] { args[1], args[2] });

                        Task.Run(() => SendMessageProtocol());
                    }
                    
                }
            }
        }
        public static void ConnectProtocol()
        {
            try
            {
                Console.Clear();
                Console.WriteLine("Client is Currently not connected to any servers");
                Console.WriteLine("Join Server-");
                Console.WriteLine();
                Console.Write("Enter IP: ");
                string IP = Console.ReadLine();
                Console.Write("Enter port: ");
                int port = Int32.Parse(Console.ReadLine());

                client.Connect(IP, port);
                Console.Clear();
                Console.WriteLine("Connected!");
                Console.Write("Enter username: ");
                name = Console.ReadLine();
                write("JOIN`" + name);
                Console.Clear();
                Console.WriteLine("Loading...") ;
                while (!Connected)
                {
                    if(client.GetStream().DataAvailable)
                    {
                        
                        string data = ReadToEnd(client.GetStream());
                        string[] args = data.Split('`');
                        if (args[0] == "RESP")
                        {
                            if (args[1] == "SUCCESS")
                            {
                                Connected= true;

                            }
                            else
                            {
                                break;
                            }
                        }
                        else
                        {
                            break;
                        }
                    }
                }

            }
            catch (Exception)
            {
                Console.WriteLine();
                Console.WriteLine("Failed to connect! Please try again.");
                Console.WriteLine("Press Any Key...");
                Console.ReadKey();
            }
        }
        public static void write(string data)
        {
            try
            {
                client.GetStream().Write(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetBytes(data).Length);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
            
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
            Console.WriteLine(recievedbytes.ToArray());
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
