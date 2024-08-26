#region

using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

#endregion

namespace TelltaleWidescreenPatcher;

public partial class Form1 : Form
{
    private static Form1 _form;

    public Form1()
    {
        InitializeComponent();
        var ver = Assembly.GetEntryAssembly()?.GetName().Version;
        Text += " v" + ver.Major + "." + ver.Minor + (ver.Build.ToString() == "0" ? "" : "." + ver.Build);
        AllowDrop = true;
        DragEnter += Form1_DragEnter;
        DragDrop += Form1_DragDrop;
        _form = this;
    }

    private void BrowseButton_Click(object sender, EventArgs e)
    {
        // Show the dialog that allows user to select a file, the
        // call will result a value from the DialogResult enum
        // when the dialog is dismissed.
        var dlg = new OpenFileDialog
        {
            Filter = "Telltale Game|*.exe"
        };
        var result = dlg.ShowDialog();
        // if a file is selected
        if (result != DialogResult.OK) return;
        // Set the selected file URL to the textbox
        PathBox.Text = dlg.FileName;

        // Change status
        CheckStatus();
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

    private void CheckStatus()
    {
        if (File.Exists(PathBox.Text) && Path.GetExtension(PathBox.Text) == ".exe" && ResolutionBox.SelectedItem != null)
        {
            StatusText.Text = "Found executable!";
            StatusText.ForeColor = Color.YellowGreen;
            PatchButton.Enabled = true;
            progressBar1.Value = 0;
        }
        else
        {
            StatusText.Text = "Waiting...";
            StatusText.ForeColor = Color.Blue;
            PatchButton.Enabled = false;
            progressBar1.Value = 0;
        }
    }

    public static void SetStatus(string status, Color color)
    {
        _form.StatusText.Invoke((MethodInvoker)(() => { _form.StatusText.Text = status; }));
        _form.StatusText.Invoke((MethodInvoker)(() => { _form.StatusText.ForeColor = color; }));
    }

    public static string GetResolution()
    {
        string resolution = null;
        _form.StatusText.Invoke((MethodInvoker)(() =>
        {
            resolution = _form.ResolutionBox.SelectedItem?.ToString();
        }));

        return resolution;
    }

    public static void SetProgress(int value, Color? color = null)
    {
        _form.progressBar1.Invoke((MethodInvoker)(() => { _form.progressBar1.Value = value; }));
        _form.progressBar1.Invoke((MethodInvoker)(() =>
        {
            _form.progressBar1.ForeColor = color.GetValueOrDefault(Color.Green);
        }));
    }

    public static void IncrementProgress(int value)
    {
        _form.progressBar1.Invoke((MethodInvoker)(() => { _form.progressBar1.Increment(value); }));
    }

    private void PatchButton_Click(object sender, EventArgs e)
    {
        if (PatchWorker.IsBusy) return;
        PatchButton.Enabled = false;
        PatchWorker.RunWorkerAsync();
    }

    private void PatchWorker_DoWork(object sender, DoWorkEventArgs e)
    {
        Program.PatchFunction(PathBox.Text);
        SetProgress(100);
        _form.PatchButton.Invoke((MethodInvoker)(() => { _form.PatchButton.Enabled = true; }));
    }

    private static void Form1_DragEnter(object sender, DragEventArgs e)
    {
        if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        if (files.Length == 1 && Path.GetExtension(files[0]) == ".exe")
            e.Effect = DragDropEffects.Copy;
    }

    private void Form1_DragDrop(object sender, DragEventArgs e)
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        PathBox.Text = files[0];
    }
}
