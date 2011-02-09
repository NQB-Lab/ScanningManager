/*
 * Created by SharpDevelop.
 * User: oferfrid
 * Date: 1/29/2008
 * Time: 1:31 PM
 * 
 * To change this template use Tools | Options | Coding | Edit Standard Headers.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using System.IO;
using System.Runtime.InteropServices;
using WIALib ;

namespace ScaningManager
{
	/// <summary>
	/// Description of MainForm.
	/// </summary>
	public partial class MainForm : Form
	{
		ScannerControl myScannerControl;
		ScannerControl[] Scanners;
		int NumberOfScanners;
		Bitmap[] LastScans;
		DateTime NextScan;
		int HunderdNano2Sec = 10000000;
		
		public MainForm()
		{

			
			//
			// The InitializeComponent() call is required for Windows Forms designer support.
			//
			InitializeComponent();
			
			myScannerControl = new ScannerControl();
			
		}
		
		void calcExperimentEnd(object sender, EventArgs e)
		{
			dtpEndDateTime.Value = dtpStartDateTime.Value.AddMinutes(Convert.ToInt32(tbRepetitions.Text)*
			                                                         Convert.ToDouble(tbTimeGap.Text));
		}
		
		
		
		void BtnScanClick(object sender, EventArgs e)
		{
			// Initializing the properties of scanning for each scanner
			// ---------------------------------------------------------
			NumberOfScanners = lbScannersList.SelectedItems.Count;
			Scanners = new ScannerControl[NumberOfScanners];
			LastScans = new Bitmap[NumberOfScanners];
			for ( int i=0;i<NumberOfScanners;i++)
			{
				Scanners[i] = new ScannerControl();
				Scanners[i].SelectDevice(lbScannersList.SelectedItems[i]);
				try{

				Scanners[i].SelectPicsPropertiesFromUI();
				}
				catch (System.ApplicationException)
				{
					MessageBox.Show("Prop");
					return;
				}
				//+++++++++++++++++++++++++++++++
				// if cancel then delete scanner object
				// +++++++++++++++++++++++++++++++
				
				cmbActiveScanners.Items.Add(lbScannersList.SelectedItems[i]);
				
			}
			
			// taking the fisrt picture
			// -------------------------
			ScanNow();
			
			// total experiment progress bar
			progExperimentProgress.Minimum = 0;
			progExperimentProgress.Maximum = Convert.ToInt32((dtpEndDateTime.Value - DateTime.Now).Ticks/(HunderdNano2Sec));
			progExperimentProgress.Value = 0;
			
			// Setting the timer to the first picture
			// ---------------------------------------
			int TimeToFirstScan = Convert.ToInt32((dtpStartDateTime.Value - DateTime.Now).Ticks/HunderdNano2Sec);
			if (TimeToFirstScan < 0)
			{
				// starting experiment immediatly
				StartTimerTick(this, new EventArgs());
			}
			else
			{
				// starting experiment with delay
				StartTimer.Interval =  TimeToFirstScan*1000 ;
				StartTimer.Start();
				// starting progress bar
				NextScan = dtpStartDateTime.Value;
				progTimeToNextScan.Minimum = 0;
				progTimeToNextScan.Maximum = TimeToFirstScan;
				progTimeToNextScan.Value = 0;		
				lblTimeToNextScan.Text = @"Time To Next Scan: " + Seconds2hhmmssString(TimeToFirstScan);//TimeToFirstScan.ToString() + " seconds";
			}
			UpdateProgressTimer.Start();
			
			// disabling the configuration and enabling the status group
			// ----------------------------------------------------------
			gbExperimentStatus.Enabled = true;
			gbScanningConfiguration.Enabled = false;
			gbExperimentConfiguration.Enabled = false;
		}
		
		string GetDateString (DateTime _DateTime)
		{
			//return DateTime.Now.Year.ToString() + DateTime.Now.Month.ToString("D2")  + DateTime.Now.Day.ToString("D2") + DateTime.Now.Hour.ToString("D2") + DateTime.Now.Minute.ToString("D2") + DateTime.Now.Second.ToString("D2") ;
			return _DateTime.ToString("yyyyMMdd_HHmmss");
		}
		
		

		void BtnSelectOutputFolderClick(object sender, EventArgs e)
		{
			fbdSaveTo.RootFolder = System.Environment.SpecialFolder.MyComputer;
			fbdSaveTo.ShowNewFolderButton = true;
			fbdSaveTo.SelectedPath  =  System.Configuration.ConfigurationManager.AppSettings["ImagesFolder"];
			if (fbdSaveTo.ShowDialog() == DialogResult.OK)
			{
				tbOutputPath.Text  =  fbdSaveTo.SelectedPath;
			}
		}
		
		
		
		
		void MainFormLoad(object sender, EventArgs e)
		{				
			dtpStartDateTime.Value = DateTime.Now;
			calcExperimentEnd(this, new EventArgs());
			tbOutputPath.Text =System.Configuration.ConfigurationManager.AppSettings["ImagesFolder"];
			
			foreach(DeviceInfo myDeviceInfo in  myScannerControl.GetConnectedDevices())
			{
				lbScannersList.Items.Add(myDeviceInfo );
			}
		}
		
		void ScanningTimerTick(object sender, EventArgs e)
		{
			ScanNow();
			NextScan = DateTime.Now.AddMinutes(Convert.ToInt32(tbTimeGap.Text));
			UpdateExperimentProgress();
		}
		
		void StartTimerTick(object sender, EventArgs e)
		{
			StartTimer.Stop();
			ScanningTimer.Interval = Convert.ToInt32(Decimal.Floor(Convert.ToDecimal(tbTimeGap.Text)*60*1000));
			ScanningTimer.Start();
			
			// progress bar init			
			NextScan = DateTime.Now.AddMinutes(Convert.ToInt32(tbTimeGap.Text));
			lblTimeToNextScan.Text = @"Time To Next Scan: " + Seconds2hhmmssString((Convert.ToInt32(tbTimeGap.Text)*60));//(Convert.ToInt32(tbTimeGap.Text)*60).ToString() + " seconds";
			progTimeToNextScan.Maximum = Convert.ToInt32(tbTimeGap.Text)*60;
			progTimeToNextScan.Minimum = 0;
			progTimeToNextScan.Value = 0;

			
			// start experiment
			ScanningTimer.Interval = Convert.ToInt32(Decimal.Floor(Convert.ToDecimal(tbTimeGap.Text)*60*1000));
			ScanNow();
			
		}
		
		void ScanNow()
		{
			if (dtpEndDateTime.Value < DateTime.Now)
			{
				ScanningTimer.Stop();
			}
			else
			{
				for (int i=0 ; i<NumberOfScanners;i++)
				{
					LastScans[i]= Scanners[i].Scan(tbOutputPath.Text +  @"\" + tbFileName.Text + @"_" + i.ToString()+ @"_" + GetDateString(DateTime.Now)  +@".tif");
					picLastScan.Image = LastScans[i];
				}
			}
		}
		
		void BtnExitClick(object sender, EventArgs e)
		{
			ScanningTimer.Stop();
			StartTimer.Stop();
			UpdateProgressTimer.Stop();
			gbScanningConfiguration.Enabled   = true;
			gbExperimentConfiguration.Enabled = true;
			gbExperimentStatus.Enabled        = true;
		}
		
		void CmbActiveScannersSelectedIndexChanged(object sender, EventArgs e)
		{
			picLastScan.Image = LastScans[cmbActiveScanners.SelectedIndex];
		}
		
		void UpdateProgressTimerTick(object sender, EventArgs e)
		{
			UpdateNextScanProgress();
			UpdateExperimentProgress();
		}
		
		void UpdateNextScanProgress()
		{
			int TimeLeft = Convert.ToInt32((NextScan - DateTime.Now).Ticks/HunderdNano2Sec);
			if (TimeLeft <= 0)
			{
				progTimeToNextScan.Value = progTimeToNextScan.Maximum;
				lblTimeToNextScan.Text = @"Time To Next Scan: 0 seconds";
			}
			else
			{
				progTimeToNextScan.Value = progTimeToNextScan.Maximum - TimeLeft;
				lblTimeToNextScan.Text = @"Time To Next Scan: " + Seconds2hhmmssString(TimeLeft);//TimeLeft.ToString() + " seconds";
				lblProgress.Text = @"Time Left: " + Seconds2hhmmssString(Convert.ToInt32((dtpEndDateTime.Value - DateTime.Now).Ticks/(HunderdNano2Sec)));//+ TimeLeft.ToString() + " minutes";
			}
		}
		
		//-------------------------------
		void UpdateExperimentProgress()
		{
			int TimeLeft = Convert.ToInt32((dtpEndDateTime.Value - DateTime.Now).Ticks/(HunderdNano2Sec));
			if (TimeLeft <= 0)
			{
				progExperimentProgress.Value = progExperimentProgress.Maximum;
				lblProgress.Text = @"Experiment Ended";
				ScanningTimer.Stop();
				UpdateProgressTimer.Stop();
			}
			else
			{
				progExperimentProgress.Value = progExperimentProgress.Maximum - TimeLeft;
				lblProgress.Text = @"Time Left: " + Seconds2hhmmssString(TimeLeft);//TimeLeft.ToString() + " minutes";
			}
		}
		
		string Seconds2hhmmssString(int TimeInSeconds)
		{
			string TSstr = string.Empty;
			int SecondsInHour = 60*24;
			int SecondsInMinute = 60;
			int sec = 0;
			int min = 0;
			int hr = 0;
			
			hr = TimeInSeconds/SecondsInHour;
			min = (TimeInSeconds - hr*SecondsInHour)/SecondsInMinute;
			sec = TimeInSeconds - hr*SecondsInHour - min*SecondsInMinute;
			
			TSstr = hr.ToString("D2") + ":" + min.ToString("D2") + ":" + sec.ToString("D2");
			return TSstr;
		}
		
		
	}
}
