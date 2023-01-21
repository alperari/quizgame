﻿using System;
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
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace Server
{
    public partial class Form1 : Form
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

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

        List<Client> clientSockets = new List<Client>();
        List<Client> playerSockets = new List<Client>();

        List<string> names = new List<string>();
      

        List<string> questions = new List<string>();
        List<string> answersReal = new List<string>();

        int numberOfQuestions = 0;

        List<Tuple<Client, int>> clientAnswers = new List<Tuple<Client, int>>();

        bool terminating = false;
        bool listening = false;
        bool isGameStarted = false;
        string serverIP;
        bool oneplayer = true;
        bool alreadyCalculated = false;
        string roundStatus = "NONE";
        List<string> winnerName = new List<string>();
        List<Client> game_winner = new List<Client>();
        bool isQuestionPrinted = false;
        string win = "";
        bool game_w_found = false;
        static Barrier barrier = new Barrier(0);


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

                    if (command == "registerName") //BARANNNNN 1. DEĞİŞİKLİK
                    {
                        bool con = false;
                        lock (this)
                        {
                            con = names.Contains(name);
                        }
                        
                        if (!con)
                            {
                                logs.AppendText(name + " is connected.\n");

                                string message = "validConnection";
                                Byte[] buffer = new Byte[1000];
                                buffer = Encoding.UTF8.GetBytes(message);

                                sendMessageToClient(newClient, message);

                                newClient.name = name;
                                lock(this)
                                {
                                    if (!isGameStarted)
                                    {
                                        clientSockets.Add(newClient);
                                        playerSockets.Add(newClient);
                                        names.Add(name);
                                        



                                    }
                                    else
                                    {
                                        clientSockets.Add(newClient);
                                        names.Add(name);
                                    }
                                }
                               
                                
                                


                                Thread receiveThread = new Thread(() => ThreadReceiveFunction(newClient));
                                
                                //while (isGameStarted) {}
                                
                                receiveThread.Start();

                            



                        }
                        else
                        {
                            logs.AppendText(name + " tried to make an invalid connection with a taken name.\n");
                            /*for(int i = 0; i< names.Count;i++)
                            {
                                logs.AppendText(names[i] + " ");
                            }
                            logs.AppendText("\n");*/
                            
                            sendMessageToClient(newClient, "invalidConnection:nameTaken");
                            newClient.socket.Close();
                            //connected = false;
                        }
                    }
                        
                    

                }

                catch
                {
                    if (!terminating)
                    {
                        logs.AppendText("The socket stopped working.\n");
                    }
                    listening = false;

                }
            }
        }
        
        
        private void ThreadReceiveFunction(Client thisClient) // updated
        {
            bool connected = true;
            for (int i = 0; i < names.Count; i++)
            {
                logs.AppendText(names[i] + " ");
            }
            logs.AppendText("\n");
            while (connected && !terminating)
            {
                try
                {
                    System.Threading.Thread.Sleep(100);
                    
                    sendMessageToClient((Client)thisClient, "waitingForPlayers");
                    bool playing_game = false;
                    lock (this)
                    {
                        playing_game = playerSockets.Contains(thisClient) && (playerSockets.Count >= 2) && (!start_game.Enabled);
                    }
                    if (playing_game)//BARAAAAAAAAAAAAAAAAN 2. DEĞİŞİKLİK
                    {
                        lock (this)
                        {
                            oneplayer = false;
                            barrier.AddParticipant();
                            if (!isGameStarted)
                            {
                                isGameStarted = true;

                                
                                logs.AppendText("Game started.\n");
                            }
                            
                        }
                        System.Threading.Thread.Sleep(100);
                        sendMessageToClient(thisClient, "Game is started. There will be " + numberOfQuestions + " questions.\n");
                        
                        barrier.SignalAndWait();
                        int realAnswer = 0;
                        int currentQuestionNo = 0;
                        for (int i = 0; i < numberOfQuestions; i++)
                        {
                            
                            
                            currentQuestionNo = currentQuestionNo % (questions.Count);


                            string question = questions[currentQuestionNo];
                            realAnswer = Int16.Parse(answersReal[currentQuestionNo]);
                            sendMessageToClient(thisClient, "Question" + (i+1).ToString() + " ==> " + question);


                            lock (this)
                            {
                                if (!isQuestionPrinted)
                                {
                                    isQuestionPrinted = true;
                                    logs.AppendText("Question" + (i+1).ToString() + " ==> " + question+"\n");

                                }
                            }

                            barrier.SignalAndWait();
                            if (oneplayer)
                            {
                                sendMessageToClient(thisClient, "Only one player remains\n");

                                break;
                            }
                            barrier.SignalAndWait();
                            string incomingMessage = receiveMessageFromClient(thisClient);
                            lock(this)
                            {
                                if (oneplayer)
                                {
                                    sendMessageToClient(thisClient, "OnePlayer");

                                    break;
                                }
                            }
                            
                            logs.AppendText("Client: " + incomingMessage + "\n");
                            if (incomingMessage == "waitingforPlayers")
                            {
                                terminating = false;
                                throw new System.Net.Sockets.SocketException();
                            }
                            

                            string[] parsedIncomingMessage = incomingMessage.Split(':');

                            string command = parsedIncomingMessage[0];
                            string name = parsedIncomingMessage[1];
                            int clientAnswer = Int16.Parse(parsedIncomingMessage[2]);


                            

                            

                            // We will store clients and their answers each round as List<Tuple>
                            Tuple<Client, int> clientAnswerTuple = Tuple.Create(thisClient, clientAnswer);
                            

                            lock (this)
                            {
                                //logs.AppendText(thisClient.name + " " + clientAnswer.ToString() + "\n");
                                clientAnswers.Add(clientAnswerTuple);
                              
                            }
                            if (oneplayer)
                            {
                                sendMessageToClient(thisClient, "Only one player remains\n");

                                break;
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
                                if (!alreadyCalculated)
                                    calculateScores(realAnswer, ref roundStatus, ref winnerName);
                                alreadyCalculated = true;
                                if (oneplayer)
                                {
                                    sendMessageToClient(thisClient, "Only one player remains\n");
                                    
                                    
                                    break;
                                }
                            }
                            
                            barrier.SignalAndWait();
                            
                            // Send scores to all clients, in descending order

                            /*
                            lock(this)
                            {
                                
                                logs.AppendText(thisClient.name+ "!!!\n");
                                
                            }
                            logs.AppendText("\n");
                            */

                            sendRoundResults(thisClient, realAnswer, roundStatus, ref winnerName);
                            sendScores(thisClient);
                            if (oneplayer)
                            {
                                sendMessageToClient(thisClient, "Only one player remains\n");

                                break;
                            }

                            barrier.SignalAndWait();


                            // After each round, make sure you revert things properly
                            // So that new calculations are done correctly in the next round
                            lock (this)
                            {
                                alreadyCalculated = false;
                                isQuestionPrinted = false;
                                roundStatus = "NONE";
                                winnerName.Clear();
                                clientAnswers.Clear();
                            }

                            currentQuestionNo++;
                            
                            barrier.SignalAndWait();
                        }
                        lock(this)
                        {
                            if (oneplayer == true)
                            {
                                
                                sendScores(thisClient);
                                alreadyCalculated = false;
                                isQuestionPrinted = false;
                                roundStatus = "NONE";
                                winnerName.Clear();
                                clientAnswers.Clear();
                                sendMessageToClient(playerSockets[0], "Only one player remains\n");
                                
                            }
                        }
                       
                        
                        lock (this)
                        {
                            if(!game_w_found)
                            {
                                game_w_found = true;
                                game_winner = find_winners(playerSockets);

                                for (int i = 0; i < game_winner.Count; i++)
                                {

                                    if (i != game_winner.Count - 1)
                                    {
                                        win += game_winner[i].name + ", ";
                                    }
                                    else
                                    {
                                        win += game_winner[i].name;
                                    }
                                    //logs.AppendText(game_winner[i].name + "?");
                                    
                                }
                                //logs.AppendText("\n");
                            }

                        }
                        barrier.SignalAndWait();
                        /*
                        lock(this)
                        {
                            for (int t = 0; t < playerSockets.Count; t++)
                            {
                                logs.AppendText(playerSockets[t].name + "/");
                            }
                            logs.AppendText("\n");
                            for (int t = 0; t < clientSockets.Count; t++)
                            {
                                logs.AppendText(clientSockets[t].name + "*");
                            }
                            logs.AppendText("\n");
                        }
                        */
                        lock(this)
                        {
                            
                            if ((game_winner.Count == playerSockets.Count) && (playerSockets.Count != 1))
                            {

                                sendMessageToClient(thisClient, "It is tie. Game is over\n");

                            }
                            else if (game_winner.Contains(thisClient))
                            {

                                sendMessageToClient(thisClient, "You are winner. Game is over\n");
                                logs.AppendText("Game is over. " + win + " is/are the winner(s).\n");
                                


                            }

                            else
                            {

                                sendMessageToClient(thisClient, win + " is/are the winner(s). Game is over\n");

                            }
                        }
                        

                        barrier.SignalAndWait();
                       
                        
                        foreach (Client c in clientSockets)
                        {
                            c.score = 0.0;
                        }
                        lock(this)
                        {
                            
                            barrier.RemoveParticipant();

                            playerSockets.Clear();
                           
                            for(int i = 0; i < clientSockets.Count;i++)
                            {
                                playerSockets.Add(clientSockets[i]);
                            }
                            
                            
                            clientAnswers.Clear();
                            game_winner.Clear();
                            isGameStarted = false;
                            start_game.Enabled = true;
                            win = "";
                            game_w_found = false;
                            

                        }
                        

                    }
                    else
                    {
                        /*for (int i = 0; i < names.Count; i++)
                        {
                            logs.AppendText(names[i] + " ");
                        }
                        logs.AppendText("\n");*/
                    }
                    

                   


                }
                catch
                {
                    lock (this)
                    {
                        if (playerSockets.Contains(thisClient)&& isGameStarted)
                        {
                            logs.AppendText(thisClient.name + "'s point become 0.\n");
                            barrier.RemoveParticipant();
                            
                        }
                        clientSockets.Remove(thisClient);
                        playerSockets.Remove(thisClient);
                        names.Remove(thisClient.name);
                        /*for (int i = 0; i < names.Count; i++)
                        {
                            logs.AppendText(names[i] + " ");
                        }
                        logs.AppendText("\n");*/
                        thisClient.socket.Close();
                       
                        
                        game_winner.Remove(thisClient);
                        winnerName.Remove(thisClient.name);
                        thisClient.score = 0;
                        connected = false;
                    
                    
                        if (!terminating)
                        {
                            // That means, the client disconnected itself
                            
                            logs.AppendText(thisClient.name + " is disconnected.\n");

                            // If one of the clients left during the game. Other client is the winner automatically.

                            if ((playerSockets.Count < 2) && (!start_game.Enabled))
                            {

                                oneplayer = true;
                                sendMessageToClient(playerSockets[0], "OnePlayer");


                            }
                        }


                    }

                }
            }
        }

        public List<Client> find_winners(List<Client> players)
        {
            double highest_score = 0.0;
            List<Client> w = new List<Client>();
            
                foreach (Client c in players)
                {
                    if (c.score > highest_score)
                    {
                        highest_score = c.score;
                        w.Clear();
                        w.Add(c);
                    }
                    else if (c.score == highest_score)
                    {
                        w.Add(c);
                    }

                }
            
            return w;
        }
        double point = 0.0;
        public void calculateScores(int realAnswer, ref string roundStatus, ref List<string> winnerName)
        {
            bool isSameAnswer = true;

            // Check if all of answers are the same
            lock(this)
            {
                
                for (int i = 0; i < clientAnswers.Count - 1; i++)
                {
                    if (Math.Abs(clientAnswers[i].Item2 - realAnswer) != Math.Abs(clientAnswers[i+1].Item2 - realAnswer))
                    {
                        isSameAnswer = false;
                        break;
                    }
                }
            }
            

            //Debug.Write(closestClient.name, closestClient.score.ToString());
            bool pce1 = false;
            lock(this)
            {

                pce1 = (playerSockets.Count != 1);
            }
            if(pce1)
            {

                if (isSameAnswer)
                {
                    lock (this)
                    {
                        for (int i = 0; i < playerSockets.Count; i++)
                        {
                            point = (1.0 / playerSockets.Count);
                            point = (double)System.Math.Round(point, 2);

                            playerSockets[i].score += point;
                            
                        }
                        roundStatus = "TIE";
                    }

                    return;
                }
                else
                {
                    // Find the client with closest answer

                    List<String> closestClientName = new List<string>();
                    int closestAnswer = int.MaxValue;

                    for (int i = 0; i < clientAnswers.Count; i++)
                    {
                        Tuple<Client, int> clientAnswer = clientAnswers[i];
                        if (Math.Abs(clientAnswer.Item2 - realAnswer) < Math.Abs(closestAnswer - realAnswer))
                        {
                            closestAnswer = clientAnswer.Item2;
                            closestClientName.Clear();
                            closestClientName.Add(clientAnswer.Item1.name);
                        }
                        else if (Math.Abs(clientAnswer.Item2 - realAnswer) == Math.Abs(closestAnswer - realAnswer))
                        {
                            closestClientName.Add(clientAnswer.Item1.name);
                        }
                    }
                    // Update that client's score

                    lock (this)
                    {

                        for (int i = 0; i < playerSockets.Count; i++)
                        {
                            if (closestClientName.Contains(playerSockets[i].name))
                            {
                                point = (1.0 / closestClientName.Count);
                                point = Math.Round(point, 2);

                                playerSockets[i].score += point;

                            }
                        }
                    }
                    winnerName = closestClientName;
                    return;
                }
            }
            else
            {
                winnerName.Clear();
                winnerName.Add(playerSockets[0].name);
            }
        }

        public void sendScores(Client thisClient)
        {
            //List<Client> clientSocketsOrdered = newList
            List<Client> clientSocketsOrdered;
            lock (this)
            {
                clientSocketsOrdered = playerSockets.OrderByDescending(element => element.score).ToList();
            }
            

            string message = "\nScores:\n";
            foreach (Client client in clientSocketsOrdered)
            {
                message += client.name + ": " + client.score.ToString() + "\n";
            }
            Byte[] buffer = Encoding.UTF8.GetBytes(message);
            thisClient.socket.Send(buffer);
        }

        public void sendRoundResults(Client thisClient, int realAnswer, string roundStatus, ref List<string> winnerName)
        {
            if (roundStatus == "TIE")
            {
                string message = "Correct answer was: " + realAnswer.ToString() + ".";
                string message2 = "It is tie. Each player earned "+ point.ToString()+ " point(s).\n";
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                Byte[] buffer2 = Encoding.UTF8.GetBytes(message2);
                thisClient.socket.Send(buffer);
                thisClient.socket.Send(buffer2);


            }
            else
            {
                string message = "Correct answer was: " + realAnswer.ToString() + ".";
                string winner_names = "";
                for (int i = 0; i < winnerName.Count; i++)
                {
                    if(i != (winnerName.Count-1)) 
                    {
                        winner_names += winnerName[i] + ", ";
                    }
                    else
                    {
                        winner_names += winnerName[i];
                    }
                }
                
                string message2 = "Player(s): " + winner_names + " won this round and earned " + point.ToString() + " point(s).\n";
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
                if (counter % 2 == 0)
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
                serverSocket.Listen(16);

                listening = true;
                button_listen.Enabled = false;
                textBox_message.Enabled = true;
                button_send.Enabled = true;
                start_game.Enabled = true;
                numberOfQuestions = Int16.Parse(textBox_numberOfQuestions.Text);

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
            if (message != "" && message.Length <= 64)
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

                        serverSocket.Close();
                    }

                }
            }
        }
        
        private void start_game_Click(object sender, EventArgs e)
        {
            if(clientSockets.Count > 1)
            {
                lock(this)
                {
                    start_game.Enabled = false;
                    isGameStarted = true;
                    
                }
                
            }
            else
            {
                logs.AppendText("There is less than 2 players\n");
            }
            
        }
    }
}
