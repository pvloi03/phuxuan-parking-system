namespace PhuXuanParkingSystem.Forms
{
    partial class FrmMain
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
            this.pnlHeader = new System.Windows.Forms.Panel();
            this.lblClock = new System.Windows.Forms.Label();
            this.lblSystemStatus = new System.Windows.Forms.Label();
            this.lblAppTitle = new System.Windows.Forms.Label();
            this.tblMainLayout = new System.Windows.Forms.TableLayoutPanel();
            this.grpInLane = new System.Windows.Forms.GroupBox();
            this.tblInCameras = new System.Windows.Forms.TableLayoutPanel();
            this.pnlInPlateVideo = new System.Windows.Forms.Panel();
            this.pnlInOverviewVideo = new System.Windows.Forms.Panel();
            this.picInPlate = new System.Windows.Forms.PictureBox();
            this.picInOverview = new System.Windows.Forms.PictureBox();
            this.grpOutLane = new System.Windows.Forms.GroupBox();
            this.tblOutCameras = new System.Windows.Forms.TableLayoutPanel();
            this.pnlOutPlateVideo = new System.Windows.Forms.Panel();
            this.pnlOutOverviewVideo = new System.Windows.Forms.Panel();
            this.picOutPlate = new System.Windows.Forms.PictureBox();
            this.picOutOverview = new System.Windows.Forms.PictureBox();
            this.grpInInfo = new System.Windows.Forms.GroupBox();
            this.tblInInfo = new System.Windows.Forms.TableLayoutPanel();
            this.lblInPlateTag = new System.Windows.Forms.Label();
            this.txtInPlate = new System.Windows.Forms.TextBox();
            this.lblInTimeTag = new System.Windows.Forms.Label();
            this.lblInTimeVal = new System.Windows.Forms.Label();
            this.lblInOwnerTag = new System.Windows.Forms.Label();
            this.lblInOwnerVal = new System.Windows.Forms.Label();
            this.lblInDeptTag = new System.Windows.Forms.Label();
            this.lblInDeptVal = new System.Windows.Forms.Label();
            this.lblInTypeTag = new System.Windows.Forms.Label();
            this.lblInTypeVal = new System.Windows.Forms.Label();
            this.lblInStatusTag = new System.Windows.Forms.Label();
            this.lblInStatusVal = new System.Windows.Forms.Label();
            this.grpOutInfo = new System.Windows.Forms.GroupBox();
            this.tblOutInfo = new System.Windows.Forms.TableLayoutPanel();
            this.lblOutPlateTag = new System.Windows.Forms.Label();
            this.txtOutPlate = new System.Windows.Forms.TextBox();
            this.lblOutTimeTag = new System.Windows.Forms.Label();
            this.lblOutTimeVal = new System.Windows.Forms.Label();
            this.lblOutOwnerTag = new System.Windows.Forms.Label();
            this.lblOutOwnerVal = new System.Windows.Forms.Label();
            this.lblOutDeptTag = new System.Windows.Forms.Label();
            this.lblOutDeptVal = new System.Windows.Forms.Label();
            this.lblOutTypeTag = new System.Windows.Forms.Label();
            this.lblOutTypeVal = new System.Windows.Forms.Label();
            this.lblOutStatusTag = new System.Windows.Forms.Label();
            this.lblOutStatusVal = new System.Windows.Forms.Label();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.lblFooterStatus = new System.Windows.Forms.ToolStripStatusLabel();
            this.timerClock = new System.Windows.Forms.Timer(this.components);
            this.pnlHeader.SuspendLayout();
            this.tblMainLayout.SuspendLayout();
            this.grpInLane.SuspendLayout();
            this.tblInCameras.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picInPlate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picInOverview)).BeginInit();
            this.grpOutLane.SuspendLayout();
            this.tblOutCameras.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.picOutPlate)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOutOverview)).BeginInit();
            this.grpInInfo.SuspendLayout();
            this.tblInInfo.SuspendLayout();
            this.grpOutInfo.SuspendLayout();
            this.tblOutInfo.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // pnlHeader
            // 
            this.pnlHeader.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.pnlHeader.Controls.Add(this.lblClock);
            this.pnlHeader.Controls.Add(this.lblSystemStatus);
            this.pnlHeader.Controls.Add(this.lblAppTitle);
            this.pnlHeader.Dock = System.Windows.Forms.DockStyle.Top;
            this.pnlHeader.Location = new System.Drawing.Point(0, 0);
            this.pnlHeader.Name = "pnlHeader";
            this.pnlHeader.Size = new System.Drawing.Size(1400, 48);
            this.pnlHeader.TabIndex = 0;
            // 
            // lblClock
            // 
            this.lblClock.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.lblClock.Font = new System.Drawing.Font("Segoe UI", 10.5F, System.Drawing.FontStyle.Bold);
            this.lblClock.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(230)))), ((int)(((byte)(235)))), ((int)(((byte)(240)))));
            this.lblClock.Location = new System.Drawing.Point(1150, 11);
            this.lblClock.Name = "lblClock";
            this.lblClock.Size = new System.Drawing.Size(238, 25);
            this.lblClock.TabIndex = 2;
            this.lblClock.Text = "24/08/2026 00:00:00";
            this.lblClock.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // lblSystemStatus
            // 
            this.lblSystemStatus.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.lblSystemStatus.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblSystemStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(200)))), ((int)(((byte)(210)))), ((int)(((byte)(220)))));
            this.lblSystemStatus.Location = new System.Drawing.Point(460, 15);
            this.lblSystemStatus.Name = "lblSystemStatus";
            this.lblSystemStatus.Size = new System.Drawing.Size(665, 25);
            this.lblSystemStatus.TabIndex = 1;
            this.lblSystemStatus.Text = "Đang khởi tạo hệ thống...";
            this.lblSystemStatus.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblAppTitle
            // 
            this.lblAppTitle.AutoSize = true;
            this.lblAppTitle.Font = new System.Drawing.Font("Segoe UI", 11.5F, System.Drawing.FontStyle.Bold);
            this.lblAppTitle.ForeColor = System.Drawing.Color.White;
            this.lblAppTitle.Location = new System.Drawing.Point(12, 10);
            this.lblAppTitle.Name = "lblAppTitle";
            this.lblAppTitle.Size = new System.Drawing.Size(418, 31);
            this.lblAppTitle.TabIndex = 0;
            this.lblAppTitle.Text = "HỆ THỐNG KIỂM SOÁT XE THÁI THỤY";
            // 
            // tblMainLayout
            // 
            this.tblMainLayout.ColumnCount = 2;
            this.tblMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblMainLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblMainLayout.Controls.Add(this.grpInLane, 0, 0);
            this.tblMainLayout.Controls.Add(this.grpOutLane, 1, 0);
            this.tblMainLayout.Controls.Add(this.grpInInfo, 0, 1);
            this.tblMainLayout.Controls.Add(this.grpOutInfo, 1, 1);
            this.tblMainLayout.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblMainLayout.Location = new System.Drawing.Point(0, 48);
            this.tblMainLayout.Name = "tblMainLayout";
            this.tblMainLayout.Padding = new System.Windows.Forms.Padding(4);
            this.tblMainLayout.RowCount = 2;
            this.tblMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 70F));
            this.tblMainLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tblMainLayout.Size = new System.Drawing.Size(1400, 781);
            this.tblMainLayout.TabIndex = 1;
            // 
            // grpInLane
            // 
            this.grpInLane.Controls.Add(this.tblInCameras);
            this.grpInLane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInLane.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpInLane.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.grpInLane.Location = new System.Drawing.Point(7, 7);
            this.grpInLane.Name = "grpInLane";
            this.grpInLane.Padding = new System.Windows.Forms.Padding(6);
            this.grpInLane.Size = new System.Drawing.Size(690, 535);
            this.grpInLane.TabIndex = 0;
            this.grpInLane.TabStop = false;
            this.grpInLane.Text = "LÀN VÀO (IN-LANE)";
            // 
            // tblInCameras
            // 
            this.tblInCameras.ColumnCount = 2;
            this.tblInCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblInCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblInCameras.Controls.Add(this.pnlInPlateVideo, 0, 0);
            this.tblInCameras.Controls.Add(this.pnlInOverviewVideo, 1, 0);
            this.tblInCameras.Controls.Add(this.picInPlate, 0, 1);
            this.tblInCameras.Controls.Add(this.picInOverview, 1, 1);
            this.tblInCameras.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblInCameras.Location = new System.Drawing.Point(6, 32);
            this.tblInCameras.Name = "tblInCameras";
            this.tblInCameras.RowCount = 2;
            this.tblInCameras.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.tblInCameras.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.tblInCameras.Size = new System.Drawing.Size(678, 497);
            this.tblInCameras.TabIndex = 0;
            // 
            // pnlInPlateVideo
            // 
            this.pnlInPlateVideo.BackColor = System.Drawing.Color.Black;
            this.pnlInPlateVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInPlateVideo.Location = new System.Drawing.Point(3, 3);
            this.pnlInPlateVideo.Name = "pnlInPlateVideo";
            this.pnlInPlateVideo.Size = new System.Drawing.Size(333, 282);
            this.pnlInPlateVideo.TabIndex = 0;
            this.pnlInPlateVideo.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlInPlateVideo_Paint);
            // 
            // pnlInOverviewVideo
            // 
            this.pnlInOverviewVideo.BackColor = System.Drawing.Color.Black;
            this.pnlInOverviewVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlInOverviewVideo.Location = new System.Drawing.Point(342, 3);
            this.pnlInOverviewVideo.Name = "pnlInOverviewVideo";
            this.pnlInOverviewVideo.Size = new System.Drawing.Size(333, 282);
            this.pnlInOverviewVideo.TabIndex = 1;
            this.pnlInOverviewVideo.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlInOverviewVideo_Paint);
            // 
            // picInPlate
            // 
            this.picInPlate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(238)))), ((int)(((byte)(240)))));
            this.picInPlate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picInPlate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picInPlate.Location = new System.Drawing.Point(3, 291);
            this.picInPlate.Name = "picInPlate";
            this.picInPlate.Size = new System.Drawing.Size(333, 203);
            this.picInPlate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picInPlate.TabIndex = 2;
            this.picInPlate.TabStop = false;
            // 
            // picInOverview
            // 
            this.picInOverview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(238)))), ((int)(((byte)(240)))));
            this.picInOverview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picInOverview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picInOverview.Location = new System.Drawing.Point(342, 291);
            this.picInOverview.Name = "picInOverview";
            this.picInOverview.Size = new System.Drawing.Size(333, 203);
            this.picInOverview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picInOverview.TabIndex = 3;
            this.picInOverview.TabStop = false;
            // 
            // grpOutLane
            // 
            this.grpOutLane.Controls.Add(this.tblOutCameras);
            this.grpOutLane.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOutLane.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpOutLane.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.grpOutLane.Location = new System.Drawing.Point(703, 7);
            this.grpOutLane.Name = "grpOutLane";
            this.grpOutLane.Padding = new System.Windows.Forms.Padding(6);
            this.grpOutLane.Size = new System.Drawing.Size(690, 535);
            this.grpOutLane.TabIndex = 1;
            this.grpOutLane.TabStop = false;
            this.grpOutLane.Text = "LÀN RA (OUT-LANE)";
            // 
            // tblOutCameras
            // 
            this.tblOutCameras.ColumnCount = 2;
            this.tblOutCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblOutCameras.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblOutCameras.Controls.Add(this.pnlOutPlateVideo, 0, 0);
            this.tblOutCameras.Controls.Add(this.pnlOutOverviewVideo, 1, 0);
            this.tblOutCameras.Controls.Add(this.picOutPlate, 0, 1);
            this.tblOutCameras.Controls.Add(this.picOutOverview, 1, 1);
            this.tblOutCameras.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblOutCameras.Location = new System.Drawing.Point(6, 32);
            this.tblOutCameras.Name = "tblOutCameras";
            this.tblOutCameras.RowCount = 2;
            this.tblOutCameras.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 58F));
            this.tblOutCameras.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 42F));
            this.tblOutCameras.Size = new System.Drawing.Size(678, 497);
            this.tblOutCameras.TabIndex = 0;
            // 
            // pnlOutPlateVideo
            // 
            this.pnlOutPlateVideo.BackColor = System.Drawing.Color.Black;
            this.pnlOutPlateVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOutPlateVideo.Location = new System.Drawing.Point(3, 3);
            this.pnlOutPlateVideo.Name = "pnlOutPlateVideo";
            this.pnlOutPlateVideo.Size = new System.Drawing.Size(333, 282);
            this.pnlOutPlateVideo.TabIndex = 0;
            this.pnlOutPlateVideo.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlOutPlateVideo_Paint);
            // 
            // pnlOutOverviewVideo
            // 
            this.pnlOutOverviewVideo.BackColor = System.Drawing.Color.Black;
            this.pnlOutOverviewVideo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlOutOverviewVideo.Location = new System.Drawing.Point(342, 3);
            this.pnlOutOverviewVideo.Name = "pnlOutOverviewVideo";
            this.pnlOutOverviewVideo.Size = new System.Drawing.Size(333, 282);
            this.pnlOutOverviewVideo.TabIndex = 1;
            this.pnlOutOverviewVideo.Paint += new System.Windows.Forms.PaintEventHandler(this.PnlOutOverviewVideo_Paint);
            // 
            // picOutPlate
            // 
            this.picOutPlate.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(238)))), ((int)(((byte)(240)))));
            this.picOutPlate.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picOutPlate.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picOutPlate.Location = new System.Drawing.Point(3, 291);
            this.picOutPlate.Name = "picOutPlate";
            this.picOutPlate.Size = new System.Drawing.Size(333, 203);
            this.picOutPlate.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picOutPlate.TabIndex = 2;
            this.picOutPlate.TabStop = false;
            // 
            // picOutOverview
            // 
            this.picOutOverview.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(235)))), ((int)(((byte)(238)))), ((int)(((byte)(240)))));
            this.picOutOverview.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.picOutOverview.Dock = System.Windows.Forms.DockStyle.Fill;
            this.picOutOverview.Location = new System.Drawing.Point(342, 291);
            this.picOutOverview.Name = "picOutOverview";
            this.picOutOverview.Size = new System.Drawing.Size(333, 203);
            this.picOutOverview.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.picOutOverview.TabIndex = 3;
            this.picOutOverview.TabStop = false;
            // 
            // grpInInfo
            // 
            this.grpInInfo.Controls.Add(this.tblInInfo);
            this.grpInInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpInInfo.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpInInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.grpInInfo.Location = new System.Drawing.Point(7, 548);
            this.grpInInfo.Name = "grpInInfo";
            this.grpInInfo.Padding = new System.Windows.Forms.Padding(8);
            this.grpInInfo.Size = new System.Drawing.Size(690, 226);
            this.grpInInfo.TabIndex = 2;
            this.grpInInfo.TabStop = false;
            this.grpInInfo.Text = "THÔNG TIN XE VÀO";
            // 
            // tblInInfo
            // 
            this.tblInInfo.ColumnCount = 4;
            this.tblInInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.tblInInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblInInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.tblInInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblInInfo.Controls.Add(this.lblInPlateTag, 0, 0);
            this.tblInInfo.Controls.Add(this.txtInPlate, 1, 0);
            this.tblInInfo.Controls.Add(this.lblInTimeTag, 2, 0);
            this.tblInInfo.Controls.Add(this.lblInTimeVal, 3, 0);
            this.tblInInfo.Controls.Add(this.lblInOwnerTag, 0, 1);
            this.tblInInfo.Controls.Add(this.lblInOwnerVal, 1, 1);
            this.tblInInfo.Controls.Add(this.lblInDeptTag, 2, 1);
            this.tblInInfo.Controls.Add(this.lblInDeptVal, 3, 1);
            this.tblInInfo.Controls.Add(this.lblInTypeTag, 0, 2);
            this.tblInInfo.Controls.Add(this.lblInTypeVal, 1, 2);
            this.tblInInfo.Controls.Add(this.lblInStatusTag, 2, 2);
            this.tblInInfo.Controls.Add(this.lblInStatusVal, 3, 2);
            this.tblInInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblInInfo.Location = new System.Drawing.Point(8, 34);
            this.tblInInfo.Name = "tblInInfo";
            this.tblInInfo.RowCount = 3;
            this.tblInInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblInInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.tblInInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tblInInfo.Size = new System.Drawing.Size(674, 184);
            this.tblInInfo.TabIndex = 0;
            // 
            // lblInPlateTag
            // 
            this.lblInPlateTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInPlateTag.AutoSize = true;
            this.lblInPlateTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblInPlateTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblInPlateTag.Location = new System.Drawing.Point(3, 7);
            this.lblInPlateTag.Name = "lblInPlateTag";
            this.lblInPlateTag.Size = new System.Drawing.Size(78, 50);
            this.lblInPlateTag.TabIndex = 0;
            this.lblInPlateTag.Text = "Biển số xe:";
            // 
            // txtInPlate
            // 
            this.txtInPlate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtInPlate.BackColor = System.Drawing.Color.White;
            this.txtInPlate.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.txtInPlate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(0)))), ((int)(((byte)(80)))), ((int)(((byte)(160)))));
            this.txtInPlate.Location = new System.Drawing.Point(108, 12);
            this.txtInPlate.Name = "txtInPlate";
            this.txtInPlate.ReadOnly = true;
            this.txtInPlate.Size = new System.Drawing.Size(226, 39);
            this.txtInPlate.TabIndex = 1;
            this.txtInPlate.Text = "---";
            this.txtInPlate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblInTimeTag
            // 
            this.lblInTimeTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInTimeTag.AutoSize = true;
            this.lblInTimeTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblInTimeTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblInTimeTag.Location = new System.Drawing.Point(340, 19);
            this.lblInTimeTag.Name = "lblInTimeTag";
            this.lblInTimeTag.Size = new System.Drawing.Size(93, 25);
            this.lblInTimeTag.TabIndex = 2;
            this.lblInTimeTag.Text = "Thời gian:";
            // 
            // lblInTimeVal
            // 
            this.lblInTimeVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInTimeVal.AutoSize = true;
            this.lblInTimeVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblInTimeVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblInTimeVal.Location = new System.Drawing.Point(445, 19);
            this.lblInTimeVal.Name = "lblInTimeVal";
            this.lblInTimeVal.Size = new System.Drawing.Size(36, 25);
            this.lblInTimeVal.TabIndex = 3;
            this.lblInTimeVal.Text = "---";
            // 
            // lblInOwnerTag
            // 
            this.lblInOwnerTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInOwnerTag.AutoSize = true;
            this.lblInOwnerTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblInOwnerTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblInOwnerTag.Location = new System.Drawing.Point(3, 80);
            this.lblInOwnerTag.Name = "lblInOwnerTag";
            this.lblInOwnerTag.Size = new System.Drawing.Size(72, 25);
            this.lblInOwnerTag.TabIndex = 4;
            this.lblInOwnerTag.Text = "Chủ xe:";
            // 
            // lblInOwnerVal
            // 
            this.lblInOwnerVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInOwnerVal.AutoSize = true;
            this.lblInOwnerVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblInOwnerVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblInOwnerVal.Location = new System.Drawing.Point(108, 80);
            this.lblInOwnerVal.Name = "lblInOwnerVal";
            this.lblInOwnerVal.Size = new System.Drawing.Size(36, 25);
            this.lblInOwnerVal.TabIndex = 5;
            this.lblInOwnerVal.Text = "---";
            // 
            // lblInDeptTag
            // 
            this.lblInDeptTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInDeptTag.AutoSize = true;
            this.lblInDeptTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblInDeptTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblInDeptTag.Location = new System.Drawing.Point(340, 80);
            this.lblInDeptTag.Name = "lblInDeptTag";
            this.lblInDeptTag.Size = new System.Drawing.Size(70, 25);
            this.lblInDeptTag.TabIndex = 6;
            this.lblInDeptTag.Text = "Đơn vị:";
            // 
            // lblInDeptVal
            // 
            this.lblInDeptVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInDeptVal.AutoSize = true;
            this.lblInDeptVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblInDeptVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblInDeptVal.Location = new System.Drawing.Point(445, 80);
            this.lblInDeptVal.Name = "lblInDeptVal";
            this.lblInDeptVal.Size = new System.Drawing.Size(36, 25);
            this.lblInDeptVal.TabIndex = 7;
            this.lblInDeptVal.Text = "---";
            // 
            // lblInTypeTag
            // 
            this.lblInTypeTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInTypeTag.AutoSize = true;
            this.lblInTypeTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblInTypeTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblInTypeTag.Location = new System.Drawing.Point(3, 140);
            this.lblInTypeTag.Name = "lblInTypeTag";
            this.lblInTypeTag.Size = new System.Drawing.Size(74, 25);
            this.lblInTypeTag.TabIndex = 8;
            this.lblInTypeTag.Text = "Loại xe:";
            // 
            // lblInTypeVal
            // 
            this.lblInTypeVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInTypeVal.AutoSize = true;
            this.lblInTypeVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblInTypeVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblInTypeVal.Location = new System.Drawing.Point(108, 140);
            this.lblInTypeVal.Name = "lblInTypeVal";
            this.lblInTypeVal.Size = new System.Drawing.Size(36, 25);
            this.lblInTypeVal.TabIndex = 9;
            this.lblInTypeVal.Text = "---";
            // 
            // lblInStatusTag
            // 
            this.lblInStatusTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInStatusTag.AutoSize = true;
            this.lblInStatusTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblInStatusTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblInStatusTag.Location = new System.Drawing.Point(340, 140);
            this.lblInStatusTag.Name = "lblInStatusTag";
            this.lblInStatusTag.Size = new System.Drawing.Size(99, 25);
            this.lblInStatusTag.TabIndex = 10;
            this.lblInStatusTag.Text = "Trạng thái:";
            // 
            // lblInStatusVal
            // 
            this.lblInStatusVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblInStatusVal.AutoSize = true;
            this.lblInStatusVal.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblInStatusVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(70)))));
            this.lblInStatusVal.Location = new System.Drawing.Point(445, 140);
            this.lblInStatusVal.Name = "lblInStatusVal";
            this.lblInStatusVal.Size = new System.Drawing.Size(159, 25);
            this.lblInStatusVal.TabIndex = 11;
            this.lblInStatusVal.Text = "Sẵn sàng đón xe";
            // 
            // grpOutInfo
            // 
            this.grpOutInfo.Controls.Add(this.tblOutInfo);
            this.grpOutInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grpOutInfo.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.grpOutInfo.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(45)))), ((int)(((byte)(52)))), ((int)(((byte)(58)))));
            this.grpOutInfo.Location = new System.Drawing.Point(703, 548);
            this.grpOutInfo.Name = "grpOutInfo";
            this.grpOutInfo.Padding = new System.Windows.Forms.Padding(8);
            this.grpOutInfo.Size = new System.Drawing.Size(690, 226);
            this.grpOutInfo.TabIndex = 3;
            this.grpOutInfo.TabStop = false;
            this.grpOutInfo.Text = "THÔNG TIN XE RA";
            // 
            // tblOutInfo
            // 
            this.tblOutInfo.ColumnCount = 4;
            this.tblOutInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.tblOutInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblOutInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 105F));
            this.tblOutInfo.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 50F));
            this.tblOutInfo.Controls.Add(this.lblOutPlateTag, 0, 0);
            this.tblOutInfo.Controls.Add(this.txtOutPlate, 1, 0);
            this.tblOutInfo.Controls.Add(this.lblOutTimeTag, 2, 0);
            this.tblOutInfo.Controls.Add(this.lblOutTimeVal, 3, 0);
            this.tblOutInfo.Controls.Add(this.lblOutOwnerTag, 0, 1);
            this.tblOutInfo.Controls.Add(this.lblOutOwnerVal, 1, 1);
            this.tblOutInfo.Controls.Add(this.lblOutDeptTag, 2, 1);
            this.tblOutInfo.Controls.Add(this.lblOutDeptVal, 3, 1);
            this.tblOutInfo.Controls.Add(this.lblOutTypeTag, 0, 2);
            this.tblOutInfo.Controls.Add(this.lblOutTypeVal, 1, 2);
            this.tblOutInfo.Controls.Add(this.lblOutStatusTag, 2, 2);
            this.tblOutInfo.Controls.Add(this.lblOutStatusVal, 3, 2);
            this.tblOutInfo.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tblOutInfo.Location = new System.Drawing.Point(8, 34);
            this.tblOutInfo.Name = "tblOutInfo";
            this.tblOutInfo.RowCount = 3;
            this.tblOutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 35F));
            this.tblOutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 32F));
            this.tblOutInfo.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 33F));
            this.tblOutInfo.Size = new System.Drawing.Size(674, 184);
            this.tblOutInfo.TabIndex = 1;
            // 
            // lblOutPlateTag
            // 
            this.lblOutPlateTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutPlateTag.AutoSize = true;
            this.lblOutPlateTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblOutPlateTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblOutPlateTag.Location = new System.Drawing.Point(3, 7);
            this.lblOutPlateTag.Name = "lblOutPlateTag";
            this.lblOutPlateTag.Size = new System.Drawing.Size(78, 50);
            this.lblOutPlateTag.TabIndex = 0;
            this.lblOutPlateTag.Text = "Biển số xe:";
            // 
            // txtOutPlate
            // 
            this.txtOutPlate.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.txtOutPlate.BackColor = System.Drawing.Color.White;
            this.txtOutPlate.Font = new System.Drawing.Font("Segoe UI", 12F, System.Drawing.FontStyle.Bold);
            this.txtOutPlate.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(180)))), ((int)(((byte)(40)))), ((int)(((byte)(40)))));
            this.txtOutPlate.Location = new System.Drawing.Point(108, 12);
            this.txtOutPlate.Name = "txtOutPlate";
            this.txtOutPlate.ReadOnly = true;
            this.txtOutPlate.Size = new System.Drawing.Size(226, 39);
            this.txtOutPlate.TabIndex = 1;
            this.txtOutPlate.Text = "---";
            this.txtOutPlate.TextAlign = System.Windows.Forms.HorizontalAlignment.Center;
            // 
            // lblOutTimeTag
            // 
            this.lblOutTimeTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutTimeTag.AutoSize = true;
            this.lblOutTimeTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblOutTimeTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblOutTimeTag.Location = new System.Drawing.Point(340, 19);
            this.lblOutTimeTag.Name = "lblOutTimeTag";
            this.lblOutTimeTag.Size = new System.Drawing.Size(93, 25);
            this.lblOutTimeTag.TabIndex = 2;
            this.lblOutTimeTag.Text = "Thời gian:";
            // 
            // lblOutTimeVal
            // 
            this.lblOutTimeVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutTimeVal.AutoSize = true;
            this.lblOutTimeVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblOutTimeVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblOutTimeVal.Location = new System.Drawing.Point(445, 19);
            this.lblOutTimeVal.Name = "lblOutTimeVal";
            this.lblOutTimeVal.Size = new System.Drawing.Size(36, 25);
            this.lblOutTimeVal.TabIndex = 3;
            this.lblOutTimeVal.Text = "---";
            // 
            // lblOutOwnerTag
            // 
            this.lblOutOwnerTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutOwnerTag.AutoSize = true;
            this.lblOutOwnerTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblOutOwnerTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblOutOwnerTag.Location = new System.Drawing.Point(3, 80);
            this.lblOutOwnerTag.Name = "lblOutOwnerTag";
            this.lblOutOwnerTag.Size = new System.Drawing.Size(72, 25);
            this.lblOutOwnerTag.TabIndex = 4;
            this.lblOutOwnerTag.Text = "Chủ xe:";
            // 
            // lblOutOwnerVal
            // 
            this.lblOutOwnerVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutOwnerVal.AutoSize = true;
            this.lblOutOwnerVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblOutOwnerVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblOutOwnerVal.Location = new System.Drawing.Point(108, 80);
            this.lblOutOwnerVal.Name = "lblOutOwnerVal";
            this.lblOutOwnerVal.Size = new System.Drawing.Size(36, 25);
            this.lblOutOwnerVal.TabIndex = 5;
            this.lblOutOwnerVal.Text = "---";
            // 
            // lblOutDeptTag
            // 
            this.lblOutDeptTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutDeptTag.AutoSize = true;
            this.lblOutDeptTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblOutDeptTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblOutDeptTag.Location = new System.Drawing.Point(340, 80);
            this.lblOutDeptTag.Name = "lblOutDeptTag";
            this.lblOutDeptTag.Size = new System.Drawing.Size(70, 25);
            this.lblOutDeptTag.TabIndex = 6;
            this.lblOutDeptTag.Text = "Đơn vị:";
            // 
            // lblOutDeptVal
            // 
            this.lblOutDeptVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutDeptVal.AutoSize = true;
            this.lblOutDeptVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblOutDeptVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblOutDeptVal.Location = new System.Drawing.Point(445, 80);
            this.lblOutDeptVal.Name = "lblOutDeptVal";
            this.lblOutDeptVal.Size = new System.Drawing.Size(36, 25);
            this.lblOutDeptVal.TabIndex = 7;
            this.lblOutDeptVal.Text = "---";
            // 
            // lblOutTypeTag
            // 
            this.lblOutTypeTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutTypeTag.AutoSize = true;
            this.lblOutTypeTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblOutTypeTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblOutTypeTag.Location = new System.Drawing.Point(3, 140);
            this.lblOutTypeTag.Name = "lblOutTypeTag";
            this.lblOutTypeTag.Size = new System.Drawing.Size(74, 25);
            this.lblOutTypeTag.TabIndex = 8;
            this.lblOutTypeTag.Text = "Loại xe:";
            // 
            // lblOutTypeVal
            // 
            this.lblOutTypeVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutTypeVal.AutoSize = true;
            this.lblOutTypeVal.Font = new System.Drawing.Font("Segoe UI", 9.5F);
            this.lblOutTypeVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(30)))), ((int)(((byte)(35)))), ((int)(((byte)(40)))));
            this.lblOutTypeVal.Location = new System.Drawing.Point(108, 140);
            this.lblOutTypeVal.Name = "lblOutTypeVal";
            this.lblOutTypeVal.Size = new System.Drawing.Size(36, 25);
            this.lblOutTypeVal.TabIndex = 9;
            this.lblOutTypeVal.Text = "---";
            // 
            // lblOutStatusTag
            // 
            this.lblOutStatusTag.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutStatusTag.AutoSize = true;
            this.lblOutStatusTag.Font = new System.Drawing.Font("Segoe UI Semibold", 9F);
            this.lblOutStatusTag.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(80)))), ((int)(((byte)(90)))), ((int)(((byte)(100)))));
            this.lblOutStatusTag.Location = new System.Drawing.Point(340, 140);
            this.lblOutStatusTag.Name = "lblOutStatusTag";
            this.lblOutStatusTag.Size = new System.Drawing.Size(99, 25);
            this.lblOutStatusTag.TabIndex = 10;
            this.lblOutStatusTag.Text = "Trạng thái:";
            // 
            // lblOutStatusVal
            // 
            this.lblOutStatusVal.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.lblOutStatusVal.AutoSize = true;
            this.lblOutStatusVal.Font = new System.Drawing.Font("Segoe UI", 9.5F, System.Drawing.FontStyle.Bold);
            this.lblOutStatusVal.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(40)))), ((int)(((byte)(140)))), ((int)(((byte)(70)))));
            this.lblOutStatusVal.Location = new System.Drawing.Point(445, 140);
            this.lblOutStatusVal.Name = "lblOutStatusVal";
            this.lblOutStatusVal.Size = new System.Drawing.Size(159, 25);
            this.lblOutStatusVal.TabIndex = 11;
            this.lblOutStatusVal.Text = "Sẵn sàng đón xe";
            // 
            // statusStrip
            // 
            this.statusStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblFooterStatus});
            this.statusStrip.Location = new System.Drawing.Point(0, 829);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(1400, 32);
            this.statusStrip.TabIndex = 2;
            this.statusStrip.Text = "statusStrip1";
            // 
            // lblFooterStatus
            // 
            this.lblFooterStatus.Font = new System.Drawing.Font("Segoe UI", 9F);
            this.lblFooterStatus.ForeColor = System.Drawing.Color.FromArgb(((int)(((byte)(70)))), ((int)(((byte)(80)))), ((int)(((byte)(90)))));
            this.lblFooterStatus.Name = "lblFooterStatus";
            this.lblFooterStatus.Size = new System.Drawing.Size(157, 25);
            this.lblFooterStatus.Text = "Sẵn sàng làm việc.";
            // 
            // timerClock
            // 
            this.timerClock.Enabled = true;
            this.timerClock.Interval = 1000;
            this.timerClock.Tick += new System.EventHandler(this.TimerClock_Tick);
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 25F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(245)))), ((int)(((byte)(247)))), ((int)(((byte)(249)))));
            this.ClientSize = new System.Drawing.Size(1400, 861);
            this.Controls.Add(this.tblMainLayout);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.pnlHeader);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Name = "FrmMain";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "Hệ thống Quản lý Bãi xe - Thái Thụy";
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FrmMain_FormClosing);
            this.Load += new System.EventHandler(this.FrmMain_Load);
            this.Shown += new System.EventHandler(this.FrmMain_Shown);
            this.pnlHeader.ResumeLayout(false);
            this.pnlHeader.PerformLayout();
            this.tblMainLayout.ResumeLayout(false);
            this.grpInLane.ResumeLayout(false);
            this.tblInCameras.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picInPlate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picInOverview)).EndInit();
            this.grpOutLane.ResumeLayout(false);
            this.tblOutCameras.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.picOutPlate)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.picOutOverview)).EndInit();
            this.grpInInfo.ResumeLayout(false);
            this.tblInInfo.ResumeLayout(false);
            this.tblInInfo.PerformLayout();
            this.grpOutInfo.ResumeLayout(false);
            this.tblOutInfo.ResumeLayout(false);
            this.tblOutInfo.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Panel pnlHeader;
        private System.Windows.Forms.Label lblAppTitle;
        private System.Windows.Forms.Label lblSystemStatus;
        private System.Windows.Forms.Label lblClock;
        private System.Windows.Forms.TableLayoutPanel tblMainLayout;
        private System.Windows.Forms.GroupBox grpInLane;
        private System.Windows.Forms.TableLayoutPanel tblInCameras;
        private System.Windows.Forms.Panel pnlInPlateVideo;
        private System.Windows.Forms.Panel pnlInOverviewVideo;
        private System.Windows.Forms.PictureBox picInPlate;
        private System.Windows.Forms.PictureBox picInOverview;
        private System.Windows.Forms.GroupBox grpOutLane;
        private System.Windows.Forms.TableLayoutPanel tblOutCameras;
        private System.Windows.Forms.Panel pnlOutPlateVideo;
        private System.Windows.Forms.Panel pnlOutOverviewVideo;
        private System.Windows.Forms.PictureBox picOutPlate;
        private System.Windows.Forms.PictureBox picOutOverview;
        private System.Windows.Forms.GroupBox grpInInfo;
        private System.Windows.Forms.TableLayoutPanel tblInInfo;
        private System.Windows.Forms.Label lblInPlateTag;
        private System.Windows.Forms.TextBox txtInPlate;
        private System.Windows.Forms.Label lblInTimeTag;
        private System.Windows.Forms.Label lblInTimeVal;
        private System.Windows.Forms.Label lblInOwnerTag;
        private System.Windows.Forms.Label lblInOwnerVal;
        private System.Windows.Forms.Label lblInDeptTag;
        private System.Windows.Forms.Label lblInDeptVal;
        private System.Windows.Forms.Label lblInTypeTag;
        private System.Windows.Forms.Label lblInTypeVal;
        private System.Windows.Forms.Label lblInStatusTag;
        private System.Windows.Forms.Label lblInStatusVal;
        private System.Windows.Forms.GroupBox grpOutInfo;
        private System.Windows.Forms.TableLayoutPanel tblOutInfo;
        private System.Windows.Forms.Label lblOutPlateTag;
        private System.Windows.Forms.TextBox txtOutPlate;
        private System.Windows.Forms.Label lblOutTimeTag;
        private System.Windows.Forms.Label lblOutTimeVal;
        private System.Windows.Forms.Label lblOutOwnerTag;
        private System.Windows.Forms.Label lblOutOwnerVal;
        private System.Windows.Forms.Label lblOutDeptTag;
        private System.Windows.Forms.Label lblOutDeptVal;
        private System.Windows.Forms.Label lblOutTypeTag;
        private System.Windows.Forms.Label lblOutTypeVal;
        private System.Windows.Forms.Label lblOutStatusTag;
        private System.Windows.Forms.Label lblOutStatusVal;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel lblFooterStatus;
        private System.Windows.Forms.Timer timerClock;
    }
}
