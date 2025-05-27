using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Domain.Models;
using Types;

namespace HabitTrackerGUI
{
    public partial class AddHabitForm : Form
    {
        private List<PrefFixedTime> _timings = new List<PrefFixedTime>();
        private Guid _habitguid;

        public AddHabitForm()
        {
            InitializeComponent();
            cmbTimeOption.SelectedIndex = 0;
            UpdateTimeInputsVisibility();
            _habitguid = Guid.NewGuid();
        }

        private void cmbTimeOption_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateTimeInputsVisibility();
        }

        private void UpdateTimeInputsVisibility()
        {
            var option = (TimeOption)cmbTimeOption.SelectedIndex;
            pnlTimeIntervals.Visible = option == TimeOption.Preffered || option == TimeOption.Fixed;
        }

        private void btnAddTimeInterval_Click(object sender, EventArgs e)
        {
            try
            {
                var start = TimeOnly.Parse(txtStartTime.Text);
                var end = TimeOnly.Parse(txtEndTime.Text);

                _timings.Add(new PrefFixedTime(
                    Guid.NewGuid(),
                    start,
                    end,
                    _habitguid));

                lstTimeIntervals.Items.Add($"{start} - {end}");
                txtStartTime.Clear();
                txtEndTime.Clear();
            }
            catch (Exception)
            {
                MessageBox.Show("Некорректный формат времени. Используйте HH:mm", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnRemoveTimeInterval_Click(object sender, EventArgs e)
        {
            if (lstTimeIntervals.SelectedIndex >= 0)
            {
                _timings.RemoveAt(lstTimeIntervals.SelectedIndex);
                lstTimeIntervals.Items.RemoveAt(lstTimeIntervals.SelectedIndex);
            }
        }

        public Habit GetHabit(string userName)
        {
            if (string.IsNullOrWhiteSpace(txtName.Text))
                throw new Exception("Введите название привычки");

            if (!int.TryParse(txtMinutes.Text, out int minutes) || minutes <= 0)
                throw new Exception("Введите корректное количество минут");

            if (!int.TryParse(txtDaysPerWeek.Text, out int days) || days < 1 || days > 7)
                throw new Exception("Введите корректное количество дней (1-7)");

            var option = (TimeOption)cmbTimeOption.SelectedIndex;

            if ((option == TimeOption.Preffered || option == TimeOption.Fixed) && _timings.Count == 0)
                throw new Exception("Для выбранного типа привычки необходимо указать хотя бы один временной интервал");

            return new Habit(
                _habitguid,
                txtName.Text,
                minutes,
                option,
                userName,
                [],
                _timings,
                days);
        }

        private void btnOK_Click(object sender, EventArgs e)
        {
            try
            {
                GetHabit(""); // Валидация
                this.DialogResult = DialogResult.OK;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void InitializeComponent()
        {
            this.lblName = new System.Windows.Forms.Label();
            this.txtName = new System.Windows.Forms.TextBox();
            this.lblMinutes = new System.Windows.Forms.Label();
            this.txtMinutes = new System.Windows.Forms.TextBox();
            this.lblTimeOption = new System.Windows.Forms.Label();
            this.cmbTimeOption = new System.Windows.Forms.ComboBox();
            this.pnlTimeIntervals = new System.Windows.Forms.Panel();
            this.lblStartTime = new System.Windows.Forms.Label();
            this.txtStartTime = new System.Windows.Forms.TextBox();
            this.lblEndTime = new System.Windows.Forms.Label();
            this.txtEndTime = new System.Windows.Forms.TextBox();
            this.btnAddTimeInterval = new System.Windows.Forms.Button();
            this.btnRemoveTimeInterval = new System.Windows.Forms.Button();
            this.lstTimeIntervals = new System.Windows.Forms.ListBox();
            this.lblDaysPerWeek = new System.Windows.Forms.Label();
            this.txtDaysPerWeek = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.pnlTimeIntervals.SuspendLayout();
            this.SuspendLayout();

            // lblName
            this.lblName.AutoSize = true;
            this.lblName.Location = new System.Drawing.Point(20, 20);
            this.lblName.Name = "lblName";
            this.lblName.Size = new System.Drawing.Size(60, 13);
            this.lblName.Text = "Название:";

            // txtName
            this.txtName.Location = new System.Drawing.Point(120, 17);
            this.txtName.Name = "txtName";
            this.txtName.Size = new System.Drawing.Size(200, 20);

            // lblMinutes
            this.lblMinutes.AutoSize = true;
            this.lblMinutes.Location = new System.Drawing.Point(20, 50);
            this.lblMinutes.Name = "lblMinutes";
            this.lblMinutes.Size = new System.Drawing.Size(95, 13);
            this.lblMinutes.Text = "Минут в день:";

            // txtMinutes
            this.txtMinutes.Location = new System.Drawing.Point(120, 47);
            this.txtMinutes.Name = "txtMinutes";
            this.txtMinutes.Size = new System.Drawing.Size(50, 20);

            // lblTimeOption
            this.lblTimeOption.AutoSize = true;
            this.lblTimeOption.Location = new System.Drawing.Point(20, 80);
            this.lblTimeOption.Name = "lblTimeOption";
            this.lblTimeOption.Size = new System.Drawing.Size(79, 13);
            this.lblTimeOption.Text = "Тип привычки:";

            // cmbTimeOption
            this.cmbTimeOption.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbTimeOption.FormattingEnabled = true;
            this.cmbTimeOption.Items.AddRange(new object[] {
                "Безразличное время",
                "Предпочтительное время",
                "Фиксированное время"});
            this.cmbTimeOption.Location = new System.Drawing.Point(120, 77);
            this.cmbTimeOption.Name = "cmbTimeOption";
            this.cmbTimeOption.Size = new System.Drawing.Size(200, 21);
            this.cmbTimeOption.SelectedIndexChanged += new System.EventHandler(this.cmbTimeOption_SelectedIndexChanged);

            // pnlTimeIntervals
            this.pnlTimeIntervals.Controls.Add(this.lblStartTime);
            this.pnlTimeIntervals.Controls.Add(this.txtStartTime);
            this.pnlTimeIntervals.Controls.Add(this.lblEndTime);
            this.pnlTimeIntervals.Controls.Add(this.txtEndTime);
            this.pnlTimeIntervals.Controls.Add(this.btnAddTimeInterval);
            this.pnlTimeIntervals.Controls.Add(this.btnRemoveTimeInterval);
            this.pnlTimeIntervals.Controls.Add(this.lstTimeIntervals);
            this.pnlTimeIntervals.Location = new System.Drawing.Point(20, 110);
            this.pnlTimeIntervals.Name = "pnlTimeIntervals";
            this.pnlTimeIntervals.Size = new System.Drawing.Size(300, 150);

            // lblStartTime
            this.lblStartTime.AutoSize = true;
            this.lblStartTime.Location = new System.Drawing.Point(3, 5);
            this.lblStartTime.Name = "lblStartTime";
            this.lblStartTime.Size = new System.Drawing.Size(44, 13);
            this.lblStartTime.Text = "Начало:";

            // txtStartTime
            this.txtStartTime.Location = new System.Drawing.Point(50, 2);
            this.txtStartTime.Name = "txtStartTime";
            this.txtStartTime.Size = new System.Drawing.Size(50, 20);
            this.txtStartTime.Text = "08:00";

            // lblEndTime
            this.lblEndTime.AutoSize = true;
            this.lblEndTime.Location = new System.Drawing.Point(110, 5);
            this.lblEndTime.Name = "lblEndTime";
            this.lblEndTime.Size = new System.Drawing.Size(38, 13);
            this.lblEndTime.Text = "Конец:";

            // txtEndTime
            this.txtEndTime.Location = new System.Drawing.Point(150, 2);
            this.txtEndTime.Name = "txtEndTime";
            this.txtEndTime.Size = new System.Drawing.Size(50, 20);
            this.txtEndTime.Text = "09:00";

            // btnAddTimeInterval
            this.btnAddTimeInterval.Location = new System.Drawing.Point(210, 0);
            this.btnAddTimeInterval.Name = "btnAddTimeInterval";
            this.btnAddTimeInterval.Size = new System.Drawing.Size(75, 23);
            this.btnAddTimeInterval.Text = "Добавить";
            this.btnAddTimeInterval.Click += new System.EventHandler(this.btnAddTimeInterval_Click);

            // btnRemoveTimeInterval
            this.btnRemoveTimeInterval.Location = new System.Drawing.Point(210, 30);
            this.btnRemoveTimeInterval.Name = "btnRemoveTimeInterval";
            this.btnRemoveTimeInterval.Size = new System.Drawing.Size(75, 23);
            this.btnRemoveTimeInterval.Text = "Удалить";
            this.btnRemoveTimeInterval.Click += new System.EventHandler(this.btnRemoveTimeInterval_Click);

            // lstTimeIntervals
            this.lstTimeIntervals.FormattingEnabled = true;
            this.lstTimeIntervals.Location = new System.Drawing.Point(3, 30);
            this.lstTimeIntervals.Name = "lstTimeIntervals";
            this.lstTimeIntervals.Size = new System.Drawing.Size(200, 95);

            // lblDaysPerWeek
            this.lblDaysPerWeek.AutoSize = true;
            this.lblDaysPerWeek.Location = new System.Drawing.Point(20, 270);
            this.lblDaysPerWeek.Name = "lblDaysPerWeek";
            this.lblDaysPerWeek.Size = new System.Drawing.Size(95, 13);
            this.lblDaysPerWeek.Text = "Дней в неделю:";

            // txtDaysPerWeek
            this.txtDaysPerWeek.Location = new System.Drawing.Point(120, 267);
            this.txtDaysPerWeek.Name = "txtDaysPerWeek";
            this.txtDaysPerWeek.Size = new System.Drawing.Size(50, 20);
            this.txtDaysPerWeek.Text = "7";

            // btnOK
            this.btnOK.Location = new System.Drawing.Point(80, 300);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(100, 30);
            this.btnOK.Text = "ОК";
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);

            // btnCancel
            this.btnCancel.Location = new System.Drawing.Point(200, 300);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(100, 30);
            this.btnCancel.Text = "Отмена";
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);

            // AddHabitForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(350, 350);
            this.Controls.Add(this.lblName);
            this.Controls.Add(this.txtName);
            this.Controls.Add(this.lblMinutes);
            this.Controls.Add(this.txtMinutes);
            this.Controls.Add(this.lblTimeOption);
            this.Controls.Add(this.cmbTimeOption);
            this.Controls.Add(this.pnlTimeIntervals);
            this.Controls.Add(this.lblDaysPerWeek);
            this.Controls.Add(this.txtDaysPerWeek);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.btnCancel);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AddHabitForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Добавить привычку";
            this.pnlTimeIntervals.ResumeLayout(false);
            this.pnlTimeIntervals.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblName;
        private System.Windows.Forms.TextBox txtName;
        private System.Windows.Forms.Label lblMinutes;
        private System.Windows.Forms.TextBox txtMinutes;
        private System.Windows.Forms.Label lblTimeOption;
        private System.Windows.Forms.ComboBox cmbTimeOption;
        private System.Windows.Forms.Panel pnlTimeIntervals;
        private System.Windows.Forms.Label lblStartTime;
        private System.Windows.Forms.TextBox txtStartTime;
        private System.Windows.Forms.Label lblEndTime;
        private System.Windows.Forms.TextBox txtEndTime;
        private System.Windows.Forms.Button btnAddTimeInterval;
        private System.Windows.Forms.Button btnRemoveTimeInterval;
        private System.Windows.Forms.ListBox lstTimeIntervals;
        private System.Windows.Forms.Label lblDaysPerWeek;
        private System.Windows.Forms.TextBox txtDaysPerWeek;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
    }
}