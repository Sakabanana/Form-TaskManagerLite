using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Diagnostics;

namespace Form_task2
{
    public partial class Form1 : Form
    {

        
        private Timer refreshTimer;// 定义计时器用于自动刷新进程列表

        public Form1()
        {
            InitializeComponent();
            InitializeCustomComponents();
        }//在构造函数中调用自定义组件初始化方法

        // 自定义组件初始化方法
        private void InitializeCustomComponents()
        {
            // 初始化计时器
            refreshTimer = new Timer();
            refreshTimer.Interval = 10000; // 每10秒刷新一次
            refreshTimer.Tick += RefreshTimer_Tick;

            // 设置ListView的视图模式为详细信息
            listView1.View = View.Details;
            listView1.FullRowSelect = true;
            listView1.GridLines = true;

            // 添加列标题
            listView1.Columns.Add("进程名称", 200);
            listView1.Columns.Add("进程ID", 80);
            listView1.Columns.Add("窗口标题", 250);
            listView1.Columns.Add("内存使用(KB)", 120);

            // 设置进度条
            progressBar1.Minimum = 0;
            progressBar1.Maximum = 100;
            progressBar1.Value = 0;
        }
        // 打开记事本的按钮事件处理程序
        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                Process.Start("notepad.exe");
                MessageBox.Show("记事本已启动", "提示");
                RefreshProcessList(); // 刷新列表
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法打开记事本：{ex.Message}", "错误");
            }
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // 窗体加载时刷新进程列表
        }

        // 关闭所有记事本进程的按钮事件处理程序
        private void button2_Click(object sender, EventArgs e)
        {
            try
            {
                Process[] notepads = Process.GetProcessesByName("notepad");

                if (notepads.Length == 0)
                {
                    MessageBox.Show("没有运行中的记事本程序", "提示");
                    return;
                }

                int closedCount = 0;
                int killedCount = 0;

                foreach (Process notepad in notepads)
                {
                    // 先尝试友好关闭
                    bool closed = notepad.CloseMainWindow();

                    if (closed)
                    {
                        // 等待3秒，让进程正常退出
                        if (notepad.WaitForExit(3000))
                        {
                            closedCount++;
                        }
                        else
                        {
                            notepad.Kill();
                            killedCount++;
                            MessageBox.Show($"进程 {notepad.ProcessName} (ID:{notepad.Id}) 未响应，已强制终止", "警告");
                        }
                    }
                    else
                    {
                        // 如果没有主窗口，直接终止
                        notepad.Kill();
                        killedCount++;
                    }
                }

                MessageBox.Show($"已关闭 {closedCount} 个记事本程序\n强制终止 {killedCount} 个", "完成");
                RefreshProcessList(); // 刷新列表
            }
            catch (Exception ex)
            {
                MessageBox.Show($"操作失败：{ex.Message}", "错误");
            }
        }

        private void progressBar1_Click(object sender, EventArgs e)
        {
            //刷新的进度条（可有可无，美观作用）
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //刷新显示进程的列表
            RefreshProcessList();
        }

        private void button5_Click(object sender, EventArgs e)
        {
            //关闭显示进程中的选择的进程
            if (listView1.SelectedItems.Count == 0)
            {
                MessageBox.Show("请先选择一个进程", "提示");
                return;
            }

            try
            {
                string processName = listView1.SelectedItems[0].SubItems[0].Text;
                int processId = int.Parse(listView1.SelectedItems[0].SubItems[1].Text);

                // 获取进程实例
                Process selectedProcess = Process.GetProcessById(processId);

                // 确认对话框
                DialogResult result = MessageBox.Show(
                    $"确定要关闭进程 \"{processName}\" (ID:{processId}) 吗？\n\n注意：关闭系统关键进程可能导致系统不稳定！",
                    "确认关闭",
                    MessageBoxButtons.YesNo,
                    MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    // 先尝试友好关闭
                    if (selectedProcess.CloseMainWindow())
                    {
                        if (!selectedProcess.WaitForExit(3000))
                        {
                            selectedProcess.Kill();
                            MessageBox.Show($"进程 {processName} 未响应，已强制终止", "警告");
                        }
                        else
                        {
                            MessageBox.Show($"进程 {processName} 已正常关闭", "完成");
                        }
                    }
                    else
                    {
                        // 没有主窗口，直接强制终止
                        selectedProcess.Kill();
                        selectedProcess.WaitForExit();
                        MessageBox.Show($"进程 {processName} 已被强制终止", "完成");
                    }

                    RefreshProcessList(); // 刷新列表
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"无法关闭进程：{ex.Message}", "错误");
            }
        }
        //核心
        private void RefreshProcessList()
        {
            try
            {
                // 清空现有列表
                listView1.Items.Clear();

                // 获取所有进程
                Process[] processes = Process.GetProcesses();

                // 更新进度条
                progressBar1.Maximum = processes.Length;
                progressBar1.Value = 0;

                // 过滤掉系统空闲进程（无权限访问）
                foreach (Process process in processes)
                {
                    try
                    {
                        // 跳过系统空闲进程
                        if (process.Id == 0) continue;

                        // 创建列表项
                        ListViewItem item = new ListViewItem(process.ProcessName);// 获取进程名称
                        item.SubItems.Add(process.Id.ToString());// 获取进程ID

                        // 获取窗口标题
                        string windowTitle = string.IsNullOrEmpty(process.MainWindowTitle) ?
                            "[无窗口]" : process.MainWindowTitle;
                        item.SubItems.Add(windowTitle);

                        // 获取内存使用量（工作集）
                        long memory = process.WorkingSet64 / 1024;
                        item.SubItems.Add(memory.ToString());

                        // 根据进程名称设置颜色（可选，让记事本进程高亮显示）
                        if (process.ProcessName.ToLower() == "notepad")
                        {
                            item.BackColor = Color.LightYellow;
                        }

                        listView1.Items.Add(item);
                        progressBar1.Value++;// 更新进度条
                    }
                    catch
                    {
                        // 某些系统进程可能无法访问，跳过
                        continue;
                    }
                }

                // 更新状态标签
                lblInfo.Text = $"当前进程数量：{listView1.Items.Count}";
                

                // 启动或停止计时器
                if (checkBoxAutoRefresh.Checked)
                {
                    if (!refreshTimer.Enabled)
                        refreshTimer.Start();
                }
                else
                {
                    if (refreshTimer.Enabled)
                        refreshTimer.Stop();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"刷新进程列表失败：{ex.Message}", "错误");
            }
        }

        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // 定时器触发时刷新进程列表
            RefreshProcessList();
        }

        private void button1_Click_1(object sender, EventArgs e)
        {
            string searchText = textBoxSearch.Text.Trim().ToLower();
            if (string.IsNullOrEmpty(searchText))
            {
                RefreshProcessList();
                return;
            }

            RefreshProcessList();

            bool found = false;
            int foundCount = 0;

            foreach (ListViewItem item in listView1.Items)
            {
                if (item.SubItems[0].Text.ToLower().Contains(searchText))
                {
                    item.EnsureVisible(); // 确保找到的项可见
                    item.Selected = true;
                    item.BackColor = Color.LightGreen;
                    found = true;
                    foundCount++;
                }
            }

            if (found)
            {
                MessageBox.Show($"搜索完成，找到 {foundCount} 个包含 \"{searchText}\" 的进程", "搜索结果");
            }
            else
            {
                MessageBox.Show($"未找到包含 \"{searchText}\" 的进程", "提示");
            }
        }

        private void listView1_SelectedIndexChanged(object sender, EventArgs e)
        {
            // 显示进程列表
        }


        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            if (checkBoxAutoRefresh.Checked)
            {
                refreshTimer.Start();
                MessageBox.Show("已开启自动刷新（每10秒）", "提示");
            }
            else
            {
                refreshTimer.Stop();
            }
        }
            private void lblInfo_Click(object sender, EventArgs e)
        {
            // 显示进程数量信息
            MessageBox.Show($"当前显示的进程数量：{listView1.Items.Count}\n" +
                           $"总系统进程数：{Process.GetProcesses().Length}\n" +
                           $"（部分系统进程无法显示）", "统计信息");
        }

        private void textBoxSearch_TextChanged(object sender, EventArgs e)
        {

        }
    }
    
}
