using FileTailSearch;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TestLogCollecter
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            try
            {
                //E:\WJKJ\wj_isfp_rm\WJ.CLIENT.AGENT\bin\Debug\netcoreapp3.1\logs\INFO\2020-12-29.log
                if (string.IsNullOrEmpty(boxFilePath.Text))
                {
                    MessageBox.Show("请填写文件");
                    return;
                }
                string path = boxFilePath.Text;
                if (!File.Exists(path))
                {
                    richTextBox1.Text = "不存在这个文件！";
                    boxFilePath.Text = $@"E:\WJKJ\wj_isfp_rm\WJ.CLIENT.AGENT\bin\Debug\netcoreapp3.1\logs\INFO\{DateTime.Now.ToString("yyyy-MM-dd")}.log";
                }
                if (string.IsNullOrEmpty(boxLineNum.Text))
                {
                    MessageBox.Show("请填写查找的行数！");
                }
                string regect = boxRegect.Text;
                string lineNum = boxLineNum.Text;
                System.Diagnostics.Stopwatch watch = new System.Diagnostics.Stopwatch();
                watch.Start();
                Tail tail = new Tail();
                var data = tail.ReadFirstTime(path, int.Parse(lineNum));
                watch.Stop();
                var time = watch.Elapsed.TotalMilliseconds;
                timeDes.Text = $"获取日志共耗时：{time}毫秒！";
                if (!string.IsNullOrEmpty(data))
                {
                    richTextBox1.Text = data;
                }
            }
            catch (Exception ex)
            {

                MessageBox.Show(ex.Message);
            }
            
        }
    }
}
