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
    public class Client
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
        List<string> answersReal = new List<string>();

        List<(Client, int)> clientAnswers = new List<(Client, int)>();

        int currentClosest = Int16.MaxValue;
        string currentClosestName = "";


        bool terminating = false;
        bool listening = false;
        bool isGameStarted = false;
        string serverIP;

        bool alreadyCalculated = false;
        string roundStatus = "NONE";
        string winnerName = "NONE";


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
                        sendMessageToClient(thisClient, "Game is started.\n");


                        for (int i=0; i< questions.Capacity; i++)
                        {
                            string question = questions[i];
                            int realAnswer = Int16.Parse(answersReal[i]);
                            sendMessageToClient(thisClient, question);


                            string incomingMessage = receiveMessageFromClient(thisClient);

                            logs.AppendText("Client: " + incomingMessage + "\n");

                            string[] parsedIncomingMessage = incomingMessage.Split(':');

                            string command = parsedIncomingMessage[0];
                            string name = parsedIncomingMessage[1];
                            int clientAnswer = Int16.Parse(parsedIncomingMessage[2]);



                            // We will store clients and their answers each round as List<Tuple>
                            (Client, int) clientAnswerTuple = (thisClient, clientAnswer);


                            lock (this)
                            {
                                clientAnswers.Add(clientAnswerTuple);
                            }


                            // Both players will wait each other here
                            // Due to barrier
                            barrier.SignalAndWait();

                            // Now you can compare their answers
                            // (barrier guarantees both gave their answers)
                            // And calculate their scores



                            lock (this)
                            {
                                // This lock makes sure that only one thread will perform score calculation
                                // PLUS: it will decide if the game is TIE or there is a winner (ref roundStatus, ref winnerName)
                                if(!alreadyCalculated)
                                    calculateScores(realAnswer, ref roundStatus, ref winnerName);
                                alreadyCalculated = true;
                            }

                            barrier.SignalAndWait();


                            // Send scores to all clients, in descending order
                            sendRoundResults(thisClient, realAnswer, roundStatus, winnerName);
                            sendScores(thisClient);


                            barrier.SignalAndWait();


                            // After each round, make sure you revert things properly
                            // So that new calculations are done correctly in the next round
                            lock (this)
                            {
                                alreadyCalculated = false;
                                roundStatus = "NONE";
                                winnerName = "NONE";
                                clientAnswers.Clear();
                            }

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
                        logs.AppendText(thisClient.name + " is disconnected.");

                        // If one of the clients left during the game. Other client is the winner automatically.
                        if (isGameStarted)
                        {
                            foreach (Client client in clientSockets)
                            {
                                String message = thisClient.name + " is left. Your are the winner.";
                                sendMessageToClient(client, message);

                                logs.AppendText("Game is over.");
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

        public void calculateScores(int realAnswer, ref string roundStatus, ref string winnerName)
        {
            bool isSameAnswer = true;

            // Check if all of answers are the same
            for(int i=0; i<clientAnswers.Count-1; i++)
            {
                if(clientAnswers[i].Item2 != clientAnswers[i + 1].Item2)
                {
                    isSameAnswer = false;
                }
            }


            //Debug.Write(closestClient.name, closestClient.score.ToString());
            if (isSameAnswer)
            {
                for(int i=0; i<clientSockets.Count; i++)
                {
                    clientSockets[i].score += 0.5;
                }
                roundStatus = "TIE";
                return;
            }
            else
            {
                // Find the client with closest answer

                String closestClientName = "";
                int closestAnswer = int.MaxValue;


                foreach ((Client, int) clientAnswer in clientAnswers)
                {
                    if (Math.Abs(clientAnswer.Item2 - realAnswer) < Math.Abs(closestAnswer - realAnswer))
                    {
                        closestAnswer = clientAnswer.Item2;
                        closestClientName = clientAnswer.Item1.name;
                    }
                }
                // Update that client's score
                for (int i = 0; i < clientSockets.Count; i++)
                {
                    if (clientSockets[i].name == closestClientName)
                    {
                        clientSockets[i].score += 1;
                        break;
                    }
                }
                winnerName = closestClientName;
                return;
            }
        }

        public void sendScores(Client thisClient)
        {
            //List<Client> clientSocketsOrdered = newList
            List<Client> clientSocketsOrdered = clientSockets.OrderByDescending(element => element.score).ToList();

            string message = "\nScores:\n";
            foreach (Client client in clientSocketsOrdered)
            {
                message += client.name + ": " + client.score.ToString() + "\n";
            }
            Byte[] buffer = Encoding.UTF8.GetBytes(message);
            thisClient.socket.Send(buffer);
        }

        public void sendRoundResults(Client thisClient, int realAnswer, string roundStatus, string winnerName)
        {
            if(roundStatus == "TIE")
            {
                string message = "Correct answer was: " + realAnswer.ToString() + ".";
                string message2 = "It is tie. Each player earned 0.5 point(s).\n";
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                Byte[] buffer2 = Encoding.UTF8.GetBytes(message2);
                thisClient.socket.Send(buffer);
                thisClient.socket.Send(buffer2);


            }
            else
            {
                string message = "Correct answer was: " + realAnswer.ToString() + ".";
                string message2 = "Player " + winnerName + " won this round and earned 1 point(s).\n";
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                Byte[] buffer2 = Encoding.UTF8.GetBytes(message2);
                thisClient.socket.Send(buffer);
                thisClient.socket.Send(buffer2);
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
                    this.answersReal.Add(line);
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
