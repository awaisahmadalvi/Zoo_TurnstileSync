namespace zooTurnstileSync
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
            this.components = new System.ComponentModel.Container();
            this.inputIP = new System.Windows.Forms.TextBox();
            this.inputPort = new System.Windows.Forms.TextBox();
            this.inputApiUrl = new System.Windows.Forms.TextBox();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.inputStatus = new System.Windows.Forms.RichTextBox();
            this.label4 = new System.Windows.Forms.Label();
            this.inputTime = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.btnConnect = new System.Windows.Forms.Button();
            this.lblConnectionStatus = new System.Windows.Forms.Label();
            this.timerSync = new System.Windows.Forms.Timer(this.components);
            this.rtLogTimer = new System.Windows.Forms.Timer(this.components);
            this.SuspendLayout();
            // 
            // inputIP
            // 
            this.inputIP.Location = new System.Drawing.Point(133, 30);
            this.inputIP.Name = "inputIP";
            this.inputIP.Size = new System.Drawing.Size(167, 20);
            this.inputIP.TabIndex = 0;
            this.inputIP.Text = "192.168.1.201";
            // 
            // inputPort
            // 
            this.inputPort.Location = new System.Drawing.Point(356, 30);
            this.inputPort.Name = "inputPort";
            this.inputPort.Size = new System.Drawing.Size(100, 20);
            this.inputPort.TabIndex = 1;
            this.inputPort.Text = "4370";
            // 
            // inputApiUrl
            // 
            this.inputApiUrl.Location = new System.Drawing.Point(133, 75);
            this.inputApiUrl.Name = "inputApiUrl";
            this.inputApiUrl.Size = new System.Drawing.Size(323, 20);
            this.inputApiUrl.TabIndex = 2;
            this.inputApiUrl.Text = "dummy";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(41, 33);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(58, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "IP Address";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(324, 33);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(26, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Port";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(41, 78);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(49, 13);
            this.label3.TabIndex = 5;
            this.label3.Text = "API URL";
            // 
            // inputStatus
            // 
            this.inputStatus.Location = new System.Drawing.Point(44, 180);
            this.inputStatus.Name = "inputStatus";
            this.inputStatus.Size = new System.Drawing.Size(412, 133);
            this.inputStatus.TabIndex = 6;
            this.inputStatus.Text = "";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(42, 160);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(37, 13);
            this.label4.TabIndex = 7;
            this.label4.Text = "Status";
            // 
            // inputTime
            // 
            this.inputTime.Location = new System.Drawing.Point(133, 119);
            this.inputTime.Name = "inputTime";
            this.inputTime.Size = new System.Drawing.Size(67, 20);
            this.inputTime.TabIndex = 8;
            this.inputTime.Text = "30";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(41, 122);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(83, 13);
            this.label5.TabIndex = 9;
            this.label5.Text = "Sync Time (sec)";
            // 
            // btnConnect
            // 
            this.btnConnect.Location = new System.Drawing.Point(381, 119);
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Size = new System.Drawing.Size(75, 23);
            this.btnConnect.TabIndex = 10;
            this.btnConnect.Text = "Connect";
            this.btnConnect.UseVisualStyleBackColor = true;
            this.btnConnect.Click += new System.EventHandler(this.btnConnect_Click);
            // 
            // lblConnectionStatus
            // 
            this.lblConnectionStatus.AutoSize = true;
            this.lblConnectionStatus.ForeColor = System.Drawing.Color.Red;
            this.lblConnectionStatus.Location = new System.Drawing.Point(41, 333);
            this.lblConnectionStatus.Name = "lblConnectionStatus";
            this.lblConnectionStatus.Size = new System.Drawing.Size(78, 13);
            this.lblConnectionStatus.TabIndex = 11;
            this.lblConnectionStatus.Text = "Dissconnected";
            // 
            // timerSync
            // 
            this.timerSync.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // rtLogTimer
            // 
            this.rtLogTimer.Tick += new System.EventHandler(this.rtLogTimer_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(515, 366);
            this.Controls.Add(this.lblConnectionStatus);
            this.Controls.Add(this.btnConnect);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.inputTime);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.inputStatus);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.inputApiUrl);
            this.Controls.Add(this.inputPort);
            this.Controls.Add(this.inputIP);
            this.Name = "Form1";
            this.Text = "Zoo Turnstile Sync (PITB)";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox inputIP;
        private System.Windows.Forms.TextBox inputPort;
        private System.Windows.Forms.TextBox inputApiUrl;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.RichTextBox inputStatus;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox inputTime;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Button btnConnect;
        private System.Windows.Forms.Label lblConnectionStatus;
        private System.Windows.Forms.Timer timerSync;
        private System.Windows.Forms.Timer rtLogTimer;
    }
}

