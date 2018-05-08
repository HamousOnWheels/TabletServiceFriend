using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.ServiceProcess;
using System.IO;

namespace Tablet_Service_Friend
{
    public partial class mainWindow : Form
    {
        string prefUtil { get; set; }
        string wacomCPL { get; set; }
        string tabletDat { get; set; }
        string bekapFajl { get; set; }
        string servis { get; set; }

        public mainWindow()
        {
            InitializeComponent();
        }

        // Verify if a service exists
        public bool servisPostoji(string ServisIme)
        {
            return ServiceController.GetServices().Any(serviceController => serviceController.ServiceName.Equals(ServisIme));
        }

        //add a line to the textbox
        public void ispisiTekst(string tekst)
        {
            richTextBox1.AppendText(tekst + Environment.NewLine);
        }

        //show error and disable almost all options if the driver isnt found on the system
        public void nemaDrajvera(string tekst)
        {
            MessageBox.Show(String.Format("Wacom {0} not found. Please update or reinstall the driver.", tekst),
                            "Driver missing",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Error);

            button1.Enabled = false;
            backupToolStripMenuItem.Enabled = false;
            restoreToolStripMenuItem.Enabled = false;
            wacomSettingsToolStripMenuItem.Enabled = false;
            wacomPreferenceFileUtilityToolStripMenuItem.Enabled = false;
            changeTabletOrientationToolStripMenuItem.Enabled = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            string progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            string appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);

            //check whether the driver installed is "Consumer" (5.x.x.x) or "Professional" (6.x.x.x)
            if (servisPostoji("WTabletServicePro"))
             {
                //find some essential driver files. NEEDS SOLUTION FOR 32-BIT SYSTEMS
                wacomCPL = progFiles + "/Tablet/Wacom/Professional_CPL.exe";
                prefUtil = progFiles + "/Tablet/Wacom/32/PrefUtil.exe";
                tabletDat = appData + "/WTablet/Wacom_Tablet.dat";
                bekapFajl = "Wacom_Tablet.dat";
                servis = "WTabletServicePro";
            }
            else if (servisPostoji("WTabletServiceCon"))
            {
                wacomCPL = progFiles + "/Tablet/Pen/Consumer_CPL.exe";
                prefUtil = progFiles + "/Tablet/Pen/32/PrefUtil.exe";
                tabletDat = appData + "/WTablet/Pen_Tablet.dat";
                bekapFajl = "Pen_Tablet.dat";
                servis = "WTabletServiceCon";
            }
            else
            {
                nemaDrajvera("service");
            }

            //check if those essential files actually exist
            if (File.Exists(prefUtil) && File.Exists(wacomCPL))
            {
                ispisiTekst("Ready");  ///add nicer text
            }
            else
            {
                nemaDrajvera("driver files");
            }
            

        }

        private void button1_Click(object sender, EventArgs e)
        {
            ServiceController wacomService = new ServiceController(servis);            
            //ispisiTekst(wacomService.Status.ToString());

            try
            {
                this.Enabled = false; //disable form while waiting

                //backup the settings first
                var bekap = Path.GetTempFileName();
                File.Copy(tabletDat, bekap, true);
                //ispisiTekst("backed up");
                
                TimeSpan timeout = TimeSpan.FromSeconds(5);

                //if the service is still running, stop it
                if (wacomService.Status == ServiceControllerStatus.Running)
                {
                    wacomService.Stop();
                    wacomService.WaitForStatus(ServiceControllerStatus.Stopped, timeout);
                    //ispisiTekst("Service stopped");
                }
                
                //now start it
                wacomService.Start();
                ispisiTekst("Restarting service...");
                wacomService.WaitForStatus(ServiceControllerStatus.Running, timeout);

                //restore the settings
                File.Copy(bekap, tabletDat, true);
                File.Delete(bekap);

                ispisiTekst("Success! Please relaunch your painting app to get pressure sensitivity back.");

                this.Enabled = true;
            }
            catch
            {
                richTextBox1.SelectionColor = Color.Red;
                ispisiTekst("Could not restart service.");
                this.Enabled = true;
            }
        }

        private void backupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog bekap = new SaveFileDialog
            {
                Title = "Backup Tablet Settings",
                FileName = bekapFajl,
                Filter = "Wacom Configuration File|*.dat"
            };  // make a dialog

            if (bekap.ShowDialog() == DialogResult.OK)
            {
                File.Copy(tabletDat, bekap.FileName, true);
                ispisiTekst("Settings backed up.");
            }
        }

        private void restoreToolStripMenuItem_Click(object sender, EventArgs e)
        {
            OpenFileDialog ristor = new OpenFileDialog
            {
                Title = "Restore Tablet Settings",
                FileName = bekapFajl,
                Filter = "Wacom Configuration File|*.dat"
            };  // make a dialog

            if (ristor.ShowDialog() == DialogResult.OK)
            {
                File.Copy(tabletDat, tabletDat+".bak", true);  // make a backup copy first
                File.Copy(ristor.FileName, tabletDat, true);
                ispisiTekst("Settings restored. A backup copy was made");
            }            
        }

        private void richTextBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {

        }

        private void wacomSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(wacomCPL);
        }

        private void wacomPreferenceFileUtilityToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(prefUtil);
        }

        private void downloadWacomDriverToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("https://www.wacom.com/en-us/support/product-support/drivers");
        }

        private void changeTabletOrientationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //to be added later
            /*Form2 orientationDialog = new Form2();
            orientationDialog.ShowDialog(); */
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }
        
        //close on escape key
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            if (keyData == Keys.Escape)
            {
                this.Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
