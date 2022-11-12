using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Windows.Forms;

namespace Client
{
    public partial class Form1 : Form
    {
        string ip;
        string port;
        int portNum;

        string name;
        bool terminating = false;
        bool connected = false;

        bool validName = false;

        Socket clientSocket;

        public Form1()
        {
            this.FormClosing += new FormClosingEventHandler(closeForm);
            InitializeComponent();
            Control.CheckForIllegalCrossThreadCalls = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {

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
            if (!validName)
            {
                while (connected)
                {
                    Byte[] bufferReceived = new Byte[64];
                    clientSocket.Receive(bufferReceived);

                    string messageReceived = Encoding.Default.GetString(bufferReceived);
                    messageReceived = messageReceived.Trim('\0');

                    if (messageReceived == "validName")
                    {
                        validName = true;
                    }
                    else if (messageReceived == "invalidName")
                    {
                        richTextBox_logs.AppendText("This name is already taken! Please provide another name.\n");
                    }
                }
            }
            else
            {
                while (connected)
                {
                    //this will keep receiving new messages while it is connected to server
                    try { 
                        //Lets load those received messages into buffer
                        Byte[] bufferReceived = new Byte[64];
                        clientSocket.Receive(bufferReceived);



                        //convert buffer data into string, by default ASCII
                        string messageReceived = Encoding.Default.GetString(bufferReceived);
                        richTextBox_logs.AppendText("Server message: " + messageReceived + "\n");



                        //Byte[] bufferToSend = Encoding.Default.GetBytes(messageToSend);
                        //clientSocket.Send(bufferToSend);

                    }
                    catch
                    {
                        if (!terminating)
                        {
                            //this means there is a problem, but it is related with server (e.g. server stops listening)
                            button_connect.Enabled = true;

                        }
                        else
                        {
                            //this means there is a problem, and it is related with client (e.g. client closes the window)
                        }
                        connected = false;
                        clientSocket.Close();
                    }
                }
            }
            
        }



        private void button_connect_Click(object sender, EventArgs e)
        {
            terminating = false;
            ip = textBox_ip.Text;
            port = textBox_port.Text;
            name = textBox_name.Text;

            if (name != "")
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

                bool convertedPortToInt = Int32.TryParse(port, out portNum);

                if (convertedPortToInt)
                {
                    try
                    {
                        clientSocket.Connect(ip, portNum); //connect and send your name

                        button_connect.Enabled = false;
                        button_disconnect.Enabled = true;
                        connected = true;
    
                        richTextBox_logs.AppendText("Connected to the server." + "\n");

                        //send client name to check if it is unique
                        Byte[] bufferToSend = Encoding.Default.GetBytes(name); 
                        clientSocket.Send(bufferToSend);

                        Thread receiveThread = new Thread(ThreadReceiveFunction);
                        receiveThread.Start();
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
                richTextBox_logs.AppendText("Please enter a valid name!\n");
            }

        }
    }
}