using Ookii.Dialogs.Wpf;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PbixTools.GUI
{
    public partial class MainForm : Form
    {

        private TraceSwitch _traceSwitch;
        private ErrorProvider _errorProvider;

        public MainForm()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // Set trace level
            _traceSwitch = new TraceSwitch("General", "Entire Application");
            _traceSwitch.Level = TraceLevel.Info;
            _errorProvider = new ErrorProvider();
            _errorProvider.BlinkStyle = ErrorBlinkStyle.NeverBlink;
            _errorProvider.RightToLeft = true;
            _errorProvider.SetIconPadding(this, 2);
            _errorProvider.ContainerControl = this;
            AutoValidate = System.Windows.Forms.AutoValidate.EnableAllowFocusChange;
            
        }


        private void buttonBrowseSources_Click(object sender, EventArgs e)
        {
            VistaOpenFileDialog ofd = new VistaOpenFileDialog();
           
            ofd.Filter = "Text Files (.pbix)|*.pbix|All Files (*.*)|*.*";
            ofd.FilterIndex = 1;
            ofd.Multiselect = true;
            bool? filesSelected = ofd.ShowDialog();
            if (filesSelected == true)
                textBoxSourceFiles.Text = string.Join(";",ofd.FileNames);
        }

        private void buttonBrowseDestination_Click(object sender, EventArgs e)
        {
            VistaFolderBrowserDialog ofd = new VistaFolderBrowserDialog();
            ofd.ShowNewFolderButton = true;
            ofd.Description = "Selected folder to save pbix files.";
            bool? pathWasSelected = ofd.ShowDialog();
            if (pathWasSelected == true)
            {
                textBoxOutputDirectory.Text = ofd.SelectedPath;
            }
        }

        private async void buttonStart_Click(object sender, EventArgs e)
        {

            if (!this.ValidateChildren())
                return;
          
            this.Enabled = false;
            this.UseWaitCursor = true;

            try
            {
                string[] files = textBoxSourceFiles.Text.Split(';');
                string outputPath = textBoxOutputDirectory.Text;

                progressBar.Maximum = files.Count();
                progressBar.Visible = true;

                var progressHandler = new Progress<int>(value =>
                {
                    labelStatus.Text = String.Format("Cleaning {0}", files[value]);
                    progressBar.Value = value;
                });

                var progress = progressHandler as IProgress<int>;
                await Task.Run(() =>
                {
                    PbixTools.CLI.PbixUtils util = new PbixTools.CLI.PbixUtils(_traceSwitch);
            
                    for (int i = 0; i < files.Count(); ++i)
                    {
                        if (progress != null)
                            progress.Report(i);
                         util.RemoveUnusedVisuals(files[i], Path.Combine(outputPath,Path.GetFileName(files[i])));
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                progressBar.Visible = false;
                labelStatus.Text = "Completed.";
                this.Enabled = true;
                this.UseWaitCursor = false;

            }
        }

        private void textBoxOutputDirectory_Validating(object sender, CancelEventArgs e)
        {
            bool thesame = false;
            foreach (string f in textBoxSourceFiles.Text.Split(';'))
                if (f.Equals(Path.Combine(textBoxOutputDirectory.Text,Path.GetFileName(f))))
                {
                    thesame = true;
                    break;
                } 

            if (textBoxOutputDirectory.TextLength == 0)
            {
                _errorProvider.SetError(textBoxOutputDirectory, "Output directory cannnot be empty");
                e.Cancel = true;
            } else if (!Directory.Exists(textBoxOutputDirectory.Text))
            {
                _errorProvider.SetError(textBoxOutputDirectory, "Output directory does not exist");
                e.Cancel = true;
            } else if (thesame)
            {
                _errorProvider.SetError(textBoxOutputDirectory, "Output directory needs to be different the source directory");
                e.Cancel = true;
            }
            else
            {
                _errorProvider.SetError(textBoxOutputDirectory, "");
            }
        }

        private void textBoxSourceFiles_Validating(object sender, CancelEventArgs e)
        {
            string[] files = textBoxSourceFiles.Text.Split(';');
            List<string> donotexist = new List<string>();
            foreach (string f in files)
                if (!File.Exists(f))
                    donotexist.Add(f);

            if (textBoxSourceFiles.TextLength == 0)
            {
                _errorProvider.SetError(textBoxSourceFiles, "Need to select files to process");
                e.Cancel = true;
            } else if (donotexist.Count > 0)
            {
                _errorProvider.SetError(textBoxSourceFiles, String.Format("The following files do not exist: {0}", string.Join(Environment.NewLine,donotexist)));
                e.Cancel = true;
            } else
            {
                _errorProvider.SetError(textBoxSourceFiles, "");
            }
        }
    }
}
