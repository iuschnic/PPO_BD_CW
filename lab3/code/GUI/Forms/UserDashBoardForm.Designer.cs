using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Domain;
using Domain.InPorts;
using Domain.Models;

namespace HabitTrackerGUI
{
    public partial class UserDashboardForm : Form
    {
        private readonly ITaskTracker _taskService;
        private User _user;
        private bool _isInitializing = true;

        public UserDashboardForm(ITaskTracker taskService, User user)
        {
            _taskService = taskService;
            _user = user;
            InitializeComponent();
            _isInitializing = true;
            UpdateUserInfo();
            LoadData();
            _isInitializing = false;
        }

        private void UpdateUserInfo()
        {
            lblUsername.Text = $"Пользователь: {_user.NameID}";
            lblPhone.Text = $"Телефон: {_user.Number}";
            chkNotifications.Checked = _user.Settings.NotifyOn;
        }

        private void LoadData()
        {
            LoadHabits();
            LoadEvents();
        }

        private void LoadHabits()
        {
            lstHabits.Items.Clear();
            foreach (var habit in _user.Habits)
            {
                lstHabits.Items.Add($"Привычка: {habit.Name}, {habit.CountInWeek} дней в неделю, {habit.MinsToComplete} мин, {habit.Option}");
                foreach (var timing in habit.PrefFixedTimings)
                    lstHabits.Items.Add($"    Предпочит/Фикс время выполнения: {timing.Start} - {timing.End}");
            }
        }

        private void LoadEvents()
        {
            lstEvents.Items.Clear();

            // Группируем события по дню недели
            var eventsByDay = new Dictionary<DayOfWeek, List<Event>>();

            foreach (var ev in _user.Events)
            {
                if (!eventsByDay.ContainsKey(ev.Day))
                {
                    eventsByDay[ev.Day] = new List<Event>();
                }
                eventsByDay[ev.Day].Add(ev);
            }

            // Сортируем дни недели
            var sortedDays = Enum.GetValues(typeof(DayOfWeek)).Cast<DayOfWeek>();

            foreach (var day in sortedDays)
            {
                if (eventsByDay.TryGetValue(day, out var dayEvents))
                {
                    lstEvents.Items.Add($"[{GetDayName(day)}]");

                    foreach (var ev in dayEvents.OrderBy(e => e.Start))
                    {
                        lstEvents.Items.Add($"   {ev.Name}: {ev.Start}-{ev.End}");
                    }

                    lstEvents.Items.Add(""); // Пустая строка между днями
                }
            }
        }

        private string GetDayName(DayOfWeek day)
        {
            return day switch
            {
                DayOfWeek.Monday => "Понедельник",
                DayOfWeek.Tuesday => "Вторник",
                DayOfWeek.Wednesday => "Среда",
                DayOfWeek.Thursday => "Четверг",
                DayOfWeek.Friday => "Пятница",
                DayOfWeek.Saturday => "Суббота",
                DayOfWeek.Sunday => "Воскресенье",
                _ => day.ToString()
            };
        }

        private void btnImportSchedule_Click(object sender, EventArgs e)
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "csv файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                Title = "Выберите файл расписания"
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var result = _taskService.ImportNewShedule(_user.NameID, openFileDialog.FileName);
                    if (result == null)
                    {
                        MessageBox.Show("Ошибка при импорте расписания", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _user = result.Item1;
                    UpdateUserInfo();
                    LoadHabits();

                    if (result.Item2.Count > 0)
                    {
                        var undistributed = string.Join("\n", result.Item2.ConvertAll(h => h.Name));
                        MessageBox.Show($"Нераспределенные привычки:\n{undistributed}",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnAddHabit_Click(object sender, EventArgs e)
        {
            var form = new AddHabitForm();
            if (form.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    var habit = form.GetHabit(_user.NameID);
                    var result = _taskService.AddHabit(habit);

                    if (result == null)
                    {
                        MessageBox.Show("Ошибка при добавлении привычки", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }

                    _user = result.Item1;
                    UpdateUserInfo();
                    LoadHabits();

                    if (result.Item2.Count > 0)
                    {
                        var undistributed = string.Join("\n", result.Item2.ConvertAll(h => h.Name));
                        MessageBox.Show($"Нераспределенные привычки:\n{undistributed}",
                            "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnDeleteHabit_Click(object sender, EventArgs e)
        {
            if (lstHabits.SelectedIndex == -1)
            {
                MessageBox.Show("Выберите привычку для удаления", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            var habitName = lstHabits.SelectedItem.ToString().Split(' ')[1];
            var result = _taskService.DeleteHabit(_user.NameID, habitName);

            if (result == null)
            {
                MessageBox.Show("Ошибка при удалении привычки", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _user = result.Item1;
            LoadHabits();
            MessageBox.Show("Привычка успешно удалена", "Успех",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void btnDeleteAllHabits_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить все привычки?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                var result = _taskService.DeleteHabits(_user.NameID);

                if (result == null)
                {
                    MessageBox.Show("Ошибка при удалении привычек", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _user = result.Item1;
                LoadHabits();
                MessageBox.Show("Все привычки успешно удалены", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void chkNotifications_CheckedChanged(object sender, EventArgs e)
        {
            if (_isInitializing)
                return;
            var settings = new UserSettings(
                _user.Settings.Id,
                chkNotifications.Checked,
                _user.Settings.UserNameID,
                _user.Settings.SettingsTimes);

            var user = _taskService.ChangeSettings(settings);

            if (user != null)
            {
                _user = user;
                MessageBox.Show("Настройки уведомлений обновлены", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void btnNotificationTimes_Click(object sender, EventArgs e)
        {
            var form = new NotificationTimesForm(_user.Settings.SettingsTimes);
            if (form.ShowDialog() == DialogResult.OK)
            {
                var settings = new UserSettings(
                    _user.Settings.Id,
                    _user.Settings.NotifyOn,
                    _user.Settings.UserNameID,
                    form.GetTimes());

                var user = _taskService.ChangeSettings(settings);

                if (user != null)
                {
                    _user = user;
                    MessageBox.Show("Временные интервалы обновлены", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void btnDeleteAccount_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите удалить свою учетную запись?", "Подтверждение",
                MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
            {
                if (_taskService.DeleteUser(_user.NameID))
                {
                    MessageBox.Show("Учетная запись успешно удалена", "Успех",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Ошибка при удалении учетной записи", "Ошибка",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnLogout_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void InitializeComponent()
        {
            this.lblUsername = new System.Windows.Forms.Label();
            this.lblPhone = new System.Windows.Forms.Label();
            this.lblHabits = new System.Windows.Forms.Label();
            this.lstHabits = new System.Windows.Forms.ListBox();
            this.btnImportSchedule = new System.Windows.Forms.Button();
            this.btnAddHabit = new System.Windows.Forms.Button();
            this.btnDeleteHabit = new System.Windows.Forms.Button();
            this.btnDeleteAllHabits = new System.Windows.Forms.Button();
            this.chkNotifications = new System.Windows.Forms.CheckBox();
            this.btnNotificationTimes = new System.Windows.Forms.Button();
            this.btnDeleteAccount = new System.Windows.Forms.Button();
            this.btnLogout = new System.Windows.Forms.Button();
            // Новые элементы для событий
            this.lblEvents = new System.Windows.Forms.Label();
            this.lstEvents = new System.Windows.Forms.ListBox();
            this.SuspendLayout();

            // lblUsername
            this.lblUsername.AutoSize = true;
            this.lblUsername.Location = new System.Drawing.Point(20, 20);
            this.lblUsername.Name = "lblUsername";
            this.lblUsername.Size = new System.Drawing.Size(100, 13);
            this.lblUsername.Text = "Пользователь:";

            // lblPhone
            this.lblPhone.AutoSize = true;
            this.lblPhone.Location = new System.Drawing.Point(20, 40);
            this.lblPhone.Name = "lblPhone";
            this.lblPhone.Size = new System.Drawing.Size(55, 13);
            this.lblPhone.Text = "Телефон:";

            // lblHabits
            this.lblHabits.AutoSize = true;
            this.lblHabits.Location = new System.Drawing.Point(20, 70);
            this.lblHabits.Name = "lblHabits";
            this.lblHabits.Size = new System.Drawing.Size(60, 13);
            this.lblHabits.Text = "Привычки:";

            // lstHabits
            this.lstHabits.FormattingEnabled = true;
            this.lstHabits.Location = new System.Drawing.Point(20, 90);
            this.lstHabits.Name = "lstHabits";
            this.lstHabits.Size = new System.Drawing.Size(300, 160);

            // btnImportSchedule
            this.btnImportSchedule.Location = new System.Drawing.Point(330, 90);
            this.btnImportSchedule.Name = "btnImportSchedule";
            this.btnImportSchedule.Size = new System.Drawing.Size(150, 30);
            this.btnImportSchedule.Text = "Импорт расписания";
            this.btnImportSchedule.Click += new System.EventHandler(this.btnImportSchedule_Click);

            // btnAddHabit
            this.btnAddHabit.Location = new System.Drawing.Point(330, 130);
            this.btnAddHabit.Name = "btnAddHabit";
            this.btnAddHabit.Size = new System.Drawing.Size(150, 30);
            this.btnAddHabit.Text = "Добавить привычку";
            this.btnAddHabit.Click += new System.EventHandler(this.btnAddHabit_Click);

            // btnDeleteHabit
            this.btnDeleteHabit.Location = new System.Drawing.Point(330, 170);
            this.btnDeleteHabit.Name = "btnDeleteHabit";
            this.btnDeleteHabit.Size = new System.Drawing.Size(150, 30);
            this.btnDeleteHabit.Text = "Удалить привычку";
            this.btnDeleteHabit.Click += new System.EventHandler(this.btnDeleteHabit_Click);

            // btnDeleteAllHabits
            this.btnDeleteAllHabits.Location = new System.Drawing.Point(330, 210);
            this.btnDeleteAllHabits.Name = "btnDeleteAllHabits";
            this.btnDeleteAllHabits.Size = new System.Drawing.Size(150, 30);
            this.btnDeleteAllHabits.Text = "Удалить все привычки";
            this.btnDeleteAllHabits.Click += new System.EventHandler(this.btnDeleteAllHabits_Click);

            // chkNotifications
            this.chkNotifications.AutoSize = true;
            this.chkNotifications.Location = new System.Drawing.Point(20, 260);
            this.chkNotifications.Name = "chkNotifications";
            this.chkNotifications.Size = new System.Drawing.Size(150, 17);
            this.chkNotifications.Text = "Разрешить уведомления";
            this.chkNotifications.CheckedChanged += new System.EventHandler(this.chkNotifications_CheckedChanged);

            // btnNotificationTimes
            this.btnNotificationTimes.Location = new System.Drawing.Point(20, 290);
            this.btnNotificationTimes.Name = "btnNotificationTimes";
            this.btnNotificationTimes.Size = new System.Drawing.Size(220, 30);
            this.btnNotificationTimes.Text = "Изменить время уведомлений";
            this.btnNotificationTimes.Click += new System.EventHandler(this.btnNotificationTimes_Click);

            // btnDeleteAccount
            this.btnDeleteAccount.Location = new System.Drawing.Point(330, 290);
            this.btnDeleteAccount.Name = "btnDeleteAccount";
            this.btnDeleteAccount.Size = new System.Drawing.Size(150, 30);
            this.btnDeleteAccount.Text = "Удалить аккаунт";
            this.btnDeleteAccount.Click += new System.EventHandler(this.btnDeleteAccount_Click);

            // btnLogout
            this.btnLogout.Location = new System.Drawing.Point(330, 330);
            this.btnLogout.Name = "btnLogout";
            this.btnLogout.Size = new System.Drawing.Size(150, 30);
            this.btnLogout.Text = "Выйти";
            this.btnLogout.Click += new System.EventHandler(this.btnLogout_Click);

            // lblEvents
            this.lblEvents.AutoSize = true;
            this.lblEvents.Location = new System.Drawing.Point(20, 330);
            this.lblEvents.Name = "lblEvents";
            this.lblEvents.Size = new System.Drawing.Size(50, 13);
            this.lblEvents.Text = "Расписание:";

            // lstEvents
            this.lstEvents.FormattingEnabled = true;
            this.lstEvents.Location = new System.Drawing.Point(20, 350);
            this.lstEvents.Name = "lstEvents";
            this.lstEvents.Size = new System.Drawing.Size(460, 160);

            // UserDashboardForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(500, 550);
            this.Controls.Add(this.lblUsername);
            this.Controls.Add(this.lblPhone);
            this.Controls.Add(this.lblHabits);
            this.Controls.Add(this.lstHabits);
            this.Controls.Add(this.btnImportSchedule);
            this.Controls.Add(this.btnAddHabit);
            this.Controls.Add(this.btnDeleteHabit);
            this.Controls.Add(this.btnDeleteAllHabits);
            this.Controls.Add(this.chkNotifications);
            this.Controls.Add(this.btnNotificationTimes);
            this.Controls.Add(this.btnDeleteAccount);
            this.Controls.Add(this.btnLogout);
            this.Controls.Add(this.lblEvents);
            this.Controls.Add(this.lstEvents);
            this.Name = "UserDashboardForm";
            this.Text = "Трекер привычек - Личный кабинет";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblUsername;
        private System.Windows.Forms.Label lblPhone;
        private System.Windows.Forms.Label lblHabits;
        private System.Windows.Forms.ListBox lstHabits;
        private System.Windows.Forms.Button btnImportSchedule;
        private System.Windows.Forms.Button btnAddHabit;
        private System.Windows.Forms.Button btnDeleteHabit;
        private System.Windows.Forms.Button btnDeleteAllHabits;
        private System.Windows.Forms.CheckBox chkNotifications;
        private System.Windows.Forms.Button btnNotificationTimes;
        private System.Windows.Forms.Button btnDeleteAccount;
        private System.Windows.Forms.Button btnLogout;
        private System.Windows.Forms.Label lblEvents;
        private System.Windows.Forms.ListBox lstEvents;
    }
}