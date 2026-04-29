namespace stream
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
            picRemoteVideo = new PictureBox();
            picLocalVideo = new PictureBox();
            btnStart = new Button();
            ((System.ComponentModel.ISupportInitialize)picRemoteVideo).BeginInit();
            ((System.ComponentModel.ISupportInitialize)picLocalVideo).BeginInit();
            SuspendLayout();
            // 
            // picRemoteVideo
            // 
            picRemoteVideo.Location = new Point(249, 57);
            picRemoteVideo.Name = "picRemoteVideo";
            picRemoteVideo.Size = new Size(505, 329);
            picRemoteVideo.TabIndex = 0;
            picRemoteVideo.TabStop = false;
            // 
            // picLocalVideo
            // 
            picLocalVideo.Location = new Point(49, 57);
            picLocalVideo.Name = "picLocalVideo";
            picLocalVideo.Size = new Size(157, 101);
            picLocalVideo.TabIndex = 1;
            picLocalVideo.TabStop = false;
            // 
            // btnStart
            // 
            btnStart.Location = new Point(49, 363);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(100, 23);
            btnStart.TabIndex = 2;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            // 
            // Form1
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(800, 450);
            Controls.Add(btnStart);
            Controls.Add(picLocalVideo);
            Controls.Add(picRemoteVideo);
            Name = "Form1";
            Text = "Form1";
            ((System.ComponentModel.ISupportInitialize)picRemoteVideo).EndInit();
            ((System.ComponentModel.ISupportInitialize)picLocalVideo).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private PictureBox picRemoteVideo;
        private PictureBox picLocalVideo;
        private Button btnStart;
    }
}
