using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace TellTaleWidescreenPatcher
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            this.AllowDrop = true;
            this.DragEnter += new DragEventHandler(Form1_DragEnter);
            this.DragDrop += new DragEventHandler(Form1_DragDrop);
            form = this;
        }

        private void BrowseButton_Click(object sender, EventArgs e)
        {
            // Show the dialog that allows user to select a file, the
            // call will result a value from the DialogResult enum
            // when the dialog is dismissed.
            OpenFileDialog dlg = new OpenFileDialog
            {
                Filter = "TellTale Game Executable|*.exe"
            };
            DialogResult result = dlg.ShowDialog();
            // if a file is selected
            if (result == DialogResult.OK)
            {
                // Set the selected file URL to the textbox
                this.PathBox.Text = dlg.FileName;

                // Change status
                CheckStatus();
            }
        }

        private void PathBox_TextChanged(object sender, EventArgs e)
        {
            CheckStatus();
        }

        private void ResolutionBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            CheckStatus();
            Console.WriteLine(ResolutionBox.SelectedIndex);
        }

        public void CheckStatus()
        {
            if (File.Exists(this.PathBox.Text) && Path.GetExtension(this.PathBox.Text) == ".exe" && ResolutionBox.SelectedItem != null)
            {
                this.StatusText.Text = "Found executable!";
                this.StatusText.ForeColor = Color.YellowGreen;
                this.PatchButton.Enabled = true;
                this.progressBar1.Value = 0;
            }
            else
            {
                this.StatusText.Text = "Waiting...";
                this.StatusText.ForeColor = Color.Blue;
                this.PatchButton.Enabled = false;
                this.progressBar1.Value = 0;
            }
        }

        public static void SetStatus(string status, Color color)
        {
            form.StatusText.Invoke((MethodInvoker)(() => { form.StatusText.Text = status; }));
            form.StatusText.Invoke((MethodInvoker)(() => { form.StatusText.ForeColor = color; }));
        }

        public static int GetResolution()
        {
            int index = -1;
            form.StatusText.Invoke((MethodInvoker)(() => { index = form.ResolutionBox.SelectedIndex; }));
            return index;
        }

        public static void SetProgress(int value)
        {
            form.progressBar1.Value = value;
        }

        private static Form1 form = null;

        private void PatchButton_Click(object sender, EventArgs e)
        {
            if (!PatchWorker.IsBusy)
            {
                PatchWorker.WorkerReportsProgress = true;
                PatchButton.Enabled = false;
                PatchWorker.RunWorkerAsync();
            }
        }

        private void PatchWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            form.progressBar1.Invoke((MethodInvoker)(() => { form.progressBar1.Value = e.ProgressPercentage; }));
        }

        private void PatchWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Program.PatchFunction(this.PathBox.Text);
            form.progressBar1.Invoke((MethodInvoker)(() => { form.progressBar1.Value = 100; }));
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length == 1 && Path.GetExtension(files[0]) == ".exe")
                    e.Effect = DragDropEffects.Copy;
            }
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            this.PathBox.Text = files[0];
        }
    }
}