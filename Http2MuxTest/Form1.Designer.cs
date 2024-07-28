namespace Http2MuxTest
{
    partial class Form1
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            sendRequestBtn = new Button();
            receiverBox = new TextBox();
            senderBox = new TextBox();
            label1 = new Label();
            label2 = new Label();
            SuspendLayout();
            // 
            // sendRequestBtn
            // 
            sendRequestBtn.Location = new Point(151, 296);
            sendRequestBtn.Name = "sendRequestBtn";
            sendRequestBtn.Size = new Size(94, 29);
            sendRequestBtn.TabIndex = 0;
            sendRequestBtn.Text = "Send";
            sendRequestBtn.UseVisualStyleBackColor = true;
            sendRequestBtn.Click += SendRequestBtn_Click;
            // 
            // receiverBox
            // 
            receiverBox.ForeColor = Color.Black;
            receiverBox.Location = new Point(436, 92);
            receiverBox.Multiline = true;
            receiverBox.Name = "receiverBox";
            receiverBox.ReadOnly = true;
            receiverBox.RightToLeft = RightToLeft.No;
            receiverBox.ScrollBars = ScrollBars.Vertical;
            receiverBox.Size = new Size(330, 185);
            receiverBox.TabIndex = 1;
            // 
            // senderBox
            // 
            senderBox.ForeColor = Color.Black;
            senderBox.Location = new Point(41, 92);
            senderBox.Multiline = true;
            senderBox.Name = "senderBox";
            senderBox.ReadOnly = true;
            senderBox.RightToLeft = RightToLeft.No;
            senderBox.ScrollBars = ScrollBars.Vertical;
            senderBox.Size = new Size(330, 185);
            senderBox.TabIndex = 2;
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label1.Location = new Point(166, 57);
            label1.Name = "label1";
            label1.Size = new Size(57, 20);
            label1.TabIndex = 3;
            label1.Text = "Sender";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label2.Location = new Point(568, 57);
            label2.Name = "label2";
            label2.Size = new Size(68, 20);
            label2.TabIndex = 4;
            label2.Text = "Receiver";
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(senderBox);
            Controls.Add(receiverBox);
            Controls.Add(sendRequestBtn);
            Name = "Form1";
            Text = "Form1";
            Load += Form1_Load;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Button sendRequestBtn;
        private TextBox receiverBox;
        private TextBox senderBox;
        private Label label1;
        private Label label2;
    }
}
