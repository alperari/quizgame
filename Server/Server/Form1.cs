using System;
using System.IO;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;




namespace server
{
    public struct Client
    {
        public Socket socket;
        public string name;
        public double score;

        public Client(Socket s, string n)
        {
            socket = s;
            name = n;
            score = 0;
        }

    }

    public partial class Form1 : Form
    {

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        List<Client> clientSockets = new List<Client>();
        List<string> names = new List<string>();
        List<string> questions = new List<string>();
        List<string> answers = new List<string>();

        int currentClosest = Int16.MaxValue;
        string currentClosestName = "";


        bool terminating = false;
        bool listening = false;
        bool isGameStarted = false;
        bool isSameAnswer = false;
        string serverIP;

        static Barrier barrier = new Barrier(2, x => { });


        public void sendMessageToClient(Client thisClient, string message) // takes socket and message then sends the message to that socket
        {
            Byte[] buffer = new Byte[1000];
            buffer = Encoding.UTF8.GetBytes(message);
            thisClient.socket.Send(buffer);
        }

        public string receiveMessageFromClient(Client thisClient)
        {
            Byte[] buffer = new Byte[1000];
            thisClient.socket.Receive(buffer);

            string incomingMessage = Encoding.UTF8.GetString(buffer);
            
            incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

            incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

            return incomingMessage;
        }



        private void ThreadAcceptFunction()
        {
            while (listening)
            {
                try
                {
                    Client newClient = new Client(serverSocket.Accept(), "");

                    // Do not add this client into list before making sure it has unique name
                    //clientSockets.Add(newClient);

                    //Receive registerName:<name> for the first time
                    string incomingMessage = receiveMessageFromClient(newClient);
                    logs.AppendText("Client: " + incomingMessage + "\n");

                    string[] parsedIncomingMessage = incomingMessage.Split(':');
                    string command = parsedIncomingMessage[0];
                    string name = parsedIncomingMessage[1];

                    if (command == "registerName")
                    {
                        if (!names.Contains(name))
                        {
                            logs.AppendText(name + " is connected.\n");

                            string message = "validConnection";
                            Byte[] buffer = new Byte[1000];
                            buffer = Encoding.UTF8.GetBytes(message);

                            sendMessageToClient(newClient, message);

                            newClient.name = name;
                            clientSockets.Add(newClient);
                            names.Add(name);

                            Thread receiveThread = new Thread(() => ThreadReceiveFunction(newClient));
                            receiveThread.Start();

                        }

                        else
                        {
                            logs.AppendText(name + " tried to do invalid connection.\n");

                            sendMessageToClient(newClient, "invalidConnection");
                            newClient.socket.Close();
                            //connected = false;
                        }
                    }

                }
                catch
                {
                    if (terminating)
                    {
                        listening = false;
                    }
                    else
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }

                }
            }
        }

        private void ThreadReceiveFunction(Client thisClient) // updated
        {
            bool connected = true;

            while (connected && !terminating)
            {
                try
                {
                    if(names.Count == 2)
                    {
                        lock (this)
                        {
                            isGameStarted = true;
                        }
                        sendMessageToClient(thisClient, "Game is started\n");


                        for (int i=0; i< questions.Capacity; i++)
                        {
                            string question = questions[i];
                            string realAnswer = answers[i];
                            sendMessageToClient(thisClient, question);


                            string incomingMessage = receiveMessageFromClient(thisClient);

                            logs.AppendText("Client: " + incomingMessage + "\n");

                            string[] parsedIncomingMessage = incomingMessage.Split(':');
                            string command = parsedIncomingMessage[0];
                            string name = parsedIncomingMessage[1];
                            string clientAnswer = parsedIncomingMessage[2];
                            Console.WriteLine("a");
                            calculateScore(thisClient.name,clientAnswer, realAnswer);
                            Console.WriteLine("a");
                            ///TODO: Process incoming message and scores
                            barrier.SignalAndWait();

                            if (isSameAnswer) {
                                thisClient.score += 0.5;
                            }
                            else
                            {
                                if (thisClient.name == currentClosestName)
                                {
                                    thisClient.score += 1;
                                }
                            }
                            Console.Write(thisClient.score);


                        }
                    }

                }
                catch
                {
                    clientSockets.Remove(thisClient);
                    thisClient.socket.Close();
                    names.Remove(thisClient.name);
                    connected = false;
                    if (!terminating)
                    {
                        // That means, the client disconnected itself
                        logs.AppendText(thisClient.name + " is disconnected.\n");

                        // If one of the clients left during the game. Other client is the winner automatically.
                        if (isGameStarted)
                        {
                            foreach (Client client in clientSockets)
                            {
                                String message = thisClient.name + " is left. Your are the winner";
                                sendMessageToClient(client, message);

                                logs.AppendText("Game is over");
                                sendMessageToClient(client, "Game is over.");
        
                            }
                        }
                    }
                
                }
            }
        }

        // TODO: Client are not waiting each other during the game
        //private void startGame(Client client)
        //{
        //    while(names.Count == 2  && !terminating)
        //    {
        //        try
        //        {
        //            foreach (string question in this.questions)
        //            {
        //                sendMessageToClient(client, question);
        //                receiveMessageFromClient(client);
        //            }
        //        }
        //        catch
        //        {
        //            Console.WriteLine("ao");
        //        }

        //    }

        //}
        public void calculateScore(String clientName,String clientAnswer, String realAnswer)
        {
            int intClientAnswer = Int16.Parse(clientAnswer);
            int intRealAnswer = Int16.Parse(realAnswer);
            lock (this)
            {
                if (Math.Abs(intClientAnswer - intRealAnswer) < currentClosest)
                {
                    currentClosest = intClientAnswer;
                    currentClosestName = clientName;
                }
                else if (Math.Abs(intClientAnswer - intRealAnswer) == currentClosest)
                {
                    isSameAnswer = true;
                }
            }
        }
        private void readFile(string path)
        {
            int counter = 0;

            // Read the file and display it line by line.  
            foreach (string line in System.IO.File.ReadLines(path))
            {
                if (counter%2 == 0)
                {
                    this.questions.Add(line);
                }
                else
                {
                    this.answers.Add(line);
                }
                counter++;
            }
        }


        public Form1()
        {
            Control.CheckForIllegalCrossThreadCalls = false;
            this.FormClosing += new FormClosingEventHandler(closeForm);
            InitializeComponent();
        }
        private void closeForm(object sender, System.ComponentModel.CancelEventArgs e)
        {
            listening = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;

            string path = "../../../../questions.txt";          
            this.readFile(path);

            if (Int32.TryParse(textBox_port.Text, out serverPort))
            {
                IPEndPoint endPoint = new IPEndPoint(IPAddress.Any, serverPort);
                serverIP = endPoint.Address.ToString();
                serverSocket.Bind(endPoint);
                serverSocket.Listen(2);

                listening = true;
                button_listen.Enabled = false;
                button_stop.Enabled = true;
                textBox_message.Enabled = true;
                button_send.Enabled = true;

                Thread acceptThread = new Thread(ThreadAcceptFunction);
                acceptThread.Start();

                logs.AppendText("Started listening on address: " + serverIP + ", on port: " + serverPort + "\n");
            }
            else
            {
                logs.AppendText("Please check port number!\n");
            }
        }



        private void button_send_Click(object sender, EventArgs e)
        {
            string message = textBox_message.Text;
            if(message != "" && message.Length <= 64)
            {
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                foreach (Client client in clientSockets)
                {
                    try
                    {
                        client.socket.Send(buffer);
                    }
                    catch
                    {
                        logs.AppendText("There is a problem! Check the connection...\n");
                        terminating = true;
                        textBox_message.Enabled = false;
                        button_send.Enabled = false;
                        textBox_port.Enabled = true;
                        button_listen.Enabled = true;
                        button_stop.Enabled = true;

                        serverSocket.Close();
                    }

                }
            }
        }

        private void button_Click(object sender, EventArgs e)
        {

        }
    }
}
