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

        public static Thread ReceiveMessageThread;
        public static Thread CheckConnectionThread;
        static void Main(string[] args)
        {
            // while the client isnt connected it will keep trying to get them to connect
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

            //this is used to check for any new messages in the background
            ReceiveMessageThread = new Thread(new ThreadStart(ReceiveMessageProtocol));
            ReceiveMessageThread.IsBackground= true;
            ReceiveMessageThread.Start();

            // this is used to ensure the user is allways connected to the server else it will get the to re connect
            CheckConnectionThread = new Thread(new ThreadStart(CheckConnection));
            CheckConnectionThread.IsBackground= true;   
            CheckConnectionThread.Start();

            //display any new messages and then prompt the user for a message they may want to send
            Task.Run(() => SendMessageProtocol());

            // this is used to keep the program running
            while(true)
            {

            }    

        }

        public static void CheckConnection()
        {
            while(true)
            {
                if (Connected)
                {
                    if (!SocketConnected(client.Client))
                    {
                        Connected = false;
                        while (!Connected)
                        {
                            try
                            {
                                ConnectProtocol();
                            }
                            catch (Exception) { }
                        }

                    }
                }
                Thread.Sleep(100);
            }
            

        }

        public static void SendMessageProtocol()
        {
            /* the use of the there boolean is to check whether there is a Console.Readline already there.
             * 
             * this is done as it is very hard to abort the console.readline function after it has been called
             * so if it has already been called and hasnt been given a response then we will not call it again
             * until the user has entered some text
             * 
             */
            if(there)
            {
                Console.Clear();
                // this is used to print all of the lines of chat that the user has entered
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
                // this is used to clear the chat if the user requests
                if (message == "/clr")
                {
                    chat = new List<string[]>();
                    Console.Clear();
                    SendMessageProtocol();
                }
                else
                {
                    // will send the message to the server with arg[0] being MSG arg[2] being their name and arg[3] being the message
                    write("MSG`" + name + "`" + message);
                }
                    
            }
            
        }
        
        public static void ReceiveMessageProtocol()
        {
            while(true)
            {
                if(client.GetStream().DataAvailable)
                {
                    //if the data is a message then it will add it to the chat and reload the prompt so the new message sends
                    
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
                // gets the IP and Server that the client wants to connect to 
                Console.Clear();
                Console.WriteLine("Client is Currently not connected to any servers");
                Console.WriteLine("Join Server-");
                Console.WriteLine();
                Console.Write("Enter IP: ");
                string IP = Console.ReadLine();
                Console.Write("Enter port: ");
                int port = Int32.Parse(Console.ReadLine());

                //connects to the server and gets the username the user wants to use
                client.Connect(IP, port);
                string displayText = "Connected!";
                //this will keep checking for a response untill one is given
                while (!Connected)
                {
                    Console.Clear();
                    Console.WriteLine(displayText);
                    Console.Write("Enter username: ");
                    name = Console.ReadLine();
                    // sends the name to the server with arg[0] being JOIN and arg[1] being the username
                    write("JOIN`" + name);
                    Console.Clear();
                    Console.WriteLine("Loading...");
                    if (client.GetStream().DataAvailable)
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
                                displayText = "Username is already in use!";
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
        // used to save time when writing to the server
        public static void write(string data)
        {
            try
            {
                client.GetStream().Write(Encoding.UTF8.GetBytes(data), 0, Encoding.UTF8.GetBytes(data).Length);
            }
            catch (Exception e) { Console.WriteLine(e.ToString()); }
            
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
            Console.WriteLine(recievedbytes.ToArray());
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
