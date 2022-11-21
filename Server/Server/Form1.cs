using System;
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

        public Client(Socket s, string n)
        {
            socket = s;
            name = n;
        }
    }

    public partial class Form1 : Form
    {

        Socket serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        
        List<Client> clientSockets = new List<Client>();
        List<string> names = new List<string>();

        bool terminating = false;
        bool listening = false;
        string serverIP;

        public void RegisterName(Socket thisClient, string name)
        {
            bool isNameRegistered = names.Contains(name);
            if (isNameRegistered)
            {
                string message = "INVALID-NAME";
                Byte[] buffer = Encoding.UTF8.GetBytes(message);
                logs.AppendText(name + " is already taken.\n");
                thisClient.Send(buffer);
            }
            else
            {
                string message = "VALID-NAME";
                Byte[] buffer = Encoding.UTF8.GetBytes(message);

                thisClient.Send(buffer);
                logs.AppendText(name + " is successfully registered.\n");

                names.Add(name);
            }
        }

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
            incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

            return incomingMessage;
        }

        //public bool checkAuthorization(Socket thisClient, ref string thisClientName)
        //{
        //    Byte[] buffer = new Byte[1000];
        //    thisClient.Receive(buffer);
        //    string incomingMessage = Encoding.UTF8.GetString(buffer);

        //    logs.AppendText("Client: " + incomingMessage + "\n");

        //    string[] parsedIncomingMessage = incomingMessage.Split(':');
        //    string clientOperation = parsedIncomingMessage[0];
        //    string clientName = parsedIncomingMessage[1];

        //    if(clientOperation == "registerName")
        //    {
        //        if (!names.Contains(clientName))
        //        {
        //            logs.AppendText($"A client is connected with name: {clientName} \n");
        //            sendMessageToClient(thisClient, "Successfully registered.\n");
        //            names.Add(clientName);
        //            clientSockets.Add(thisClient);
        //            thisClientName = clientName;

        //            return true;
        //        }
        //        else
        //        {
        //            //this name is already taken, warn the client and close its connection!
        //            logs.AppendText("A client tried to connect with an already used name: " + clientName + ". Closed its connection.\n");
        //            sendMessageToClient(thisClient, "This name is already taken. Please try again.\n");
        //            thisClient.Close();
        //            return false;
        //        }
        //    }
        //    return false;

        //}


        private void ThreadAcceptFunction()
        {
            while (listening)
            {
                try
                {
                    Client newClient = new Client(serverSocket.Accept(), "");
                    
                    // Do not add this client into list before making sure it has unique name
                    //clientSockets.Add(newClient);

                    Thread receiveThread = new Thread(() => ThreadReceiveFunction(newClient)); // updated
                    receiveThread.Start();
                    //}

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

                    //Receive registerName:<name> for the first time
                    string registerMessage= receiveMessageFromClient(thisClient);
                    logs.AppendText("Client: " + registerMessage + "\n");

                    string[] parsedIncomingMessage = registerMessage.Split(':');
                    string command = parsedIncomingMessage[0];
                    string name = parsedIncomingMessage[1];

                    if (!names.Contains(name))
                    {
                        logs.AppendText(name + " is connected.\n");
                        sendMessageToClient(thisClient, "invalidConnection");
                        logs.AppendText("message sent!\n");
                        string message = "validConnection";
                        Byte[] buffer = new Byte[1000];
                        buffer = Encoding.UTF8.GetBytes(message);
                        thisClient.socket.Send(buffer);
                        thisClient.name = name;

                        clientSockets.Add(thisClient);
                        names.Add(name);
                    }
                    else
                    {
                        logs.AppendText(name + " tried to do invalid connection.\n");

                        sendMessageToClient(thisClient, "invalidConnection");
                        thisClient.socket.Close();
                        connected = false;
                    }
                   



                }
                catch
                {
                    if (!terminating)
                    {
                        // That means, the client disconnected itself
                        logs.AppendText(thisClient.name + " is disconnected.\n");
                    }
                    clientSockets.Remove(thisClient);
                    //thisClient.socket.Close();
                    //names.Remove(clientName);
                    connected = false;
                }
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
            //foreach (Socket client in clientSockets)
            //{
            //    client.Close(); 
            //}
            Environment.Exit(0);
        }

        private void button_listen_Click(object sender, EventArgs e)
        {
            int serverPort;

            if(Int32.TryParse(textBox_port.Text, out serverPort))
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
