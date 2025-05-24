using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Domain.Models;

namespace HabitTrackerGUI
{
    public partial class NotificationTimesForm : Form
    {
        private List<SettingsTime> _times;

        public NotificationTimesForm(List<SettingsTime> initialTimes)
        {
            InitializeComponent();
            _times = new List<SettingsTime>(initialTimes);
            UpdateTimesList();
        }

        private void UpdateTimesList()
        {
            lstTimes.Items.Clear();
            foreach (var time in _times)
            {
                lstTimes.Items.Add($"{time.Start} - {time.End}");
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            try
            {
                var start = TimeOnly.Parse(txtStart.Text);
                var end = TimeOnly.Parse(txtEnd.Text);

                _times.Add(new SettingsTime(
                    Guid.NewGuid(),
                    start,
                    end,
                    Guid.NewGuid()));

                UpdateTimesList();
                txtStart.Clear();
                txtEnd.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show("Некорректный формат времени. Используйте HH:mm", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            if (lstTimes.SelectedIndex >= 0)
            {
                _times.RemoveAt(lstTimes.SelectedIndex);
                UpdateTimesList();
            }
        }

        public List<SettingsTime> GetTimes()
        {
            return _times;
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.lblTitle = new System.Windows.Forms.Label();
            this.lblStart = new System.Windows.Forms.Label();
            this.txtStart = new System.Windows.Forms.TextBox();
            this.lblEnd = new System.Windows.Forms.Label();
            this.txtEnd = new System.Windows.Forms.TextBox();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnRemove = new System.Windows.Forms.Button();
            this.lstTimes = new System.Windows.Forms.ListBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.SuspendLayout();

            // lblTitle
            this.lblTitle.AutoSize = true;
            this.lblTitle.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Bold);
            this.lblTitle.Location = new System.Drawing.Point(20, 20);
            this.lblTitle.Name = "lblTitle";
            this.lblTitle.Size = new System.Drawing.Size(300, 17);
            this.lblTitle.Text = "Запрещенные интервалы для уведомлений";

            // lblStart
            this.lblStart.AutoSize = true;
            this.lblStart.Location = new System.Drawing.Point(20, 50);
            this.lblStart.Name = "lblStart";
            this.lblStart.Size = new System.Drawing.Size(44, 13);
            this.lblStart.Text = "Начало:";

            // txtStart
            this.txtStart.Location = new System.Drawing.Point(70, 47);
            this.txtStart.Name = "txtStart";
            this.txtStart.Size = new System.Drawing.Size(50, 20);
            this.txtStart.Text = "22:00";

            // lblEnd
            this.lblEnd.AutoSize = true;
            this.lblEnd.Location = new System.Drawing.Point(130, 50);
            this.lblEnd.Name = "lblEnd";
            this.lblEnd.Size = new System.Drawing.Size(38, 13);
            this.lblEnd.Text = "Конец:";

            // txtEnd
            this.txtEnd.Location = new System.Drawing.Point(180, 47);
            this.txtEnd.Name = "txtEnd";
            this.txtEnd.Size = new System.Drawing.Size(50, 20);
            this.txtEnd.Text = "08:00";

            // btnAdd
            this.btnAdd.Location = new System.Drawing.Point(240, 45);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.Text = "Добавить";
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);

            // btnRemove
            this.btnRemove.Location = new System.Drawing.Point(240, 75);
            this.btnRemove.Name = "btnRemove";
            this.btnRemove.Size = new System.Drawing.Size(75, 23);
            this.btnRemove.Text = "Удалить";
            this.btnRemove.Click += new System.EventHandler(this.btnRemove_Click);

            // lstTimes
            this.lstTimes.FormattingEnabled = true;
            this.lstTimes.Location = new System.Drawing.Point(20, 80);
            this.lstTimes.Name = "lstTimes";
            this.lstTimes.Size = new System.Drawing.Size(200, 160);

            // btnOK
            this.btnOK.Location = new System.Drawing.Point(80, 250);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 30);
            this.btnOK.Text = "ОК";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(200, 250);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.Text = "Отмена";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // NotificationTimesForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 300);
            this.Controls.Add(this.lblTitle);
            this.Controls.Add(this.lblStart);
            this.Controls.Add(this.txtStart);
            this.Controls.Add(this.lblEnd);
            this.Controls.Add(this.txtEnd);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnRemove);
            this.Controls.Add(this.lstTimes);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "NotificationTimesForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Настройка времени уведомлений";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblTitle;
        private System.Windows.Forms.Label lblStart;
        private System.Windows.Forms.TextBox txtStart;
        private System.Windows.Forms.Label lblEnd;
        private System.Windows.Forms.TextBox txtEnd;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnRemove;
        private System.Windows.Forms.ListBox lstTimes;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}