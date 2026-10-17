namespace SimpleHMS
{
    partial class LoginForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LoginForm));
            btnLogin = new Button();
            label1 = new Label();
            label2 = new Label();
            txtemail = new TextBox();
            label3 = new Label();
            label5 = new Label();
            panel2 = new Panel();
            txtpassword = new TextBox();
            pictureBox2 = new PictureBox();
            pictureBox1 = new PictureBox();
            pictureBox3 = new PictureBox();
            pictureBox6 = new PictureBox();
            panel2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).BeginInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox6).BeginInit();
            SuspendLayout();
            //
            // btnLogin
            //
            btnLogin.BackColor = Color.Ivory;
            btnLogin.ForeColor = Color.Black;
            btnLogin.Location = new Point(55, 416);
            btnLogin.Name = "btnLogin";
            btnLogin.Size = new Size(252, 43);
            btnLogin.TabIndex = 0;
            btnLogin.Text = "Login";
            btnLogin.UseVisualStyleBackColor = false;
            btnLogin.Click += btnLogin_Click;
            //
            // label1
            //
            label1.AutoSize = true;
            label1.BackColor = Color.Transparent;
            label1.Font = new Font("Segoe UI", 10F);
            label1.ForeColor = Color.AliceBlue;
            label1.Location = new Point(55, 244);
            label1.Name = "label1";
            label1.Size = new Size(41, 19);
            label1.TabIndex = 2;
            label1.Text = "Username";
            //
            // label2
            //
            label2.AutoSize = true;
            label2.BackColor = Color.Transparent;
            label2.Font = new Font("Segoe UI", 10F);
            label2.ForeColor = Color.AliceBlue;
            label2.Location = new Point(55, 322);
            label2.Name = "label2";
            label2.Size = new Size(67, 19);
            label2.TabIndex = 3;
            label2.Text = "Password";
            //
            // txtemail
            //
            txtemail.MaxLength = 50;   // Users.Username NVARCHAR(50)
            txtemail.BackColor = Color.SkyBlue;
            txtemail.Location = new Point(55, 276);
            txtemail.Name = "txtemail";
            txtemail.Size = new Size(256, 23);
            txtemail.TabIndex = 4;
            //
            // label3
            //
            label3.AutoSize = true;
            label3.BackColor = Color.Transparent;
            label3.Font = new Font("Open Sans", 20.25F, FontStyle.Bold, GraphicsUnit.Point, 0);
            label3.ForeColor = Color.White;
            label3.Location = new Point(120, 137);
            label3.Name = "label3";
            label3.Size = new Size(141, 39);
            label3.TabIndex = 7;
            label3.Text = "Welcome";
            //
            // label5
            //
            label5.AutoSize = true;
            label5.BackColor = Color.Transparent;
            label5.Font = new Font("Segoe UI", 9F);
            label5.ForeColor = Color.AliceBlue;
            label5.Location = new Point(38, 176);
            label5.Name = "label5";
            label5.Size = new Size(294, 15);
            label5.TabIndex = 10;
            label5.Text = "Start a better experience by logging into your account.";
            //
            // panel2
            //
            panel2.BackColor = Color.Transparent;
            panel2.BackgroundImageLayout = ImageLayout.Zoom;
            panel2.Controls.Add(txtpassword);
            panel2.Controls.Add(btnLogin);
            panel2.Controls.Add(pictureBox2);
            panel2.Controls.Add(label5);
            panel2.Controls.Add(label3);
            panel2.Controls.Add(pictureBox1);
            panel2.Controls.Add(txtemail);
            panel2.Controls.Add(label2);
            panel2.Controls.Add(label1);
            panel2.Controls.Add(pictureBox3);
            panel2.Location = new Point(92, 85);
            panel2.Name = "panel2";
            panel2.Size = new Size(375, 469);
            panel2.TabIndex = 13;
            //
            // txtpassword
            //
            txtpassword.MaxLength = 100;   // Users.Password legacy NVARCHAR(100)
            txtpassword.BackColor = Color.SkyBlue;
            txtpassword.Location = new Point(55, 349);
            txtpassword.Name = "txtpassword";
            txtpassword.Size = new Size(256, 23);
            txtpassword.TabIndex = 5;
            //
            // pictureBox2
            //
            pictureBox2.Image = (Image)resources.GetObject("pictureBox2.Image");
            pictureBox2.Location = new Point(-4, -37);
            pictureBox2.Name = "pictureBox2";
            pictureBox2.Size = new Size(140, 156);
            pictureBox2.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox2.TabIndex = 12;
            pictureBox2.TabStop = false;
            //
            // pictureBox1
            //
            pictureBox1.Image = (Image)resources.GetObject("pictureBox1.Image");
            pictureBox1.Location = new Point(142, 36);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(87, 81);
            pictureBox1.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox1.TabIndex = 11;
            pictureBox1.TabStop = false;
            //
            // pictureBox3
            //
            pictureBox3.Image = (Image)resources.GetObject("pictureBox3.Image");
            pictureBox3.Location = new Point(232, 38);
            pictureBox3.Name = "pictureBox3";
            pictureBox3.Size = new Size(140, 156);
            pictureBox3.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox3.TabIndex = 13;
            pictureBox3.TabStop = false;
            //
            // pictureBox6
            //
            pictureBox6.BackColor = Color.Transparent;
            pictureBox6.Image = (Image)resources.GetObject("pictureBox6.Image");
            pictureBox6.Location = new Point(324, -8);
            pictureBox6.Name = "pictureBox6";
            pictureBox6.Size = new Size(140, 156);
            pictureBox6.SizeMode = PictureBoxSizeMode.Zoom;
            pictureBox6.TabIndex = 15;
            pictureBox6.TabStop = false;
            //
            // LoginForm
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.Gainsboro;
            BackgroundImage = (Image)resources.GetObject("$this.BackgroundImage");
            BackgroundImageLayout = ImageLayout.Stretch;
            ClientSize = new Size(536, 636);
            Controls.Add(pictureBox6);
            Controls.Add(panel2);
            DoubleBuffered = true;
            FormBorderStyle = FormBorderStyle.None;
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "LoginForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Login";
            TransparencyKey = Color.Gainsboro;
            panel2.ResumeLayout(false);
            panel2.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)pictureBox2).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox3).EndInit();
            ((System.ComponentModel.ISupportInitialize)pictureBox6).EndInit();
            ResumeLayout(false);
        }

        #endregion

        private Button btnLogin;
        private Label label1;
        private Label label2;
        private TextBox txtemail;
        private Label label3;
        private Label label5;
        private Panel panel2;
        private PictureBox pictureBox1;
        private PictureBox pictureBox2;
        private PictureBox pictureBox3;
        private TextBox txtpassword;
        private PictureBox pictureBox6;
    }
}
