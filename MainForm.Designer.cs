namespace VRSEX
{
    partial class MainForm
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
            this.listview_log = new System.Windows.Forms.ListView();
            this.columnHeader1 = ((System.Windows.Forms.ColumnHeader)(new System.Windows.Forms.ColumnHeader()));
            this.button_logout = new System.Windows.Forms.Button();
            this.timer = new System.Windows.Forms.Timer(this.components);
            this.label_login = new System.Windows.Forms.Label();
            this.label_author = new System.Windows.Forms.Label();
            this.checkbox_discord_presence = new System.Windows.Forms.CheckBox();
            this.checkbox_show_location = new System.Windows.Forms.CheckBox();
            this.label_link = new System.Windows.Forms.Label();
            this.nud_RX = new System.Windows.Forms.NumericUpDown();
            this.nud_TX = new System.Windows.Forms.NumericUpDown();
            this.nud_RY = new System.Windows.Forms.NumericUpDown();
            this.nud_S3 = new System.Windows.Forms.NumericUpDown();
            this.nud_TY = new System.Windows.Forms.NumericUpDown();
            this.nud_RZ = new System.Windows.Forms.NumericUpDown();
            this.nud_TZ = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.checkbox_overlay = new System.Windows.Forms.CheckBox();
            ((System.ComponentModel.ISupportInitialize)(this.nud_RX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TX)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_RY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_S3)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TY)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_RZ)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TZ)).BeginInit();
            this.SuspendLayout();
            // 
            // listview_log
            // 
            this.listview_log.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.columnHeader1});
            this.listview_log.GridLines = true;
            this.listview_log.Location = new System.Drawing.Point(12, 134);
            this.listview_log.Name = "listview_log";
            this.listview_log.Size = new System.Drawing.Size(460, 100);
            this.listview_log.TabIndex = 19;
            this.listview_log.UseCompatibleStateImageBehavior = false;
            this.listview_log.View = System.Windows.Forms.View.Details;
            // 
            // columnHeader1
            // 
            this.columnHeader1.Text = "Log Text";
            this.columnHeader1.Width = 420;
            // 
            // button_logout
            // 
            this.button_logout.Location = new System.Drawing.Point(12, 12);
            this.button_logout.Name = "button_logout";
            this.button_logout.Size = new System.Drawing.Size(100, 23);
            this.button_logout.TabIndex = 4;
            this.button_logout.Text = "Logout";
            this.button_logout.UseVisualStyleBackColor = true;
            this.button_logout.Click += new System.EventHandler(this.button_logout_Click);
            // 
            // timer
            // 
            this.timer.Enabled = true;
            this.timer.Interval = 1000;
            this.timer.Tick += new System.EventHandler(this.timer_Tick);
            // 
            // label_login
            // 
            this.label_login.AutoSize = true;
            this.label_login.Location = new System.Drawing.Point(119, 17);
            this.label_login.Name = "label_login";
            this.label_login.Size = new System.Drawing.Size(84, 12);
            this.label_login.TabIndex = 5;
            this.label_login.Text = "Not Logged in";
            // 
            // label_author
            // 
            this.label_author.AutoSize = true;
            this.label_author.Location = new System.Drawing.Point(10, 240);
            this.label_author.Name = "label_author";
            this.label_author.Size = new System.Drawing.Size(48, 12);
            this.label_author.TabIndex = 20;
            this.label_author.Text = "Version";
            // 
            // checkbox_discord_presence
            // 
            this.checkbox_discord_presence.AutoSize = true;
            this.checkbox_discord_presence.Checked = true;
            this.checkbox_discord_presence.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkbox_discord_presence.Location = new System.Drawing.Point(215, 41);
            this.checkbox_discord_presence.Name = "checkbox_discord_presence";
            this.checkbox_discord_presence.Size = new System.Drawing.Size(125, 16);
            this.checkbox_discord_presence.TabIndex = 17;
            this.checkbox_discord_presence.Text = "Discord Presence";
            this.checkbox_discord_presence.UseVisualStyleBackColor = true;
            this.checkbox_discord_presence.CheckedChanged += new System.EventHandler(this.checkbox_discord_CheckedChanged);
            // 
            // checkbox_show_location
            // 
            this.checkbox_show_location.AutoSize = true;
            this.checkbox_show_location.Checked = true;
            this.checkbox_show_location.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkbox_show_location.Location = new System.Drawing.Point(346, 41);
            this.checkbox_show_location.Name = "checkbox_show_location";
            this.checkbox_show_location.Size = new System.Drawing.Size(108, 16);
            this.checkbox_show_location.TabIndex = 18;
            this.checkbox_show_location.Text = "Show Location";
            this.checkbox_show_location.UseVisualStyleBackColor = true;
            this.checkbox_show_location.CheckedChanged += new System.EventHandler(this.checkbox_discord_CheckedChanged);
            // 
            // label_link
            // 
            this.label_link.Cursor = System.Windows.Forms.Cursors.Cross;
            this.label_link.ForeColor = System.Drawing.Color.Blue;
            this.label_link.Location = new System.Drawing.Point(282, 240);
            this.label_link.Name = "label_link";
            this.label_link.Size = new System.Drawing.Size(190, 12);
            this.label_link.TabIndex = 21;
            this.label_link.Text = "DCinside VRChat Minor Gallery";
            this.label_link.TextAlign = System.Drawing.ContentAlignment.TopRight;
            this.label_link.DoubleClick += new System.EventHandler(this.label_link_DoubleClick);
            // 
            // nud_RX
            // 
            this.nud_RX.Location = new System.Drawing.Point(310, 159);
            this.nud_RX.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.nud_RX.Minimum = new decimal(new int[] {
            360,
            0,
            0,
            -2147483648});
            this.nud_RX.Name = "nud_RX";
            this.nud_RX.Size = new System.Drawing.Size(50, 21);
            this.nud_RX.TabIndex = 7;
            this.nud_RX.Visible = false;
            // 
            // nud_TX
            // 
            this.nud_TX.Location = new System.Drawing.Point(310, 186);
            this.nud_TX.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_TX.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.nud_TX.Name = "nud_TX";
            this.nud_TX.Size = new System.Drawing.Size(50, 21);
            this.nud_TX.TabIndex = 11;
            this.nud_TX.Visible = false;
            // 
            // nud_RY
            // 
            this.nud_RY.Location = new System.Drawing.Point(366, 159);
            this.nud_RY.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.nud_RY.Minimum = new decimal(new int[] {
            360,
            0,
            0,
            -2147483648});
            this.nud_RY.Name = "nud_RY";
            this.nud_RY.Size = new System.Drawing.Size(50, 21);
            this.nud_RY.TabIndex = 8;
            this.nud_RY.Visible = false;
            // 
            // nud_S3
            // 
            this.nud_S3.Location = new System.Drawing.Point(310, 213);
            this.nud_S3.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_S3.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.nud_S3.Name = "nud_S3";
            this.nud_S3.Size = new System.Drawing.Size(50, 21);
            this.nud_S3.TabIndex = 15;
            this.nud_S3.Visible = false;
            // 
            // nud_TY
            // 
            this.nud_TY.Location = new System.Drawing.Point(366, 186);
            this.nud_TY.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_TY.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.nud_TY.Name = "nud_TY";
            this.nud_TY.Size = new System.Drawing.Size(50, 21);
            this.nud_TY.TabIndex = 12;
            this.nud_TY.Visible = false;
            // 
            // nud_RZ
            // 
            this.nud_RZ.Location = new System.Drawing.Point(422, 159);
            this.nud_RZ.Maximum = new decimal(new int[] {
            360,
            0,
            0,
            0});
            this.nud_RZ.Minimum = new decimal(new int[] {
            360,
            0,
            0,
            -2147483648});
            this.nud_RZ.Name = "nud_RZ";
            this.nud_RZ.Size = new System.Drawing.Size(50, 21);
            this.nud_RZ.TabIndex = 9;
            this.nud_RZ.Visible = false;
            // 
            // nud_TZ
            // 
            this.nud_TZ.Location = new System.Drawing.Point(422, 186);
            this.nud_TZ.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
            this.nud_TZ.Minimum = new decimal(new int[] {
            1000,
            0,
            0,
            -2147483648});
            this.nud_TZ.Name = "nud_TZ";
            this.nud_TZ.Size = new System.Drawing.Size(50, 21);
            this.nud_TZ.TabIndex = 13;
            this.nud_TZ.Visible = false;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(212, 161);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(50, 12);
            this.label1.TabIndex = 6;
            this.label1.Text = "Rotation";
            this.label1.Visible = false;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(212, 188);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(68, 12);
            this.label2.TabIndex = 10;
            this.label2.Text = "Translation";
            this.label2.Visible = false;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(212, 215);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(37, 12);
            this.label3.TabIndex = 14;
            this.label3.Text = "Scale";
            this.label3.Visible = false;
            // 
            // checkbox_overlay
            // 
            this.checkbox_overlay.AutoSize = true;
            this.checkbox_overlay.Checked = true;
            this.checkbox_overlay.CheckState = System.Windows.Forms.CheckState.Checked;
            this.checkbox_overlay.Location = new System.Drawing.Point(13, 41);
            this.checkbox_overlay.Name = "checkbox_overlay";
            this.checkbox_overlay.Size = new System.Drawing.Size(191, 16);
            this.checkbox_overlay.TabIndex = 16;
            this.checkbox_overlay.Text = "Show Overlay (Favorite Only)";
            this.checkbox_overlay.UseVisualStyleBackColor = true;
            this.checkbox_overlay.CheckedChanged += new System.EventHandler(this.checkbox_overlay_CheckedChanged);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(484, 261);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.nud_TZ);
            this.Controls.Add(this.nud_RZ);
            this.Controls.Add(this.nud_TY);
            this.Controls.Add(this.nud_S3);
            this.Controls.Add(this.nud_RY);
            this.Controls.Add(this.nud_TX);
            this.Controls.Add(this.nud_RX);
            this.Controls.Add(this.checkbox_show_location);
            this.Controls.Add(this.checkbox_overlay);
            this.Controls.Add(this.checkbox_discord_presence);
            this.Controls.Add(this.label_link);
            this.Controls.Add(this.label_author);
            this.Controls.Add(this.label_login);
            this.Controls.Add(this.button_logout);
            this.Controls.Add(this.listview_log);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "VRSEX Control";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MainForm_FormClosing);
            this.Load += new System.EventHandler(this.MainForm_Load);
            ((System.ComponentModel.ISupportInitialize)(this.nud_RX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TX)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_RY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_S3)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TY)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_RZ)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.nud_TZ)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion
        private System.Windows.Forms.ListView listview_log;
        private System.Windows.Forms.ColumnHeader columnHeader1;
        private System.Windows.Forms.Button button_logout;
        private System.Windows.Forms.Timer timer;
        private System.Windows.Forms.Label label_login;
        private System.Windows.Forms.Label label_author;
        private System.Windows.Forms.CheckBox checkbox_discord_presence;
        private System.Windows.Forms.CheckBox checkbox_show_location;
        private System.Windows.Forms.Label label_link;
        private System.Windows.Forms.NumericUpDown nud_RX;
        private System.Windows.Forms.NumericUpDown nud_TX;
        private System.Windows.Forms.NumericUpDown nud_RY;
        private System.Windows.Forms.NumericUpDown nud_S3;
        private System.Windows.Forms.NumericUpDown nud_TY;
        private System.Windows.Forms.NumericUpDown nud_RZ;
        private System.Windows.Forms.NumericUpDown nud_TZ;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.CheckBox checkbox_overlay;
    }
}