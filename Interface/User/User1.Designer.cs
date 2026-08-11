namespace BMS
{
    partial class User1
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
            this.menuStrip1 = new System.Windows.Forms.MenuStrip();
            this.系统ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.帮助ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.联系管理员ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.退出ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.借阅信息ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.正在借阅ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.借阅ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridView1 = new System.Windows.Forms.DataGridView();
            this.图书信息ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.归还图书ToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.menuStrip1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).BeginInit();
            this.SuspendLayout();
            // 
            // menuStrip1
            // 
            this.menuStrip1.GripMargin = new System.Windows.Forms.Padding(2, 2, 0, 2);
            this.menuStrip1.ImageScalingSize = new System.Drawing.Size(24, 24);
            this.menuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.系统ToolStripMenuItem,
            this.借阅信息ToolStripMenuItem,
            this.图书信息ToolStripMenuItem});
            this.menuStrip1.Location = new System.Drawing.Point(0, 0);
            this.menuStrip1.Name = "menuStrip1";
            this.menuStrip1.Size = new System.Drawing.Size(1057, 32);
            this.menuStrip1.TabIndex = 0;
            this.menuStrip1.Text = "menuStrip1";
            // 
            // 系统ToolStripMenuItem
            // 
            this.系统ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.帮助ToolStripMenuItem,
            this.联系管理员ToolStripMenuItem,
            this.退出ToolStripMenuItem});
            this.系统ToolStripMenuItem.Name = "系统ToolStripMenuItem";
            this.系统ToolStripMenuItem.Size = new System.Drawing.Size(62, 28);
            this.系统ToolStripMenuItem.Text = "系统";
            // 
            // 帮助ToolStripMenuItem
            // 
            this.帮助ToolStripMenuItem.Name = "帮助ToolStripMenuItem";
            this.帮助ToolStripMenuItem.Size = new System.Drawing.Size(200, 34);
            this.帮助ToolStripMenuItem.Text = "帮助";
            // 
            // 联系管理员ToolStripMenuItem
            // 
            this.联系管理员ToolStripMenuItem.Name = "联系管理员ToolStripMenuItem";
            this.联系管理员ToolStripMenuItem.Size = new System.Drawing.Size(200, 34);
            this.联系管理员ToolStripMenuItem.Text = "联系管理员";
            // 
            // 退出ToolStripMenuItem
            // 
            this.退出ToolStripMenuItem.Name = "退出ToolStripMenuItem";
            this.退出ToolStripMenuItem.Size = new System.Drawing.Size(200, 34);
            this.退出ToolStripMenuItem.Text = "退出";
            // 
            // 借阅信息ToolStripMenuItem
            // 
            this.借阅信息ToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.正在借阅ToolStripMenuItem,
            this.借阅ToolStripMenuItem,
            this.归还图书ToolStripMenuItem});
            this.借阅信息ToolStripMenuItem.Name = "借阅信息ToolStripMenuItem";
            this.借阅信息ToolStripMenuItem.Size = new System.Drawing.Size(98, 28);
            this.借阅信息ToolStripMenuItem.Text = "借阅信息";
            // 
            // 正在借阅ToolStripMenuItem
            // 
            this.正在借阅ToolStripMenuItem.Name = "正在借阅ToolStripMenuItem";
            this.正在借阅ToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.正在借阅ToolStripMenuItem.Text = "正在借阅";
            this.正在借阅ToolStripMenuItem.Click += new System.EventHandler(this.正在借阅ToolStripMenuItem_Click);
            // 
            // 借阅ToolStripMenuItem
            // 
            this.借阅ToolStripMenuItem.Name = "借阅ToolStripMenuItem";
            this.借阅ToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.借阅ToolStripMenuItem.Text = "借阅";
            this.借阅ToolStripMenuItem.Click += new System.EventHandler(this.借阅ToolStripMenuItem_Click);
            // 
            // dataGridView1
            // 
            this.dataGridView1.AllowUserToAddRows = false;
            this.dataGridView1.AllowUserToDeleteRows = false;
            this.dataGridView1.AllowUserToOrderColumns = true;
            this.dataGridView1.AutoSizeColumnsMode = System.Windows.Forms.DataGridViewAutoSizeColumnsMode.Fill;
            this.dataGridView1.BackgroundColor = System.Drawing.SystemColors.ButtonHighlight;
            this.dataGridView1.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.dataGridView1.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.dataGridView1.Location = new System.Drawing.Point(0, 32);
            this.dataGridView1.MultiSelect = false;
            this.dataGridView1.Name = "dataGridView1";
            this.dataGridView1.ReadOnly = true;
            this.dataGridView1.RowHeadersVisible = false;
            this.dataGridView1.RowHeadersWidth = 62;
            this.dataGridView1.RowTemplate.Height = 30;
            this.dataGridView1.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView1.Size = new System.Drawing.Size(1057, 640);
            this.dataGridView1.TabIndex = 1;
            // 
            // 图书信息ToolStripMenuItem
            // 
            this.图书信息ToolStripMenuItem.Name = "图书信息ToolStripMenuItem";
            this.图书信息ToolStripMenuItem.Size = new System.Drawing.Size(98, 28);
            this.图书信息ToolStripMenuItem.Text = "图书信息";
            this.图书信息ToolStripMenuItem.Click += new System.EventHandler(this.查看ToolStripMenuItem_Click);
            // 
            // 归还图书ToolStripMenuItem
            // 
            this.归还图书ToolStripMenuItem.Name = "归还图书ToolStripMenuItem";
            this.归还图书ToolStripMenuItem.Size = new System.Drawing.Size(270, 34);
            this.归还图书ToolStripMenuItem.Text = "归还图书";
            this.归还图书ToolStripMenuItem.Click += new System.EventHandler(this.归还图书ToolStripMenuItem_Click);
            // 
            // User1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(9F, 18F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1057, 672);
            this.Controls.Add(this.dataGridView1);
            this.Controls.Add(this.menuStrip1);
            this.MainMenuStrip = this.menuStrip1;
            this.Name = "User1";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "用户主页面";
            this.Load += new System.EventHandler(this.User1_Load);
            this.menuStrip1.ResumeLayout(false);
            this.menuStrip1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView1)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.MenuStrip menuStrip1;
        private System.Windows.Forms.ToolStripMenuItem 系统ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 帮助ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 联系管理员ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 退出ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 借阅信息ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 正在借阅ToolStripMenuItem;
        private System.Windows.Forms.DataGridView dataGridView1;
        private System.Windows.Forms.ToolStripMenuItem 借阅ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 图书信息ToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem 归还图书ToolStripMenuItem;
    }
}