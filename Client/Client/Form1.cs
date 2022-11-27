using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using System.Diagnostics;

namespace Client
{
    public partial class Form1 : Form
    {
        string ip = "localhost";
        string port = "1111";
        int portNum = 1111;


        string name;
        bool terminating = false;
        bool connected = false;

        bool isNameRegistered = false;

        Socket clientSocket;


        public void sendMessageToServer(string message)
        {
            Byte[] bufferToSend = Encoding.UTF8.GetBytes(message);
            clientSocket.Send(bufferToSend);
        }

        public string receiveMessageFromServer()
        {
            Byte[] buffer = new Byte[1000];
            clientSocket.Receive(buffer);

            string incomingMessage = Encoding.UTF8.GetString(buffer);
            incomingMessage = incomingMessage.Substring(0, incomingMessage.IndexOf("\0"));

            return incomingMessage;
        }

        public void onDisconnectRevertButtons()
        {
            connected = false;
            terminating = true;

            isNameRegistered = false;

            button_connect.Enabled = true;
            button_disconnect.Enabled = false;

            button_send.Enabled = false;
            textBox_message.Enabled = false;
            textBox_ip.Enabled = true;
            textBox_port.Enabled = true;
            textBox_name.Enabled = true;

            // Make sure that '.Close()' comes after connected=false
            // Otherwise receiving thread will keep listening from server even after disconnected (yeah weird)
            //clientSocket.Close();
        }


        public Form1()
        {
            this.FormClosing += new FormClosingEventHandler(closeForm);
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void closeForm(object sender, System.ComponentModel.CancelEventArgs e)
        {
            connected = false;
            terminating = true;
            Environment.Exit(0);
        }

        private void ThreadReceiveFunction()
        {
            //first, try to authenticate ourselves
            while (connected)
            {
                //this will keep receiving new messages while it is connected to server
                try
                {
                    string messageReceived = receiveMessageFromServer();
                    Console.WriteLine(messageReceived);
                    if (messageReceived == "Game is over.")
                    {
                        // If game is over, we need to throw exception to activate the catch part
                        richTextBox_logs.AppendText("Server: " + messageReceived + "\n");
                        connected = false;
                        terminating = true;
                        throw new System.Net.Sockets.SocketException();
                    }

                    richTextBox_logs.AppendText("Server: " + messageReceived + "\n");

                    ///TODO: Keep receiving questions and send answers
                    ///

                }
                catch
                {
                    if (!terminating)
                    {
                        //this means there is a problem, but it is related with server (e.g. server stops listening)
                        button_connect.Enabled = true;
                        button_disconnect.Enabled = false;
                        button_send.Enabled = false;
                        textBox_message.Enabled = false;
                        textBox_name.Enabled = true;
                        textBox_ip.Enabled = true;
                        textBox_port.Enabled = true;
                        richTextBox_logs.AppendText("Server shut down.\n");

                    }
                    else
                    {
                        //this means that game is over
                        button_connect.Enabled = true;
                        button_disconnect.Enabled = false;
                        button_send.Enabled = false;
                        textBox_message.Enabled = false;
                        textBox_name.Enabled = true;
                        textBox_ip.Enabled = true;
                        textBox_port.Enabled = true;
                    }

                    connected = false;
                    clientSocket.Close();
                }
            }


        }



        private void button_connect_Click(object sender, EventArgs e)
        {
            terminating = false;
            ///TODO
            //ip = textBox_ip.Text;
            //port = textBox_port.Text;
            name = textBox_name.Text;

            if (name != "")
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                //if (Int32.TryParse(port, out portNum))
                if (true)
                {
                    try
                    {
                        clientSocket.Connect(ip, portNum);

                        button_connect.Enabled = false;
                        button_disconnect.Enabled = true;
                        button_send.Enabled = true;
                        textBox_message.Enabled = true;
                        textBox_ip.Enabled = false;
                        textBox_port.Enabled = false;
                        textBox_name.Enabled = false;
                        connected = true;


                        // Register my name by sending 'registerName' command
                        // If my name already exists in server,
                        // Then server will kick you out, you don't have to do clientSocket.Close()!
                        string registerMessage = "registerName:" + name;
                        sendMessageToServer(registerMessage);

                        string registerResponse = receiveMessageFromServer();
                        Debug.WriteLine(registerResponse);

                        if (registerResponse == "invalidConnection")
                        {
                            richTextBox_logs.AppendText("This name is already taken. Please try again.\n");
                            onDisconnectRevertButtons();
                            // Don't do clientSocket.Close()!
                            // Server has done that automatically

                        }
                        else if (registerResponse == "validConnection")
                        {
                            // If you reach this line, that means you registered your name successfully
                            richTextBox_logs.AppendText("Connected to the server." + "\n");
                            
                            // Now you can start your listening thread
                            Thread receiveThread = new Thread(ThreadReceiveFunction);
                            receiveThread.Start();
                        }

                    }
                    catch (Exception)
                    {
                        richTextBox_logs.AppendText("Something went wrong..." + "\n");
                    }

                }
                else
                {
                    richTextBox_logs.AppendText("Invalid port!" + "\n");
                }
            }
            else
            {
                richTextBox_logs.AppendText("Please enter your name!\n");
            }

        }

        private void button_disconnect_Click(object sender, EventArgs e)
        {
            clientSocket.Close();
            onDisconnectRevertButtons();
            richTextBox_logs.AppendText("Disconnected from the server.\n");
        }

        private void button_send_Click(object sender, EventArgs e)
        {
            string answer = "answer:"+name+":"+textBox_message.Text;
            sendMessageToServer(answer);
            string log = "Your answer: " + answer + "\n";
            richTextBox_logs.AppendText(log);
        }
    }
}