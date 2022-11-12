namespace server
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.textBox_port = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.button_listen = new System.Windows.Forms.Button();
            this.logs = new System.Windows.Forms.RichTextBox();
            this.Label2 = new System.Windows.Forms.Label();
            this.textBox_message = new System.Windows.Forms.TextBox();
            this.button_send = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.button_stop = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // textBox_port
            // 
            this.textBox_port.Location = new System.Drawing.Point(88, 69);
            this.textBox_port.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBox_port.Name = "textBox_port";
            this.textBox_port.Size = new System.Drawing.Size(182, 20);
            this.textBox_port.TabIndex = 0;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(58, 72);
            this.label1.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(29, 13);
            this.label1.TabIndex = 1;
            this.label1.Text = "Port:";
            // 
            // button_listen
            // 
            this.button_listen.Location = new System.Drawing.Point(88, 106);
            this.button_listen.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button_listen.Name = "button_listen";
            this.button_listen.Size = new System.Drawing.Size(80, 42);
            this.button_listen.TabIndex = 2;
            this.button_listen.Text = "Listen";
            this.button_listen.UseVisualStyleBackColor = true;
            this.button_listen.Click += new System.EventHandler(this.button_listen_Click);
            // 
            // logs
            // 
            this.logs.Location = new System.Drawing.Point(320, 66);
            this.logs.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.logs.Name = "logs";
            this.logs.Size = new System.Drawing.Size(338, 171);
            this.logs.TabIndex = 3;
            this.logs.Text = "";
            // 
            // Label2
            // 
            this.Label2.AutoSize = true;
            this.Label2.Location = new System.Drawing.Point(317, 268);
            this.Label2.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.Label2.Name = "Label2";
            this.Label2.Size = new System.Drawing.Size(53, 13);
            this.Label2.TabIndex = 4;
            this.Label2.Text = "Message:";
            // 
            // textBox_message
            // 
            this.textBox_message.Enabled = false;
            this.textBox_message.Location = new System.Drawing.Point(320, 286);
            this.textBox_message.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.textBox_message.Name = "textBox_message";
            this.textBox_message.Size = new System.Drawing.Size(263, 20);
            this.textBox_message.TabIndex = 5;
            // 
            // button_send
            // 
            this.button_send.Enabled = false;
            this.button_send.Location = new System.Drawing.Point(599, 278);
            this.button_send.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.button_send.Name = "button_send";
            this.button_send.Size = new System.Drawing.Size(59, 28);
            this.button_send.TabIndex = 6;
            this.button_send.Text = "Send";
            this.button_send.UseVisualStyleBackColor = true;
            this.button_send.Click += new System.EventHandler(this.button_send_Click);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(317, 51);
            this.label3.Margin = new System.Windows.Forms.Padding(2, 0, 2, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(33, 13);
            this.label3.TabIndex = 7;
            this.label3.Text = "Logs:";
            // 
            // button_stop
            // 
            this.button_stop.Enabled = false;
            this.button_stop.Location = new System.Drawing.Point(190, 106);
            this.button_stop.Margin = new System.Windows.Forms.Padding(2);
            this.button_stop.Name = "button_stop";
            this.button_stop.Size = new System.Drawing.Size(80, 42);
            this.button_stop.TabIndex = 8;
            this.button_stop.Text = "Stop";
            this.button_stop.UseVisualStyleBackColor = true;
            this.button_stop.Click += new System.EventHandler(this.button_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(707, 382);
            this.Controls.Add(this.button_stop);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.button_send);
            this.Controls.Add(this.textBox_message);
            this.Controls.Add(this.Label2);
            this.Controls.Add(this.logs);
            this.Controls.Add(this.button_listen);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.textBox_port);
            this.Margin = new System.Windows.Forms.Padding(2, 2, 2, 2);
            this.Name = "Form1";
            this.Text = "Form1";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox_port;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Button button_listen;
        private System.Windows.Forms.RichTextBox logs;
        private System.Windows.Forms.Label Label2;
        private System.Windows.Forms.TextBox textBox_message;
        private System.Windows.Forms.Button button_send;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Button button_stop;
    }
}

