﻿using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Data;
using Microsoft.Win32;
using Microsoft.WindowsAPICodePack;
using Microsoft.WindowsAPICodePack.Taskbar;
using Un4seen.Bass;
using Un4seen.Bass.AddOn.Enc;
using Un4seen.Bass.AddOn.Mix;
using Un4seen.Bass.AddOn.Vst;
using Un4seen.Bass.AddOn.Midi;
using System.Globalization;
using System.Resources;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace KeppyMIDIConverter
{
    public partial class MainWindow : Form
    {
        public static class Globals
        {
            public static AdvancedSettings frm = new AdvancedSettings();
            public static DSPPROC _myDSP;
            public static SYNCPROC _mySync;
            public static KeppyMIDIConverter.SoundfontDialog frm2 = new KeppyMIDIConverter.SoundfontDialog();
            public static Un4seen.Bass.Misc.DSP_PeakLevelMeter _plm;
            public static bool AutoClearMIDIListEnabled = false;
            public static bool AutoShutDownEnabled = false;
            public static bool DeleteEncoder = false;
            public static bool FXDisabled = true;
            public static bool NoteOff1Event = false;
            public static bool OldTimeThingy = false;
            public static bool PlaybackMode = false;
            public static bool QualityOverride = false;
            public static bool RenderingMode = false;
            public static bool TempoOverride = false;
            public static bool VSTMode = false;
            public static int ActiveVoicesInt = 0;
            public static int AverageCPU;
            public static int Bitrate = 128;
            public static int CancellationPendingValue = 0;
            public static int CurrentEncoder;
            public static int CurrentMode;
            public static int CurrentStatusMaximumInt;
            public static int CurrentStatusValueInt;
            public static int DefaultSoundfont;
            public static int FinalTempo = 120;
            public static int Frequency = 0xbb80;
            public static int LimitVoicesInt = 0x186a0;
            public static int OriginalTempo;
            public static int pictureset = 1;
            public static int SoundFont;
            public static int Time = 0;
            public static int Volume;
            public static int _Encoder;
            public static int _recHandle;
            public static int _Mixer;
            public static int _VSTHandle;
            public static int _VSTHandle2;
            public static int _VSTHandle3;
            public static int _VSTHandle4;
            public static int _VSTHandle5;
            public static int _VSTHandle6;
            public static int _VSTHandle7;
            public static int _VSTHandle8;
            public static int notecount = 0;
            public static int[] SFZBankPreset = new int[0];
            public static string BenchmarkTime;
            public static string CurrentPeak = "0.0 dB";
            public static string CurrentRMS = "0.0 dB";
            public static string CurrentAverage = "0.0 dB";
            public static string CurrentStatusTextString;
            public static string DisabledOr;
            public static string EncoderPath;
            public static string ExportLastDirectory;
            public static string ExportWhereYay;
            public static string MIDILastDirectory;
            public static string MIDIName;
            public static string NewWindowName = null;
            public static string PercentageProgress = "0";
            public static string VSTDLL = null;
            public static string VSTDLL2 = null;
            public static string VSTDLL3 = null;
            public static string VSTDLL4 = null;
            public static string VSTDLL5 = null;
            public static string VSTDLL6 = null;
            public static string VSTDLL7 = null;
            public static string VSTDLL8 = null;
            public static string VSTDLLDesc = null;
            public static string VSTDLLDesc2 = null;
            public static string VSTDLLDesc3 = null;
            public static string VSTDLLDesc4 = null;
            public static string VSTDLLDesc5 = null;
            public static string VSTDLLDesc6 = null;
            public static string VSTDLLDesc7 = null;
            public static string VSTDLLDesc8 = null;
            public static string WAVName;
            public static string[] Soundfonts = new string[0];

            // Other
            public static string ExecutablePath = Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);
        }

        private enum MoveDirection { Up = -1, Down = 1 };

        public MainWindow(String[] args, string encoder, bool deletencoder)
        {          
            InitializeComponent();
            InitializeLanguage();
            Globals.EncoderPath = encoder;
            Globals.DeleteEncoder = deletencoder;
            //To store all the soundfonts that where opened with the application
            List<String> soundfonts = null;
            //Parse through arguments
            foreach (String s in args)
            {
                //Find out is the current argument is a file path/name
                if (File.Exists(s))
                {
                    //Find out it the current file is a MIDI
                    if (s.ToLower().EndsWith(".mid") | s.ToLower().EndsWith(".midi") | s.ToLower().EndsWith(".kar") | s.ToLower().EndsWith(".rmi"))
                    {
                        //Add MIDI to midi list
                        string[] saLvwItem = new string[4];
                        string[] midiinfo = GetMoreInfoMIDI(s);
                        saLvwItem[0] = s;
                        saLvwItem[1] = midiinfo[1];
                        saLvwItem[2] = midiinfo[0];
                        saLvwItem[3] = GetSizeMIDI(s);
                        ListViewItem lvi = new ListViewItem(saLvwItem);
                        this.MIDIList.Items.Add(lvi);
                    }
                    //If the file isnt a MIDI, check if its a soundfont
                    if (s.ToLower().EndsWith(".sf2") | s.ToLower().EndsWith(".sf3") | s.ToLower().EndsWith(".sfpack") | s.ToLower().EndsWith(".sfz"))
                    {
                        //There are soundfonts beeing added to the application so create the list
                        if (soundfonts == null)
                        {
                            soundfonts = new List<String>();
                        }
                        soundfonts.Add(s);
                    }
                }
            }
            //Check if there are soundfonts
            if(soundfonts != null)
            {
                //
                Globals.Soundfonts = new string[soundfonts.Count];
                soundfonts.CopyTo(Globals.Soundfonts, 0);
                foreach(String s in soundfonts)
                {
                    Globals.frm2.SFList.Items.Add(s);
                }
            }
        }

        Timer t1 = new Timer();
        Timer t2 = new Timer();

        public static ResourceManager res_man;    // declare Resource manager to access to specific cultureinfo
        public static CultureInfo cul;            // declare culture info

        [DllImport("dwmapi.dll")]
        private static extern int DwmIsCompositionEnabled(out bool enabled);

        private void InitializeLanguage()
        {
            try {
                res_man = new ResourceManager("KeppyMIDIConverter.Languages.Lang", typeof(MainWindow).Assembly);
                cul = Program.ReturnCulture();
                MIDIList.Columns.Clear();
                MIDIList.Columns.Add(res_man.GetString("MIDIFile", cul), 1, HorizontalAlignment.Left);
                MIDIList.Columns.Add(res_man.GetString("MIDINotes", cul), 1, HorizontalAlignment.Center);
                MIDIList.Columns.Add(res_man.GetString("MIDILength", cul), 1, HorizontalAlignment.Center);
                MIDIList.Columns.Add(res_man.GetString("MIDISize", cul), 1, HorizontalAlignment.Center);
                MIDIList.Columns[0].Tag = 7;
                MIDIList.Columns[1].Tag = 1;
                MIDIList.Columns[2].Tag = 1;
                MIDIList.Columns[3].Tag = 1;
                MIDIList_SizeChanged(MIDIList, new EventArgs());
                ActionsStrip.Text = res_man.GetString("ActionsStrip", cul);
                AdvSettingsButton.Text = res_man.GetString("AdvSettingsButton", cul);
                AudioEventsStrip.Text = res_man.GetString("AudioEventsStrip", cul);
                AutoUpdatesCheckStrip.Text = res_man.GetString("AutoUpdatesCheckStrip", cul);
                AutomaticShutdownStrip.Text = res_man.GetString("AutomaticShutdownStrip", cul);
                ClearListAutomaticallyStrip.Text = res_man.GetString("ClearListAutomaticallyStrip", cul);
                ConvPosOrTimeLeft.Text = res_man.GetString("ConvPosOrTimeLeft", cul);
                FL12Discount.Text = res_man.GetString("FLStudioDiscount", cul);
                HelpStrip.Text = res_man.GetString("HelpStrip", cul);
                KaleidonKep99sGitHubPageToolStripMenuItem.Text = res_man.GetString("KaleidonKep99sGitHubPage", cul);
                KaleidonKep99sYouTubeChannelToolStripMenuItem.Text = res_man.GetString("KaleidonKep99sYouTubeChannel", cul);
                MoveDownItem.Text = res_man.GetString("MoveDOWN", cul);
                MoveUpItem.Text = res_man.GetString("MoveUP", cul);
                OptionsStrip.Text = res_man.GetString("OptionsStrip", cul);
                OverrideStrip.Text = res_man.GetString("OverrideLanguage", cul);
                SettingsBox.Text = res_man.GetString("SettingsBox", cul);
                SortByName.Text = res_man.GetString("SortByName", cul);
                VoiceLabel.Text = res_man.GetString("VoiceLabel", cul);
                abortRenderingToolStripMenuItem.Text = res_man.GetString("AbortConvPlayback", cul);
                clearMIDIsListToolStripMenuItem.Text = ClearMIDIsListRightClick.Text = res_man.GetString("ClearMIDIsList", cul);
                disabledToolStripMenuItem.Text = disabledToolStripMenuItem1.Text = disabledToolStripMenuItem2.Text = disabledToolStripMenuItem3.Text = disabledToolStripMenuItem4.Text = disabledToolStripMenuItem5.Text = res_man.GetString("DisabledText", cul);
                enabledToolStripMenuItem.Text = enabledToolStripMenuItem1.Text = enabledToolStripMenuItem2.Text = enabledToolStripMenuItem3.Text = enabledToolStripMenuItem4.Text = enabledToolStripMenuItem5.Text = res_man.GetString("EnabledText", cul);
                exitToolStripMenuItem.Text = res_man.GetString("ExitStrip", cul);
                forceCloseTheApplicationToolStripMenuItem.Text = res_man.GetString("forceCloseTheApplicationStrip", cul);
                importMIDIsToolStripMenuItem.Text = ImportMIDIsRightClick.Text = res_man.GetString("ImportMIDI", cul);
                informationAboutTheProgramToolStripMenuItem.Text = res_man.GetString("informationAboutTheProgramStrip", cul);
                label3.Text = String.Format("{0} {1}", "🔊", res_man.GetString("Volume", cul));
                openTheSoundfontsManagerToolStripMenuItem.Text = res_man.GetString("SFVSTManager", cul);
                playInRealtimeBetaToolStripMenuItem.Text = res_man.GetString("RenderToSpeakers", cul);
                removeSelectedMIDIsToolStripMenuItem.Text = RemoveMIDIsRightClick.Text = res_man.GetString("RemoveMIDI", cul);
                startRenderingOGGToolStripMenuItem.Text = res_man.GetString("RenderToOGG", cul);
                startRenderingWAVToolStripMenuItem.Text = res_man.GetString("RenderToWAV", cul);
                supportTheDeveloperWithADonationToolStripMenuItem.Text = res_man.GetString("supportTheDeveloperWithADonation", cul);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Keppy's MIDI Converter tried to load an invalid language, so English has been loaded automatically.", "Error with the languages", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                res_man = new ResourceManager("KeppyMIDIConverter.Languages.Lang", typeof(MainWindow).Assembly);
                cul = CultureInfo.CreateSpecificCulture("en");
                MIDIList.Columns.Clear();
                MIDIList.Columns.Add(res_man.GetString("MIDIFile", cul), 434, HorizontalAlignment.Left);
                MIDIList.Columns.Add(res_man.GetString("MIDINotes", cul), 66, HorizontalAlignment.Center);
                MIDIList.Columns.Add(res_man.GetString("MIDILength", cul), 53, HorizontalAlignment.Center);
                MIDIList.Columns.Add(res_man.GetString("MIDISize", cul), 53, HorizontalAlignment.Center);
                ActionsStrip.Text = res_man.GetString("ActionsStrip", cul);
                AdvSettingsButton.Text = res_man.GetString("AdvSettingsButton", cul);
                AudioEventsStrip.Text = res_man.GetString("AudioEventsStrip", cul);
                AutoUpdatesCheckStrip.Text = res_man.GetString("AutoUpdatesCheckStrip", cul);
                AutomaticShutdownStrip.Text = res_man.GetString("AutomaticShutdownStrip", cul);
                ClearListAutomaticallyStrip.Text = res_man.GetString("ClearListAutomaticallyStrip", cul);
                ConvPosOrTimeLeft.Text = res_man.GetString("ConvPosOrTimeLeft", cul);
                FL12Discount.Text = res_man.GetString("FLStudioDiscount", cul);
                HelpStrip.Text = res_man.GetString("HelpStrip", cul);
                KaleidonKep99sGitHubPageToolStripMenuItem.Text = res_man.GetString("KaleidonKep99sGitHubPage", cul);
                KaleidonKep99sYouTubeChannelToolStripMenuItem.Text = res_man.GetString("KaleidonKep99sYouTubeChannel", cul);
                MoveDownItem.Text = res_man.GetString("MoveDOWN", cul);
                MoveUpItem.Text = res_man.GetString("MoveUP", cul);
                OptionsStrip.Text = res_man.GetString("OptionsStrip", cul);
                OverrideStrip.Text = res_man.GetString("OverrideLanguage", cul);
                SettingsBox.Text = res_man.GetString("SettingsBox", cul);
                SortByName.Text = res_man.GetString("SortByName", cul);
                VoiceLabel.Text = res_man.GetString("VoiceLabel", cul);
                abortRenderingToolStripMenuItem.Text = res_man.GetString("AbortConvPlayback", cul);
                clearMIDIsListToolStripMenuItem.Text = ClearMIDIsListRightClick.Text = res_man.GetString("ClearMIDIsList", cul);
                disabledToolStripMenuItem.Text = disabledToolStripMenuItem1.Text = disabledToolStripMenuItem2.Text = disabledToolStripMenuItem3.Text = disabledToolStripMenuItem4.Text = disabledToolStripMenuItem5.Text = res_man.GetString("DisabledText", cul);
                enabledToolStripMenuItem.Text = enabledToolStripMenuItem1.Text = enabledToolStripMenuItem2.Text = enabledToolStripMenuItem3.Text = enabledToolStripMenuItem4.Text = enabledToolStripMenuItem5.Text = res_man.GetString("EnabledText", cul);
                exitToolStripMenuItem.Text = res_man.GetString("ExitStrip", cul);
                forceCloseTheApplicationToolStripMenuItem.Text = res_man.GetString("forceCloseTheApplicationStrip", cul);
                importMIDIsToolStripMenuItem.Text = ImportMIDIsRightClick.Text = res_man.GetString("ImportMIDI", cul);
                informationAboutTheProgramToolStripMenuItem.Text = res_man.GetString("informationAboutTheProgramStrip", cul);
                label3.Text = res_man.GetString("Volume", cul);
                openTheSoundfontsManagerToolStripMenuItem.Text = res_man.GetString("SFVSTManager", cul);
                playInRealtimeBetaToolStripMenuItem.Text = res_man.GetString("RenderToSpeakers", cul);
                removeSelectedMIDIsToolStripMenuItem.Text = RemoveMIDIsRightClick.Text = res_man.GetString("RemoveMIDI", cul);
                startRenderingOGGToolStripMenuItem.Text = res_man.GetString("RenderToOGG", cul);
                startRenderingWAVToolStripMenuItem.Text = res_man.GetString("RenderToWAV", cul);
                supportTheDeveloperWithADonationToolStripMenuItem.Text = res_man.GetString("supportTheDeveloperWithADonation", cul);
            }
        }

        private void MainWindow_Load(object sender, EventArgs e)
        {
            BassNet.Registration("kaleidonkep99@outlook.com", "2X203132524822");
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.DoubleBuffer, true);
            this.Menu = DefaultMenu;
            MIDIList.ContextMenu = DefMenu;
            // Fade in
            t1.Interval = 1; // Increases opacity every 10ms
            t1.Tick += new EventHandler(fadeIn);  // This calls the function that changes opacity 
            t1.Start();
            // Fade in
            if (Environment.OSVersion.Version.Major == 5 && Environment.OSVersion.Version.Minor == 1)
            {
                if (Environment.OSVersion.Version.Build == 2600)
                {
                    // Continues
                }
                else
                {
                    // If you're using Windows XP SP1 or older, the converter will close itself.
                    KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("WinXPSP1NotSupportedTitle", cul), res_man.GetString("WinXPSP1NotSupportedMessage", cul), 1, 0);
                    errordialog.ShowDialog();
                    this.Hide();
                    Application.ExitThread();
                }
            }
            else
            {
                try
                {
                    Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter");
                    Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages");
                    Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings");
                    RegistryKey Key = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter");
                    if (Key != null)
                    {
                        RegistryKey Language = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", true);
                        try
                        {
                            if (Convert.ToInt32(Language.GetValue("langoverride", 0)) == 1)
                            {
                                enabledToolStripMenuItem5.PerformClick();
                            }
                            else
                            {
                                disabledToolStripMenuItem5.PerformClick();
                            }
                        }
                        catch
                        {
                            enabledToolStripMenuItem5.Checked = false;
                            disabledToolStripMenuItem5.Checked = true;
                            Language.SetValue("langoverride", "0", RegistryValueKind.DWord);
                        }
                        RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                        try
                        {
                            // Generic settings
                            VoiceLimit.Value = Convert.ToInt32(Settings.GetValue("voices", Convert.ToDecimal(1000)));
                            VolumeBar.Value = Convert.ToInt32(Settings.GetValue("volume", 10000));
                            Globals.Volume = Convert.ToInt32(Settings.GetValue("volume", 10000));
                            Globals.Frequency = Convert.ToInt32(Settings.GetValue("audiofreq", 44100));
                            Globals.Bitrate = Convert.ToInt32(Settings.GetValue("oggbitrate", 256));
                            // Audio events
                            if (Convert.ToInt32(Settings.GetValue("audioevents", 1)) == 1)
                            {
                                enabledToolStripMenuItem4.Checked = true;
                                disabledToolStripMenuItem4.Checked = false;
                                Settings.SetValue("audioevents", "1", RegistryValueKind.DWord);
                            }
                            else
                            {
                                enabledToolStripMenuItem4.Checked = false;
                                disabledToolStripMenuItem4.Checked = true;
                                Settings.SetValue("audioevents", "0", RegistryValueKind.DWord);
                            }
                            // Autoupdate lel
                            if (Convert.ToInt32(Settings.GetValue("autoupdatecheck", 1)) == 1)
                            {
                                enabledToolStripMenuItem3.Checked = true;
                                disabledToolStripMenuItem3.Checked = false;
                                Settings.SetValue("autoupdatecheck", "1", RegistryValueKind.DWord);
                            }
                            else
                            {
                                enabledToolStripMenuItem3.Checked = false;
                                disabledToolStripMenuItem3.Checked = true;
                                Settings.SetValue("autoupdatecheck", "0", RegistryValueKind.DWord);
                            }
                            // Old time thingy for TheGhastModding lel
                            if (Convert.ToInt32(Settings.GetValue("oldtimethingy", 0)) == 1)
                            {
                                enabledToolStripMenuItem2.Checked = true;
                                disabledToolStripMenuItem2.Checked = false;
                                Globals.OldTimeThingy = true;
                                Settings.SetValue("oldtimethingy", "1", RegistryValueKind.DWord);
                            }
                            else
                            {
                                enabledToolStripMenuItem2.Checked = false;
                                disabledToolStripMenuItem2.Checked = true;
                                Globals.OldTimeThingy = false;
                                Settings.SetValue("oldtimethingy", "0", RegistryValueKind.DWord);
                            }
                            // Note off setting
                            if (Convert.ToInt32(Settings.GetValue("noteoff1", 0)) == 1)
                            {
                                Globals.NoteOff1Event = true;
                                Settings.SetValue("noteoff1", "1", RegistryValueKind.DWord);
                            }
                            else
                            {
                                Globals.NoteOff1Event = false;
                                Settings.SetValue("noteoff1", "0", RegistryValueKind.DWord);
                            }
                            // BASS default sound effects (Reverb and chorus)
                            if (Convert.ToInt32(Settings.GetValue("disablefx", 0)) == 1)
                            {
                                Globals.FXDisabled = true;
                                Settings.SetValue("disablefx", "1", RegistryValueKind.DWord);
                            }
                            else
                            {
                                Globals.FXDisabled = false;
                                Settings.SetValue("disablefx", "0", RegistryValueKind.DWord);
                            }
                            // OGG bitrate override
                            if (Convert.ToInt32(Settings.GetValue("overrideogg", 0)) == 1)
                            {
                                Globals.QualityOverride = true;
                                Settings.SetValue("overrideogg", "1", RegistryValueKind.DWord);
                            }
                            else
                            {
                                Globals.QualityOverride = false;
                                Settings.SetValue("overrideogg", "0", RegistryValueKind.DWord);
                            }
                            Globals.MIDILastDirectory = Settings.GetValue("lastmidifolder", System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)).ToString();
                            Globals.ExportLastDirectory = Settings.GetValue("lastexportfolder", System.Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory)).ToString();
                            Settings.Close();
                        }
                        catch (Exception exception)
                        {
                            KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("FatalError", cul), exception.ToString(), 1, 0);
                            errordialog.ShowDialog();
                            Settings.Close();
                        }
                    }
                    else if (Key == null)
                    {
                        Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings");
                        RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                        VoiceLimit.Value = 100000;
                        Settings.SetValue("voices", "100000", RegistryValueKind.DWord);
                        Settings.SetValue("lastmidifolder", "", RegistryValueKind.String);
                        Settings.SetValue("lastsffolder", "", RegistryValueKind.String);
                        Settings.SetValue("lastexportfolder", "", RegistryValueKind.String);
                        Settings.SetValue("audioevents", "1", RegistryValueKind.DWord);
                        Settings.SetValue("autoupdatecheck", "1", RegistryValueKind.DWord);
                        Settings.SetValue("oldtimethingy", "0", RegistryValueKind.DWord);
                        Settings.SetValue("noteoff1", "0", RegistryValueKind.DWord);
                        Settings.SetValue("disablefx", "0", RegistryValueKind.DWord);
                        Settings.SetValue("maxcpu", "0", RegistryValueKind.DWord);
                        Settings.SetValue("audiofreq", "44100", RegistryValueKind.DWord);
                        Settings.SetValue("volume", "10000", RegistryValueKind.DWord);
                        Settings.Close();
                    }
                }
                catch (Exception exception2)
                {
                    KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("Error", cul), exception2.ToString(), 0, 0);
                    errordialog.ShowDialog();
                }
                Globals.AutoShutDownEnabled = false;
                Globals.AutoClearMIDIListEnabled = false;
            }
        }

        private void startRenderingWAVToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.RenderingMode = true;
            this.loadingpic.Visible = true;
            this.ExportWhere.FileName = res_man.GetString("SaveHere", cul);
            this.ExportWhere.InitialDirectory = Globals.ExportLastDirectory;
            this.ExportWhere.Title = res_man.GetString("ExportWhere", cul);
            Globals.CurrentStatusTextString = null;
            if (this.ExportWhere.ShowDialog() == DialogResult.OK)
            {
                Globals.CurrentStatusTextString = null;
                Globals.ExportWhereYay = Path.GetDirectoryName(this.ExportWhere.FileName);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Globals.ExportLastDirectory = Path.GetDirectoryName(ExportWhere.FileName);
                Settings.SetValue("lastexportfolder", Globals.ExportLastDirectory);
                Settings.Close();
                ExportWhere.InitialDirectory = Globals.ExportLastDirectory;
                Globals.CurrentEncoder = 0;
                this.ConverterProcess.RunWorkerAsync();
            }
            else
            {
                Globals.RenderingMode = false;
                this.loadingpic.Visible = false;
            }
        }


        private void startRenderingOGGToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.RenderingMode = true;
            this.loadingpic.Visible = true;
            this.ExportWhere.FileName = res_man.GetString("SaveHere", cul);
            this.ExportWhere.InitialDirectory = Globals.ExportLastDirectory;
            this.ExportWhere.Title = res_man.GetString("ExportWhere", cul);
            if (this.ExportWhere.ShowDialog() == DialogResult.OK)
            {
                Globals.CurrentStatusTextString = null;
                Globals.ExportWhereYay = Path.GetDirectoryName(this.ExportWhere.FileName);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Globals.ExportLastDirectory = Path.GetDirectoryName(ExportWhere.FileName);
                Settings.SetValue("lastexportfolder", Globals.ExportLastDirectory);
                Settings.Close();
                ExportWhere.InitialDirectory = Globals.ExportLastDirectory;
                Globals.CurrentEncoder = 1;
                this.ConverterProcess.RunWorkerAsync();
            }
            else
            {
                Globals.RenderingMode = false;
                this.loadingpic.Visible = false;
            }
        }

        private void abortRenderingToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.CancellationPendingValue = 1;
            Globals.AutoShutDownEnabled = false;
            this.startRenderingWAVToolStripMenuItem.Enabled = true;
            this.startRenderingOGGToolStripMenuItem.Enabled = true;
            this.playInRealtimeBetaToolStripMenuItem.Enabled = true;
            this.abortRenderingToolStripMenuItem.Enabled = false;
        }

        private void BASSInitSystem(int type)
        {
            if (type == 0)
            {
                Bass.BASS_StreamFree(Globals._recHandle);
                Bass.BASS_Free();
                Bass.BASS_Init(0, Globals.Frequency, BASSInit.BASS_DEVICE_NOSPEAKER, IntPtr.Zero);
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_MIDI_VOICES, 100000);
            }
            else
            {
                Bass.BASS_StreamFree(Globals._recHandle);
                Bass.BASS_Free();
                Bass.BASS_Init(-1, Globals.Frequency, BASSInit.BASS_DEVICE_LATENCY, IntPtr.Zero);
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATEPERIOD, 0);
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_UPDATETHREADS, 0);
                BASS_INFO info = Bass.BASS_GetInfo();
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_BUFFER, info.minbuf + 10 + 50);
                Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_MIDI_VOICES, 2000);
            }
        }

        private void BASSVSTShowDialog(int towhichstream, int whichvst, BASS_VST_INFO vstInfo)
        {
            Form f = new Form();
            f.Width = vstInfo.editorWidth + 4;
            f.Height = vstInfo.editorHeight + 34;
            f.FormBorderStyle = FormBorderStyle.FixedDialog;
            f.Text = res_man.GetString("DSPSettings", cul) + vstInfo.effectName;
            f.StartPosition = FormStartPosition.CenterScreen;
            f.MaximizeBox = false;
            f.MinimizeBox = false;
            BassVst.BASS_VST_EmbedEditor(whichvst, f.Handle);
            try
            {
                f.ShowDialog();
                BassVst.BASS_VST_EmbedEditor(whichvst, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                sb.AppendLine("==== Start of Keppy's MIDI Converter Error ====");
                sb.AppendLine(ex.ToString());
                sb.AppendLine("====  End of Keppy's MIDI Converter Error  ====");
                System.Threading.Thread thread = new System.Threading.Thread(() => Clipboard.SetText(sb.ToString()));
                thread.SetApartmentState(System.Threading.ApartmentState.STA);
                thread.Start();
                thread.Join();
                MessageBox.Show(String.Format(res_man.GetString("VSTInvalidCallError", cul), vstInfo.effectName, ex.ToString()), "Keppy's MIDI Converter - " + res_man.GetString("VSTInvalidCallTitle", cul), MessageBoxButtons.OK, MessageBoxIcon.Error);
                BassVst.BASS_VST_EmbedEditor(whichvst, IntPtr.Zero);
                BassVst.BASS_VST_ChannelRemoveDSP(towhichstream, whichvst);
            }
        }

        private void BASSVSTInit(int towhichstream)
        {
            if (Globals.VSTMode == true)
            {
                Globals._VSTHandle = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL, BASSVSTDsp.BASS_VST_DEFAULT, 1);
                Globals._VSTHandle2 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL2, BASSVSTDsp.BASS_VST_DEFAULT, 2);
                Globals._VSTHandle3 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL3, BASSVSTDsp.BASS_VST_DEFAULT, 3);
                Globals._VSTHandle4 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL4, BASSVSTDsp.BASS_VST_DEFAULT, 4);
                Globals._VSTHandle5 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL5, BASSVSTDsp.BASS_VST_DEFAULT, 5);
                Globals._VSTHandle6 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL6, BASSVSTDsp.BASS_VST_DEFAULT, 6);
                Globals._VSTHandle7 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL7, BASSVSTDsp.BASS_VST_DEFAULT, 7);
                Globals._VSTHandle8 = BassVst.BASS_VST_ChannelSetDSP(towhichstream, Globals.VSTDLL8, BASSVSTDsp.BASS_VST_DEFAULT, 8);
                BASS_VST_INFO vstInfo = new BASS_VST_INFO();
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle2, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle2, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle3, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle3, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle4, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle4, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle5, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle5, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle6, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle6, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle7, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle7, vstInfo);
                }
                if (BassVst.BASS_VST_GetInfo(Globals._VSTHandle8, vstInfo) && vstInfo.hasEditor)
                {
                    BASSVSTShowDialog(towhichstream, Globals._VSTHandle8, vstInfo);
                }
            }
        }

        private void BASSStreamSystem(String str, int type)
        {
            Microsoft.Win32.RegistryKey Settings = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", false);
            if (type == 0)
                Globals._recHandle = BassMidi.BASS_MIDI_StreamCreateFile(str, 0L, 0L, BASSFlag.BASS_STREAM_DECODE | BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_MIDI_DECAYEND, Globals.Frequency);
            else
                Globals._recHandle = BassMidi.BASS_MIDI_StreamCreateFile(str, 0L, 0L, BASSFlag.BASS_SAMPLE_FLOAT | BASSFlag.BASS_MIDI_DECAYEND, Globals.Frequency);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, Globals.Volume);
            Bass.BASS_ChannelSetAttribute(Globals._recHandle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES, Globals.LimitVoicesInt);
            Bass.BASS_ChannelSetAttribute(Globals._recHandle, BASSAttribute.BASS_ATTRIB_MIDI_CPU, 0);
            if (Path.GetFileNameWithoutExtension(str).Length >= 49)
                Globals.NewWindowName = Path.GetFileNameWithoutExtension(str).Truncate(45);
            else
                Globals.NewWindowName = Path.GetFileNameWithoutExtension(str);
            BASS_MIDI_FONTEX[] fonts = new BASS_MIDI_FONTEX[Globals.Soundfonts.Length];
            int sfnum = 0;
            int sfzload = 0;
            List<int> termsList = new List<int>();
            foreach (string s in Globals.Soundfonts)
            {
                if (Path.GetExtension(s).ToLower() == ".sfz")
                {
                    if (Globals.SFZBankPreset.Length < 1)
                    {
                        int sbank;
                        int spreset;
                        int dbank;
                        int dpreset;
                        using (var form = new Forms.BankNPresetSel(s, 0, 1))
                        {
                            var result = form.ShowDialog();

                            if (result == DialogResult.OK)
                            {
                                sbank = form.BankValueReturn;
                                spreset = form.PresetValueReturn;
                                dbank = form.DesBankValueReturn;
                                dpreset = form.DesPresetValueReturn;
                            }
                            else
                            {
                                sbank = 0;
                                spreset = 0;
                                dbank = 0;
                                dpreset = 0;
                            }
                        }
                        for (int runs = 0; runs < 4; runs++)
                        {
                            termsList.Add(sbank);
                            termsList.Add(spreset);
                            termsList.Add(dbank);
                            termsList.Add(dpreset);
                        }
                        fonts[sfnum].font = BassMidi.BASS_MIDI_FontInit(s);
                        fonts[sfnum].spreset = spreset;
                        fonts[sfnum].sbank = sbank;
                        fonts[sfnum].dpreset = dpreset;
                        fonts[sfnum].dbank = dbank;
                        if (type == 0)
                            BassMidi.BASS_MIDI_FontSetVolume(fonts[sfnum].font, ((float)Globals.Volume / 10000));
                        BassMidi.BASS_MIDI_StreamSetFonts(Globals._recHandle, fonts, sfnum + 1);
                        sfnum += 1;
                    }
                    else
                    {
                        int sbank = Globals.SFZBankPreset[sfzload];
                        sfzload += 1;
                        int spreset = Globals.SFZBankPreset[sfzload];
                        sfzload += 1;
                        int dbank = Globals.SFZBankPreset[sfzload];
                        sfzload += 1;
                        int dpreset = Globals.SFZBankPreset[sfzload];
                        sfzload += 1;
                        fonts[sfnum].font = BassMidi.BASS_MIDI_FontInit(s);
                        fonts[sfnum].spreset = spreset;
                        fonts[sfnum].sbank = sbank;
                        fonts[sfnum].dpreset = dpreset;
                        fonts[sfnum].dbank = dbank;
                        if (type == 0)
                            BassMidi.BASS_MIDI_FontSetVolume(fonts[sfnum].font, ((float)Globals.Volume / 10000));
                        BassMidi.BASS_MIDI_StreamSetFonts(Globals._recHandle, fonts, sfnum + 1);
                        sfnum += 1;
                    }
                }
                else
                {
                    fonts[sfnum].font = BassMidi.BASS_MIDI_FontInit(s);
                    fonts[sfnum].spreset = -1;
                    fonts[sfnum].sbank = -1;
                    fonts[sfnum].dpreset = -1;
                    fonts[sfnum].dbank = 0;
                    BassMidi.BASS_MIDI_FontSetVolume(fonts[sfnum].font, ((float)Globals.Volume / 10000));
                    BassMidi.BASS_MIDI_StreamSetFonts(Globals._recHandle, fonts, sfnum + 1);
                    sfnum += 1;
                }
            }
            Globals.SFZBankPreset = termsList.ToArray();
            Globals._plm = new Un4seen.Bass.Misc.DSP_PeakLevelMeter(Globals._recHandle, 1);
            Globals._plm.CalcRMS = true;
            BassMidi.BASS_MIDI_StreamLoadSamples(Globals._recHandle);
        }

        private void BASSEffectSettings()
        {
            if (Globals.FXDisabled == true)
            {
                Bass.BASS_ChannelFlags(Globals._recHandle, BASSFlag.BASS_MIDI_NOFX, BASSFlag.BASS_MIDI_NOFX);
            }
            else
            {
                Bass.BASS_ChannelFlags(Globals._recHandle, 0, BASSFlag.BASS_MIDI_NOFX);
            }
            if (Globals.NoteOff1Event == true)
            {
                Bass.BASS_ChannelFlags(Globals._recHandle, BASSFlag.BASS_MIDI_NOTEOFF1, BASSFlag.BASS_MIDI_NOTEOFF1);
            }
            else
            {
                Bass.BASS_ChannelFlags(Globals._recHandle, 0, BASSFlag.BASS_MIDI_NOTEOFF1);
            }
        }

        private void BASSEncoderInit(Int32 stream, Int32 format, String str)
        {
            string path;
            string audiopath = Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + ".ogg";
            int num3 = 1;
            if (format == 0)
            {
                if (File.Exists(Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + ".wav"))
                {
                    do
                    {
                        path = Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + " (Copy " + num3.ToString() + ").wav";
                        ++num3;
                    } while (File.Exists(path));
                    BassEnc.BASS_Encode_Stop(Globals._recHandle);
                    Globals._Encoder = BassEnc.BASS_Encode_Start(stream, path, BASSEncode.BASS_ENCODE_AUTOFREE | BASSEncode.BASS_ENCODE_PCM, null, IntPtr.Zero);
                }
                else
                {
                    BassEnc.BASS_Encode_Stop(Globals._recHandle);
                    Globals._Encoder = BassEnc.BASS_Encode_Start(stream, Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + ".wav", BASSEncode.BASS_ENCODE_AUTOFREE | BASSEncode.BASS_ENCODE_PCM, null, IntPtr.Zero);
                }
            }
            else if (format == 1)
            {
                if (File.Exists(audiopath))
                {
                    do
                    {
                        if (Globals.QualityOverride == true)
                        {
                            path = String.Format("{0} --managed -b {1} -m {1} -M {1} - -o \"{2}\"", Globals.EncoderPath, Globals.Bitrate.ToString(), Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + " (Copy " + num3.ToString() + ").ogg");
                            audiopath = Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + " (Copy " + num3.ToString() + ").ogg";
                        }
                        else
                        {
                            path = String.Format("{0} - -o \"{1}\"", Globals.EncoderPath, Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + " (Copy " + num3.ToString() + ").ogg");
                            audiopath = Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + " (Copy " + num3.ToString() + ").ogg";
                        }
                        ++num3;
                    } while (File.Exists(audiopath));
                    foreach (Process proc in Process.GetProcessesByName(Globals.EncoderPath))
                    {
                        proc.Kill();
                    }
                    BassEnc.BASS_Encode_Stop(Globals._recHandle);
                    Globals._Encoder = BassEnc.BASS_Encode_Start(stream, path, BASSEncode.BASS_ENCODE_AUTOFREE, null, IntPtr.Zero);
                }
                else
                {
                    if (Globals.QualityOverride == true)
                    {
                        foreach (Process proc in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Globals.EncoderPath)))
                        {
                            proc.Kill();
                        }
                        BassEnc.BASS_Encode_Stop(Globals._recHandle);
                        Globals._Encoder = BassEnc.BASS_Encode_Start(stream, String.Format("{0} --managed -b {1} -m {1} -M {1} - -o \"{2}\"", Globals.EncoderPath, Globals.Bitrate.ToString(),  Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + ".ogg"), BASSEncode.BASS_ENCODE_AUTOFREE, null, IntPtr.Zero);
                    }
                    else
                    {
                        foreach (Process proc in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(Globals.EncoderPath)))
                        {
                            proc.Kill();
                        }
                        BassEnc.BASS_Encode_Stop(Globals._recHandle);
                        Globals._Encoder = BassEnc.BASS_Encode_Start(stream, String.Format("{0} - -o \"{1}\"", Globals.EncoderPath, Globals.ExportWhereYay + @"\" + Path.GetFileNameWithoutExtension(str) + ".ogg"), BASSEncode.BASS_ENCODE_AUTOFREE, null, IntPtr.Zero);
                    }
                }
            }
        }

        private int BASSPlayBackEngine(int notes, long pos)
        {
            int pnotes = notes;
            int tempo = BassMidi.BASS_MIDI_StreamGetEvent(Globals._recHandle, 0, BASSMIDIEvent.MIDI_EVENT_TEMPO);
            Globals.OriginalTempo = 60000000 / tempo;
            if (MainWindow.Globals.TempoOverride == true)
                BassMidi.BASS_MIDI_StreamEvent(Globals._recHandle, 0, BASSMIDIEvent.MIDI_EVENT_TEMPO, 60000000 / Globals.FinalTempo);
            if (Globals.FXDisabled == true)
                Bass.BASS_ChannelFlags(Globals._recHandle, BASSFlag.BASS_MIDI_NOFX, BASSFlag.BASS_MIDI_NOFX);
            else
                Bass.BASS_ChannelFlags(Globals._recHandle, 0, BASSFlag.BASS_MIDI_NOFX);
            if (Globals.NoteOff1Event == true)
                Bass.BASS_ChannelFlags(Globals._recHandle, BASSFlag.BASS_MIDI_NOTEOFF1, BASSFlag.BASS_MIDI_NOTEOFF1);
            else
                Bass.BASS_ChannelFlags(Globals._recHandle, 0, BASSFlag.BASS_MIDI_NOTEOFF1);
            Bass.BASS_SetConfig(BASSConfig.BASS_CONFIG_GVOL_STREAM, Globals.Volume);
            Bass.BASS_ChannelSetAttribute(Globals._recHandle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES, Globals.LimitVoicesInt);
            long num6 = Bass.BASS_ChannelGetPosition(Globals._recHandle);
            float num7 = ((float)pos) / 1048576f;
            float num8 = ((float)num6) / 1048576f;
            double num9 = Bass.BASS_ChannelBytes2Seconds(Globals._recHandle, pos);
            double num10 = Bass.BASS_ChannelBytes2Seconds(Globals._recHandle, num6);
            TimeSpan span = TimeSpan.FromSeconds(num9);
            TimeSpan span2 = TimeSpan.FromSeconds(num10);
            string str4 = span.Minutes.ToString() + ":" + span.Seconds.ToString().PadLeft(2, '0') + "." + span.Milliseconds.ToString().PadLeft(3, '0');
            string str5 = span2.Minutes.ToString() + ":" + span2.Seconds.ToString().PadLeft(2, '0') + "." + span2.Milliseconds.ToString().PadLeft(3, '0');
            float percentage = num8 / num7;
            float percentagefinal;
            if (percentage * 100 < 0)
                percentagefinal = 0.0f;
            else if (percentage * 100 > 100)
                percentagefinal = 1.0f;
            else
                percentagefinal = percentage;
            Globals.PercentageProgress = percentagefinal.ToString("0.0%");
            float num11 = 0f;
            Bass.BASS_ChannelGetAttribute(Globals._recHandle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES_ACTIVE, ref num11);
            Globals.CurrentStatusTextString = String.Format(res_man.GetString("PlaybackStatus", cul), percentage.ToString("0%"), str5, str4, Globals.notecount.ToString("N0"), pnotes.ToString("N0"));
            Globals.ActiveVoicesInt = Convert.ToInt32(num11);
            Globals.CurrentStatusMaximumInt = Convert.ToInt32((long)(pos / 0x100000L));
            Globals.CurrentStatusValueInt = Convert.ToInt32((long)(num6 / 0x100000L));
            Bass.BASS_ChannelUpdate(Globals._recHandle, 0);
            System.Threading.Thread.Sleep(1);
            return pnotes;
        }

        private void BASSEncodingEngine(long pos, int length, DateTime starttime)
        {
            System.Threading.Thread.Sleep(1);
            TimeSpan timespent = DateTime.Now - starttime;
            long num6 = Bass.BASS_ChannelGetPosition(Globals._recHandle);
            float num7 = ((float)pos) / 1048576f;
            float num8 = ((float)num6) / 1048576f;
            double num9 = Bass.BASS_ChannelBytes2Seconds(Globals._recHandle, pos);
            double num10 = Bass.BASS_ChannelBytes2Seconds(Globals._recHandle, num6);
            TimeSpan span = TimeSpan.FromSeconds(num9);
            TimeSpan span2 = TimeSpan.FromSeconds(num10);
            string str4 = span.Minutes.ToString() + ":" + span.Seconds.ToString().PadLeft(2, '0') + "." + span.Milliseconds.ToString().PadLeft(3, '0');
            string str5 = span2.Minutes.ToString() + ":" + span2.Seconds.ToString().PadLeft(2, '0') + "." + span2.Milliseconds.ToString().PadLeft(3, '0');
            float num11 = 0f;
            float num12 = 0f;
            Bass.BASS_ChannelGetAttribute(Globals._recHandle, BASSAttribute.BASS_ATTRIB_MIDI_VOICES_ACTIVE, ref num11);
            Bass.BASS_ChannelGetAttribute(Globals._recHandle, BASSAttribute.BASS_ATTRIB_CPU, ref num12);
            Globals.ActiveVoicesInt = Convert.ToInt32(num11);
            Globals.CurrentStatusMaximumInt = Convert.ToInt32((long)(pos / 0x100000L));
            Globals.CurrentStatusValueInt = Convert.ToInt32((long)(num6 / 0x100000L));
            int secondsremaining = (int)(timespent.TotalSeconds / (int)num6 * ((int)pos - (int)num6));
            TimeSpan span3 = TimeSpan.FromSeconds(secondsremaining);
            string str6 = span3.Minutes.ToString() + ":" + span3.Seconds.ToString().PadLeft(2, '0');
            string str7 = timespent.Minutes.ToString() + ":" + timespent.Seconds.ToString().PadLeft(2, '0');
            float percentage = num8 / num7;
            float percentagefinal;
            if (percentage * 100 < 0)
                percentagefinal = 0.0f;
            else if (percentage * 100 > 100)
                percentagefinal = 1.0f;
            else
                percentagefinal = percentage;
            Globals.PercentageProgress = percentagefinal.ToString("0.0%");
            float[] buffer = new float[length / 4];
            if (MainWindow.Globals.TempoOverride == true)
            {
                BassMidi.BASS_MIDI_StreamEvent(Globals._recHandle, 0, BASSMIDIEvent.MIDI_EVENT_NOTE, Globals.FinalTempo);
            }

            Bass.BASS_ChannelGetData(Globals._recHandle, buffer, length);

            if (num12 < 100f)
            {
                if (Globals.OldTimeThingy == false)
                    Globals.CurrentStatusTextString = String.Format(res_man.GetString("ConvStatusFasterNew", cul), num8.ToString("0.00"), percentagefinal.ToString("0.0%"), str6, str7, Convert.ToInt32(num12).ToString(), ((float)(100f / num12)).ToString("0.0"));
                else
                    Globals.CurrentStatusTextString = String.Format(res_man.GetString("ConvStatusFasterOld", cul), num8.ToString("0.00"), percentagefinal.ToString("0.0%"), str5, str4, Convert.ToInt32(num12).ToString(), ((float)(100f / num12)).ToString("0.0"));
            }
            else if (num12 == 100f)
            {
                if (Globals.OldTimeThingy == false)
                    Globals.CurrentStatusTextString = String.Format(res_man.GetString("ConvStatusNormalNew", cul), num8.ToString("0.00"), percentagefinal.ToString("0.0%"), str6, str7, Convert.ToInt32(num12).ToString());
                else
                    Globals.CurrentStatusTextString = String.Format(res_man.GetString("ConvStatusNormalOld", cul), num8.ToString("0.00"), percentagefinal.ToString("0.0%"), str5, str4, Convert.ToInt32(num12).ToString());
            }
            else if (num12 > 100f)
            {
                if (Globals.OldTimeThingy == false)
                    Globals.CurrentStatusTextString = String.Format(res_man.GetString("ConvStatusSlowerNew", cul), num8.ToString("0.00"), percentagefinal.ToString("0.0%"), str6, str7, Convert.ToInt32(num12).ToString(), ((float)(num12 / 100f)).ToString("0.0"));
                else
                    Globals.CurrentStatusTextString = String.Format(res_man.GetString("ConvStatusSlowerOld", cul), num8.ToString("0.00"), percentagefinal.ToString("0.0%"), str5, str4, Convert.ToInt32(num12).ToString(), ((float)(num12 / 100f)).ToString("0.0"));
            }
        }

        private delegate ListView.ListViewItemCollection GetItems(ListView lstview);

        private ListView.ListViewItemCollection getListViewItems(ListView lstview)
        {
            ListView.ListViewItemCollection temp = new ListView.ListViewItemCollection(new ListView());
            if (!lstview.InvokeRequired)
            {
                foreach (ListViewItem item in lstview.Items)
                    temp.Add((ListViewItem)item.Clone());
                return temp;
            }
            else
                return (ListView.ListViewItemCollection)this.Invoke(new GetItems(getListViewItems), new object[] { lstview });
        }

        private void BASSCloseStream(string message, string title, int type) {
            BassEnc.BASS_Encode_Stop(Globals._Encoder);
            Bass.BASS_StreamFree(Globals._recHandle);
            Bass.BASS_Free();
            Globals.CurrentStatusTextString = message;
            Globals.ActiveVoicesInt = 0;
            Globals.NewWindowName = null;
            if (type == 0)
            {
                MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                PlayConversionStop();
            }
            Globals.CancellationPendingValue = 0;
            Globals.CurrentStatusTextString = null;
            Globals.SFZBankPreset = new int[0];
        }

        private void BASSCloseStreamCrash(Exception ex)
        {
            BassEnc.BASS_Encode_Stop(Globals._Encoder);
            Bass.BASS_StreamFree(Globals._recHandle);
            Bass.BASS_Free();
            Globals.NewWindowName = null;
            Globals.RenderingMode = false;
            Globals.SFZBankPreset = new int[0];
            KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("Error", cul), ex.ToString(), 0, 1);
            errordialog.ShowDialog();
        }

        private void clearMIDIsListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.MIDIList.Items.Clear();
        }

        private void ConverterProcess_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                try
                {
                    PlayConversionStart();
                    Microsoft.Win32.RegistryKey Settings = Microsoft.Win32.Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", false);
                    bool KeepLooping = true;
                    BASSInitSystem(0);
                    while (KeepLooping)
                    {
                        foreach (ListViewItem itemerino in getListViewItems(MIDIList))
                        {
                            string str = itemerino.Text;
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(str);
                            string encpath = null;
                            BASSStreamSystem(str, 0);
                            BASSVSTInit(Globals._recHandle);
                            BASSEffectSettings();
                            BASSEncoderInit(Globals._recHandle, Globals.CurrentEncoder, str);
                            DateTime starttime = DateTime.Now;
                            long pos = Bass.BASS_ChannelGetLength(Globals._recHandle);
                            int length = Convert.ToInt32(Bass.BASS_ChannelSeconds2Bytes(Globals._recHandle, 0.0275));
                            while (Bass.BASS_ChannelIsActive(Globals._recHandle) == BASSActive.BASS_ACTIVE_PLAYING)
                            {
                                if (Globals.CancellationPendingValue != 1)
                                {
                                    BASSEncodingEngine(pos, length, starttime);
                                }
                                else if (Globals.CancellationPendingValue == 1)
                                {
                                    BASSCloseStream(res_man.GetString("ConversionAborted", cul), res_man.GetString("ConversionAborted", cul), 0);
                                    KeepLooping = false;
                                    break;
                                }
                            }
                            if (Globals.CancellationPendingValue == 1)
                            {
                                Globals.RenderingMode = false;
                                KeepLooping = false;
                                if (KeepLooping == false)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (Globals.CancellationPendingValue == 1)
                        {
                            BASSCloseStream(res_man.GetString("ConversionAborted", cul), res_man.GetString("ConversionAborted", cul), 0);
                            KeepLooping = false;
                            Globals.RenderingMode = false;
                            PlayConversionStop();
                        }
                        else
                        {
                            BASSCloseStream(res_man.GetString("ConversionCompleted", cul), res_man.GetString("ConversionCompleted", cul), 1);
                            KeepLooping = false;
                            Globals.RenderingMode = false;
                            if (Globals.AutoShutDownEnabled == true)
                            {
                                var psi = new ProcessStartInfo("shutdown", "/s /t 0");
                                psi.CreateNoWindow = true;
                                psi.UseShellExecute = false;
                                Process.Start(psi);
                            }
                            if (Globals.AutoClearMIDIListEnabled == true)
                            {
                                this.Invoke((MethodInvoker)delegate
                                {
                                    MIDIList.Items.Clear();
                                });
                                PlayConversionStop();
                            }
                            else
                            {
                                PlayConversionStop();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    BASSCloseStreamCrash(exception);
                }
            }
            catch (Exception exception2)
            {
                BASSCloseStreamCrash(exception2);
            }
        }

        private void RealTimePlayBackBeta_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                try
                {
                    PlayConversionStart();
                    bool KeepLooping = true;
                    BASSInitSystem(1);
                    while (KeepLooping)
                    {
                        foreach (ListViewItem itemerino in getListViewItems(MIDIList))
                        {
                            string str = itemerino.Text;
                            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(str);
                            string encpath = null;
                            BASSStreamSystem(str, 1);
                            BASSVSTInit(Globals._recHandle);
                            BASSEffectSettings();
                            Bass.BASS_ChannelPlay(Globals._recHandle, false);
                            long pos = Bass.BASS_ChannelGetLength(Globals._recHandle);
                            int count = BassMidi.BASS_MIDI_StreamGetEvents(Globals._recHandle, -1, BASSMIDIEvent.MIDI_EVENT_NOTE, null);
                            BASS_MIDI_EVENT[] events = new BASS_MIDI_EVENT[count];
                            BassMidi.BASS_MIDI_StreamGetEvents(Globals._recHandle, -1, BASSMIDIEvent.MIDI_EVENT_NOTE, events);
                            int notes = 0;
                            for (int a = 0; a < count; a++) { if ((events[a].param & 0xff00) != 0) { notes++; } }
                            Globals._mySync = new SYNCPROC(NoteSyncProc);
                            Bass.BASS_ChannelSetSync(Globals._recHandle, BASSSync.BASS_SYNC_MIDI_EVENT, (long)BASSMIDIEvent.MIDI_EVENT_NOTE, Globals._mySync, IntPtr.Zero);
                            Globals.notecount = 0;
                            while (Bass.BASS_ChannelIsActive(Globals._recHandle) == BASSActive.BASS_ACTIVE_PLAYING)
                            {
                                if (Globals.CancellationPendingValue != 1)
                                {
                                    notes = BASSPlayBackEngine(notes, pos);
                                }
                                else if (Globals.CancellationPendingValue == 1)
                                {
                                    BASSCloseStream(res_man.GetString("PlaybackAborted", cul), res_man.GetString("PlaybackAborted", cul), 0);
                                    KeepLooping = false;
                                    Globals.PlaybackMode = false;
                                    break;
                                }
                            }
                            if (Globals.CancellationPendingValue == 1)
                            {
                                KeepLooping = false;
                                if (KeepLooping == false)
                                {
                                    break;
                                }
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (Globals.CancellationPendingValue == 1)
                        {
                            BASSCloseStream(res_man.GetString("PlaybackAborted", cul), res_man.GetString("PlaybackAborted", cul), 1);
                            KeepLooping = false;
                            Globals.PlaybackMode = false;
                            Globals.SFZBankPreset = new int[0];
                            PlayConversionStop();
                        }
                        else
                        {
                            BassEnc.BASS_Encode_Stop(Globals._Encoder);
                            Bass.BASS_StreamFree(Globals._recHandle);
                            Bass.BASS_Free();
                            Globals.CancellationPendingValue = 0;
                            Globals.ActiveVoicesInt = 0;
                            Globals.CurrentStatusTextString = null;
                            Globals.NewWindowName = null;
                            KeepLooping = false;
                            Globals.PlaybackMode = false;
                            Globals.SFZBankPreset = new int[0];
                            PlayConversionStop();
                        }
                    }
                }
                catch (Exception exception)
                {
                    BASSCloseStreamCrash(exception);
                    Globals.PlaybackMode = false;
                    Globals.SFZBankPreset = new int[0];
                }
            }
            catch (Exception exception2)
            {
                BASSCloseStreamCrash(exception2);
                Globals.PlaybackMode = false;
                Globals.SFZBankPreset = new int[0];
            }
        }

        private void NoteSyncProc(int handle, int channel, int data, IntPtr user)
        {
            if ((data & 0xff00) != 0)
                Globals.notecount++; // Note count for playback mode
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing && (this.components != null))
            {
                this.components.Dispose();
            }
            base.Dispose(disposing);
        }

        public bool AeroEnabled()
        {
            if (Environment.OSVersion.Version.Major < 6)
                return false;
            bool Enabled = false;
            DwmIsCompositionEnabled(out Enabled);
            return Enabled;
        }

        void fadeIn(object sender, EventArgs e)
        {
            if (Opacity >= 1)
                t1.Stop(); 
            else
                if (AeroEnabled() == true) {
                    Opacity += 0.025;
                }
                else
                {
                    Opacity += 0.025;
                }      
        }

        void fadeOut(object sender, EventArgs e)
        {
            if (Opacity == 0)   
            {
                t2.Stop();
                if (Globals.DeleteEncoder == true)
                {
                    File.Delete(Globals.EncoderPath);
                }
                Process.GetCurrentProcess().Kill();  
            }
            else
                Opacity -= 0.1;
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (Bass.BASS_ChannelIsActive(Globals._recHandle) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                DialogResult dialogResult = MessageBox.Show(res_man.GetString("AppBusy", cul), "Hey!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dialogResult == DialogResult.Yes)
                {
                    Bass.BASS_StreamFree(Globals._recHandle);
                    Bass.BASS_Free();
                    t2.Interval = 1;
                    t2.Tick += new EventHandler(fadeOut);
                    t2.Start();
                }
            }
            else
            {
                t2.Interval = 1;
                t2.Tick += new EventHandler(fadeOut);
                t2.Start();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);

            if (e.CloseReason == CloseReason.WindowsShutDown) Process.GetCurrentProcess().Kill();

            // Confirm user wants to close
            if (Bass.BASS_ChannelIsActive(Globals._recHandle) == BASSActive.BASS_ACTIVE_PLAYING)
            {
                DialogResult dialogResult = MessageBox.Show(res_man.GetString("AppBusy", cul), "Hey!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
                if (dialogResult == DialogResult.Yes)
                {
                    Bass.BASS_StreamFree(Globals._recHandle);
                    Bass.BASS_Free();
                    e.Cancel = true;
                    t2.Interval = 1;
                    t2.Tick += new EventHandler(fadeOut);
                    t2.Start();  
                }
                else if (dialogResult == DialogResult.No)
                {
                    e.Cancel = true;
                }
            }
            else
            {
                e.Cancel = true;
                t2.Interval = 1;
                t2.Tick += new EventHandler(fadeOut);
                t2.Start();  
            }
        }

        private string GetSizeMIDI(string str)
        {
            try
            {
                long length = new System.IO.FileInfo(str).Length;

                if (length / 1024f >= 1000000)
                    return (((length / 1024f) / 1024f) / 1024f).ToString("0 GB");
                else if (length / 1024f >= 1000)
                    return ((length / 1024f) / 1024f).ToString("0 MB");
                else
                    return (length / 1024f).ToString("0 KB");
            }
            catch { return "-"; }
        }

        private string[] GetMoreInfoMIDI(string str)
        {
            try
            {
                string[] strings;
                long length = new System.IO.FileInfo(str).Length;
                if (length / 1024f >= 9860)
                {
                    if (ModifierKeys == Keys.Control)
                    {

                    }
                    else
                    {
                        strings = new string[] { "N/A", "N/A" };
                        return strings;
                    }
                }
                Bass.BASS_Init(0, 22050, BASSInit.BASS_DEVICE_NOSPEAKER, IntPtr.Zero);
                int time = BassMidi.BASS_MIDI_StreamCreateFile(str, 0L, 0L, BASSFlag.BASS_STREAM_DECODE, 0);
                long pos = Bass.BASS_ChannelGetLength(time);
                double num9 = Bass.BASS_ChannelBytes2Seconds(time, pos);
                TimeSpan span = TimeSpan.FromSeconds(num9);
                string str4 = span.Minutes.ToString() + ":" + span.Seconds.ToString().PadLeft(2, '0');
                int count = BassMidi.BASS_MIDI_StreamGetEvents(time, -1, BASSMIDIEvent.MIDI_EVENT_NOTE, null);
                BASS_MIDI_EVENT[] events = new BASS_MIDI_EVENT[count];
                BassMidi.BASS_MIDI_StreamGetEvents(time, -1, BASSMIDIEvent.MIDI_EVENT_NOTE, events);

                int notes = 0;
                for (int a = 0; a < count; a++) { if ((events[a].param & 0xff00) != 0) { notes++; } }
                    
                Bass.BASS_Free();
                strings = new string[] { str4, notes.ToString("N0") };
                return strings;
            }
            catch (Exception ex) {
                string[] strings = new string[] { "N/A", "N/A" };
                return strings;
            }     
        }

        private void importMIDIsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MIDIImport.Title = res_man.GetString("ImportMIDIWindow", cul);
            MIDIImport.InitialDirectory = Globals.MIDILastDirectory;
            if (this.MIDIImport.ShowDialog() == DialogResult.OK)
            {
                foreach (string str in this.MIDIImport.FileNames)
                {
                    if (Path.GetExtension(str).ToLower() == ".mid" | Path.GetExtension(str).ToLower() == ".midi" | Path.GetExtension(str).ToLower() == ".kar" | Path.GetExtension(str).ToLower() == ".rmi")
                    {
                        string[] saLvwItem = new string[4];
                        string[] midiinfo = GetMoreInfoMIDI(str);
                        saLvwItem[0] = str;
                        saLvwItem[1] = midiinfo[1];
                        saLvwItem[2] = midiinfo[0];
                        saLvwItem[3] = GetSizeMIDI(str);
                        ListViewItem lvi = new ListViewItem(saLvwItem);
                        MIDIList.Items.Add(lvi); 
                    }
                    else
                    {
                        MessageBox.Show(String.Format(res_man.GetString("InvalidMIDIFile", cul), Path.GetFileName(str)), res_man.GetString("Error", cul), MessageBoxButtons.OK, MessageBoxIcon.Error);
                    } 
                }
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Globals.MIDILastDirectory = Path.GetDirectoryName(MIDIImport.FileName);
                Settings.SetValue("lastmidifolder", Globals.MIDILastDirectory);
                MIDIImport.InitialDirectory = Globals.MIDILastDirectory;
            }  
        }

        private void informationsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            new Informations().ShowDialog();
        }

        // Links

        private void kaleidonKep99sYouTubeChannelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://www.youtube.com/channel/UCJeqODojIv4TdeHcBfHJRnA");
        }

        private void KaleidonKep99sGitHubPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start("https://github.com/KaleidonKep99/");
        }

        // No more links

        private void removeMIDIToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = this.MIDIList.SelectedIndices.Count - 1; i >= 0; i--)
            {
                this.MIDIList.Items.RemoveAt(this.MIDIList.SelectedIndices[i]);
            }
        }

        private void removeSelectedMIDIsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            for (int i = this.MIDIList.SelectedIndices.Count - 1; i >= 0; i--)
            {
                this.MIDIList.Items.RemoveAt(this.MIDIList.SelectedIndices[i]);
            }
        }

        private void MoveUpItem_Click(object sender, EventArgs e)
        {
            MoveListViewItems(MIDIList, MoveDirection.Up);
        }

        private void MoveDownItem_Click(object sender, EventArgs e)
        {
            MoveListViewItems(MIDIList, MoveDirection.Down);
        }

        private void SortByName_Click(object sender, EventArgs e)
        {
            MIDIList.Sorting = SortOrder.Ascending;
            MIDIList.Sorting = SortOrder.None;
        }

        private static void MoveListViewItems(ListView sender, MoveDirection direction)
        {
            int dir = (int)direction;
            int opp = dir * -1;

            bool valid = sender.SelectedItems.Count > 0 &&
                            ((direction == MoveDirection.Down && (sender.SelectedItems[sender.SelectedItems.Count - 1].Index < sender.Items.Count - 1))
                        || (direction == MoveDirection.Up && (sender.SelectedItems[0].Index > 0)));

            if (valid)
            {
                foreach (ListViewItem item in sender.SelectedItems)
                {
                    string str = item.Text;
                    int index = item.Index + dir;

                    sender.Items.RemoveAt(item.Index);
                    sender.Items.Insert(index, item);
                }
            }
        }

        private void MIDIList_DragDrop(object sender, System.Windows.Forms.DragEventArgs e)
        {
            string[] s = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            int i;
            for (i = 0; i < s.Length; i++)
            {
                if (Path.GetExtension(s[i]).ToLower() == ".mid" | Path.GetExtension(s[i]).ToLower() == ".midi" | Path.GetExtension(s[i]).ToLower() == ".kar" | Path.GetExtension(s[i]).ToLower() == ".rmi")
                {
                    string[] saLvwItem = new string[4];
                    string[] midiinfo = GetMoreInfoMIDI(s[i]);
                    saLvwItem[0] = s[i];
                    saLvwItem[1] = midiinfo[1];
                    saLvwItem[2] = midiinfo[0];
                    ListViewItem lvi = new ListViewItem(saLvwItem);
                    MIDIList.Items.Add(lvi); 
                }
                else
                {
                    MessageBox.Show(String.Format(res_man.GetString("InvalidMIDIFile", cul), Path.GetFileName(s[i])), res_man.GetString("Error", cul), MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void MIDIList_DragEnter(object sender, System.Windows.Forms.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.All;
            else
                e.Effect = DragDropEffects.None;
        }

        private void MIDIList_KeyPress(object sender, KeyEventArgs e)
        {
            try
            {
                if (Globals.RenderingMode == true | Globals.PlaybackMode == true)
                {

                }
                else
                {
                    if (e.KeyValue == (char)Keys.Delete)
                    {
                        for (int i = this.MIDIList.SelectedIndices.Count - 1; i >= 0; i--)
                        {
                            this.MIDIList.Items.RemoveAt(this.MIDIList.SelectedIndices[i]);
                        }
                    }
                    else if (e.KeyCode == Keys.A && e.Control)
                    {
                        foreach (ListViewItem item in MIDIList.Items)
                        {
                            item.Selected = true;
                        }
                    }
                }
            }
            catch
            {

            }
        }

        private bool Resizing = false;

        private void MIDIList_SizeChanged(object sender, EventArgs e)
        {
            if (!Resizing)
            {
                Resizing = true;
                ListView listView = sender as ListView;
                if (listView != null)
                {
                    float totalColumnWidth = 0;

                    for (int i = 0; i < listView.Columns.Count; i++)
                        totalColumnWidth += Convert.ToInt32(listView.Columns[i].Tag);

                    for (int i = 0; i < listView.Columns.Count; i++)
                    {
                        float colPercentage = (Convert.ToInt32(listView.Columns[i].Tag) / totalColumnWidth);
                        listView.Columns[i].Width = (int)(colPercentage * listView.ClientRectangle.Width);
                    }
                }
            }
            Resizing = false;
        }

        public bool SelectItem(ListBox listBox, string item)
        {
            bool contains = listBox.Items.Contains(item);
            if (!contains)
                return false;
            listBox.SelectedItem = item;
            return listBox.SelectedItems.Contains(item);
        }

        private void RenderingTimer_Tick(object sender, EventArgs e)
        {
            System.Threading.Thread.Sleep(1);
            MIDIImport.InitialDirectory = Globals.MIDILastDirectory;
            ExportWhere.InitialDirectory = Globals.ExportLastDirectory;
            Graphics gr = CurrentStatus.CreateGraphics();
            Font font = new Font("Tahoma", 8, GraphicsUnit.Point);
            if (!Globals.AutoShutDownEnabled)
            {
                Globals.AutoShutDownEnabled = false;
                disabledToolStripMenuItem.Checked = true;
            }
            if (!Globals.AutoClearMIDIListEnabled)
            {
                Globals.AutoClearMIDIListEnabled = false;
                disabledToolStripMenuItem1.Checked = true;
            }
            try
            {
                if (Bass.BASS_ChannelIsActive(Globals._recHandle) == BASSActive.BASS_ACTIVE_STOPPED)
                {
                    this.Text = "Keppy's MIDI Converter";
                    if (Environment.OSVersion.Version.Major == 5)
                    {

                    }
                    else
                    {
                        TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.NoProgress);
                    }
                    if (Globals.PlaybackMode == true)
                    {
                        this.CurrentStatus.Style = ProgressBarStyle.Marquee;
                        if (Globals.VSTMode == true)
                        {
                            this.CurrentStatusText.Text = res_man.GetString("MemoryAllocationPlayback", cul);
                        }
                        else
                        {
                            this.CurrentStatusText.Text = res_man.GetString("MemoryAllocationPlaybackVST", cul);
                        }
                        this.UsedVoices.Text = res_man.GetString("ActiveVoices", cul) + Globals.ActiveVoicesInt.ToString() + @"/" + Globals.LimitVoicesInt.ToString();
                        this.CurrentStatus.MarqueeAnimationSpeed = 100;
                        this.MIDIList.Enabled = false;
                        if (Globals.pictureset != 0)
                        {
                            this.loadingpic.Image = KeppyMIDIConverter.Properties.Resources.convbusy;
                            Globals.pictureset = 0;
                        }
                        this.importMIDIsToolStripMenuItem.Enabled = false;
                        this.removeSelectedMIDIsToolStripMenuItem.Enabled = false;
                        this.clearMIDIsListToolStripMenuItem.Enabled = false;
                        this.startRenderingWAVToolStripMenuItem.Enabled = false;
                        this.startRenderingOGGToolStripMenuItem.Enabled = false;
                        this.openTheSoundfontsManagerToolStripMenuItem.Enabled = false;
                        this.playInRealtimeBetaToolStripMenuItem.Enabled = false;
                        this.abortRenderingToolStripMenuItem.Enabled = true;
                        this.labelRMS.Text = String.Format("{0}: {1:#0.0} dB | {2}: {3:#0.0} dB | {4}: {5:#0.0} dB", res_man.GetString("RMS", cul), Globals._plm.RMS_dBV, res_man.GetString("AverageLevel", cul), Globals._plm.AVG_dBV, res_man.GetString("PeakLevel", cul), Math.Max(Globals._plm.PeakHoldLevelL_dBV, Globals._plm.PeakHoldLevelR_dBV));
                        this.SettingsBox.Enabled = false;
                        this.label3.Enabled = true;
                        this.VolumeBar.Enabled = true;
                        this.VoiceLimit.Maximum = 2000;
                        Process thisProc = Process.GetCurrentProcess();
                        thisProc.PriorityClass = ProcessPriorityClass.High;
                    }
                    else if (Globals.RenderingMode == true)
                    {
                        this.CurrentStatus.Style = ProgressBarStyle.Marquee;
                        this.CurrentStatusText.Text = res_man.GetString("MemoryAllocationConversion", cul);
                        this.UsedVoices.Text = res_man.GetString("ActiveVoices", cul) + Globals.ActiveVoicesInt.ToString() + @"/" + Globals.LimitVoicesInt.ToString();
                        this.CurrentStatus.MarqueeAnimationSpeed = 100;
                        this.MIDIList.Enabled = false;
                        if (Globals.pictureset != 0)
                        {
                            this.loadingpic.Image = KeppyMIDIConverter.Properties.Resources.convbusy;
                            Globals.pictureset = 0;
                        }
                        this.importMIDIsToolStripMenuItem.Enabled = false;
                        this.removeSelectedMIDIsToolStripMenuItem.Enabled = false;
                        this.clearMIDIsListToolStripMenuItem.Enabled = false;
                        this.startRenderingWAVToolStripMenuItem.Enabled = false;
                        this.startRenderingOGGToolStripMenuItem.Enabled = false;
                        this.openTheSoundfontsManagerToolStripMenuItem.Enabled = false;
                        this.playInRealtimeBetaToolStripMenuItem.Enabled = false;
                        this.abortRenderingToolStripMenuItem.Enabled = true;
                        this.labelRMS.Text = String.Format("{0}: {1:#0.0} dB | {2}: {3:#0.0} dB | {4}: {5:#0.0} dB", res_man.GetString("RMS", cul), Globals._plm.RMS_dBV, res_man.GetString("AverageLevel", cul), Globals._plm.AVG_dBV, res_man.GetString("PeakLevel", cul), Math.Max(Globals._plm.PeakHoldLevelL_dBV, Globals._plm.PeakHoldLevelR_dBV));
                        this.SettingsBox.Enabled = false;
                        this.label3.Enabled = false;
                        this.VolumeBar.Enabled = false;
                        this.VoiceLimit.Maximum = 100000;
                        Process thisProc = Process.GetCurrentProcess();
                        thisProc.PriorityClass = ProcessPriorityClass.High;
                    }
                    else if (Globals.RenderingMode == false & Globals.PlaybackMode == false)
                    {
                        this.CurrentStatus.Style = ProgressBarStyle.Blocks;
                        this.CurrentStatus.Maximum = 999;
                        this.CurrentStatus.Value = 0;
                        this.CurrentStatusText.Text = res_man.GetString("IdleMessage", cul);
                        this.UsedVoices.Text = res_man.GetString("ActiveVoices", cul) + @"0/" + Globals.LimitVoicesInt.ToString();
                        this.MIDIList.Enabled = true;
                        if (Globals.pictureset != 1)
                        {
                            this.loadingpic.Image = KeppyMIDIConverter.Properties.Resources.convpause;
                            Globals.pictureset = 1;
                        }
                        this.importMIDIsToolStripMenuItem.Enabled = true;
                        if (MIDIList.Items.Count < 1)
                        {
                            removeSelectedMIDIsToolStripMenuItem.Enabled = false;
                            clearMIDIsListToolStripMenuItem.Enabled = false;
                            startRenderingWAVToolStripMenuItem.Enabled = false;
                            startRenderingOGGToolStripMenuItem.Enabled = false;
                            playInRealtimeBetaToolStripMenuItem.Enabled = false;
                        }
                        else
                        {
                            if (Globals.Soundfonts.Length == 0)
                            {
                                importMIDIsToolStripMenuItem.DefaultItem = false;
                                openTheSoundfontsManagerToolStripMenuItem.DefaultItem = true;
                                removeSelectedMIDIsToolStripMenuItem.Enabled = true;
                                clearMIDIsListToolStripMenuItem.Enabled = true;
                                startRenderingWAVToolStripMenuItem.Enabled = false;
                                startRenderingOGGToolStripMenuItem.Enabled = false;
                                playInRealtimeBetaToolStripMenuItem.Enabled = false;
                            }
                            else
                            {
                                openTheSoundfontsManagerToolStripMenuItem.DefaultItem = false;
                                importMIDIsToolStripMenuItem.DefaultItem = true;
                                removeSelectedMIDIsToolStripMenuItem.Enabled = true;
                                clearMIDIsListToolStripMenuItem.Enabled = true;
                                startRenderingWAVToolStripMenuItem.Enabled = true;
                                startRenderingOGGToolStripMenuItem.Enabled = true;
                                playInRealtimeBetaToolStripMenuItem.Enabled = true;
                            }
                        }
                        this.openTheSoundfontsManagerToolStripMenuItem.Enabled = true;
                        this.abortRenderingToolStripMenuItem.Enabled = false;
                        this.labelRMS.Text = String.Format("{0}: {1:#0.0} dB | {2}: {3:#0.0} dB | {4}: {5:#0.0} dB", res_man.GetString("RMS", cul), 0, res_man.GetString("AverageLevel", cul), 0, res_man.GetString("PeakLevel", cul), 0);
                        this.SettingsBox.Enabled = true;
                        this.label3.Enabled = true;
                        this.VolumeBar.Enabled = true;
                        this.VoiceLimit.Maximum = 100000;
                        Process thisProc = Process.GetCurrentProcess();
                        thisProc.PriorityClass = ProcessPriorityClass.Idle;
                    }

                }
                else if (Bass.BASS_ChannelIsActive(Globals._recHandle) == BASSActive.BASS_ACTIVE_PLAYING)
                {
                    if (Globals.CurrentStatusTextString == null)
                    {
                        if (Environment.OSVersion.Version.Major == 5)
                        {

                        }
                        else
                        {
                            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Indeterminate);
                        }
                        if (Globals.PlaybackMode == true)
                        {
                            this.SettingsBox.Enabled = true;
                            this.SettingsBox.Cursor = Cursors.Default;
                            this.label3.Enabled = true;
                            this.VolumeBar.Enabled = true;
                            this.VoiceLimit.Maximum = 2000;
                        }
                        else
                        {
                            this.SettingsBox.Enabled = false;
                            this.label3.Enabled = false;
                            this.VolumeBar.Enabled = false;
                            this.VoiceLimit.Maximum = 100000;
                        }
                        this.CurrentStatus.Style = ProgressBarStyle.Marquee;
                        if (Globals.VSTMode == true)
                        {
                            this.CurrentStatusText.Text = res_man.GetString("BASSEngineConfigurationVST", cul);
                        }
                        else
                        {
                            this.CurrentStatusText.Text = res_man.GetString("BASSEngineConfiguration", cul);
                        }
                        this.UsedVoices.Text = res_man.GetString("ActiveVoices", cul) + Globals.ActiveVoicesInt.ToString() + @"/" + Globals.LimitVoicesInt.ToString();
                        this.CurrentStatus.MarqueeAnimationSpeed = 100;
                        this.MIDIList.Enabled = false;
                        if (Globals.pictureset != 0)
                        {
                            this.loadingpic.Image = KeppyMIDIConverter.Properties.Resources.convbusy;
                            Globals.pictureset = 0;
                        }
                        this.importMIDIsToolStripMenuItem.Enabled = false;
                        this.removeSelectedMIDIsToolStripMenuItem.Enabled = false;
                        this.clearMIDIsListToolStripMenuItem.Enabled = false;
                        this.startRenderingWAVToolStripMenuItem.Enabled = false;
                        this.startRenderingOGGToolStripMenuItem.Enabled = false;
                        this.openTheSoundfontsManagerToolStripMenuItem.Enabled = false;
                        this.playInRealtimeBetaToolStripMenuItem.Enabled = false;
                        this.abortRenderingToolStripMenuItem.Enabled = true;
                        this.labelRMS.Text = String.Format("{0}: {1:#0.0} dB | {2}: {3:#0.0} dB | {4}: {5:#0.0} dB", res_man.GetString("RMS", cul), Globals._plm.RMS_dBV, res_man.GetString("AverageLevel", cul), Globals._plm.AVG_dBV, res_man.GetString("PeakLevel", cul), Math.Max(Globals._plm.PeakHoldLevelL_dBV, Globals._plm.PeakHoldLevelR_dBV));
                        Process thisProc = Process.GetCurrentProcess();
                        thisProc.PriorityClass = ProcessPriorityClass.High;
                    }
                    else
                    {
                        if (Environment.OSVersion.Version.Major == 5)
                        {

                        }
                        else
                        {
                            TaskbarManager.Instance.SetProgressState(TaskbarProgressBarState.Normal);
                            TaskbarManager.Instance.SetProgressValue(Globals.CurrentStatusValueInt, Globals.CurrentStatusMaximumInt);
                        }
                        if (Globals.PlaybackMode == true)
                        {
                            this.SettingsBox.Enabled = true;
                            this.VolumeBar.Enabled = true;
                            this.VoiceLimit.Maximum = 2000;
                            Process thisProc = Process.GetCurrentProcess();
                            thisProc.PriorityClass = ProcessPriorityClass.RealTime;
                        }
                        else
                        {
                            this.SettingsBox.Enabled = false;
                            this.VolumeBar.Enabled = false;
                            this.VoiceLimit.Maximum = 100000;
                            Process thisProc = Process.GetCurrentProcess();
                            thisProc.PriorityClass = ProcessPriorityClass.High;
                        }
                        this.MIDIList.Enabled = false;
                        this.CurrentStatusText.Text = Globals.CurrentStatusTextString;
                        this.UsedVoices.Text = res_man.GetString("ActiveVoices", cul) + Globals.ActiveVoicesInt.ToString() + @"/" + Globals.LimitVoicesInt.ToString();
                        this.CurrentStatus.Style = ProgressBarStyle.Blocks;
                        CurrentStatus.Refresh();
                        gr.DrawString(Globals.PercentageProgress, font, Brushes.Black, new PointF(CurrentStatus.Width / 2 - (gr.MeasureString(Globals.PercentageProgress, font).Width / 2.0F), CurrentStatus.Height / 2 - (gr.MeasureString(Globals.PercentageProgress, font).Height / 2.0F)));
                        if (Globals.pictureset != 0)
                        {
                            this.loadingpic.Image = KeppyMIDIConverter.Properties.Resources.convbusy;
                            Globals.pictureset = 0;
                        }
                        this.CurrentStatus.Value = Globals.CurrentStatusValueInt;
                        this.CurrentStatus.Maximum = Globals.CurrentStatusMaximumInt;
                        this.importMIDIsToolStripMenuItem.Enabled = false;
                        this.removeSelectedMIDIsToolStripMenuItem.Enabled = false;
                        this.clearMIDIsListToolStripMenuItem.Enabled = false;
                        this.startRenderingWAVToolStripMenuItem.Enabled = false;
                        this.startRenderingOGGToolStripMenuItem.Enabled = false;
                        this.openTheSoundfontsManagerToolStripMenuItem.Enabled = false;
                        this.playInRealtimeBetaToolStripMenuItem.Enabled = false;
                        this.abortRenderingToolStripMenuItem.Enabled = true;
                        this.labelRMS.Text = String.Format("{0}: {1:#00.0} dB | {2}: {3:#00.0} dB | {4}: {5:#00.0} dB", res_man.GetString("RMS", cul), Globals._plm.RMS_dBV, res_man.GetString("AverageLevel", cul), Globals._plm.AVG_dBV, res_man.GetString("PeakLevel", cul), Math.Max(Globals._plm.PeakHoldLevelL_dBV, Globals._plm.PeakHoldLevelR_dBV));
                    }
                    if (Globals.NewWindowName == null)
                    {
                        this.Text = "Keppy's MIDI Converter";
                    }
                    else
                    {
                        if (Globals.PlaybackMode == true)
                            this.Text = String.Format(res_man.GetString("PlaybackText", cul), Globals.NewWindowName);
                        else
                            this.Text = String.Format(res_man.GetString("ConversionText", cul), Globals.NewWindowName);
                    }
                }
                gr.Dispose();
                font.Dispose();
            }
            catch
            {

            }
        }

        private void playInRealtimeBetaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.loadingpic.Visible = true;
            Globals.PlaybackMode = true;
            this.RealTimePlayBack.RunWorkerAsync();
        }

        private void VolumeBar_Scroll(object sender, EventArgs e)
        {
            try
            {
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Settings.SetValue("volume", VolumeBar.Value.ToString(), Microsoft.Win32.RegistryValueKind.DWord);
                Globals.Volume = Convert.ToInt32(this.VolumeBar.Value);
                Settings.Close();
                VolumeTip.SetToolTip(VolumeBar, Convert.ToString(Convert.ToInt32(Globals.Volume / 100)) + "%");
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("Error", cul), exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void VoiceLimit_ValueChanged(object sender, EventArgs e)
        {
            try
            {
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Settings.SetValue("voices", VoiceLimit.Value.ToString(), Microsoft.Win32.RegistryValueKind.DWord);
                Globals.LimitVoicesInt = Convert.ToInt32(VoiceLimit.Value.ToString());
                Settings.Close();
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("Error", cul), exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void AdvSettingsButton_Click(object sender, EventArgs e)
        {
            Globals.frm.ShowDialog();
        }

        private void openTheSoundfontsManagerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Globals.frm2.ShowDialog();
        }

        private void DefMenu_Popup(object sender, EventArgs e)
        {
            if (MIDIList.Items.Count < 1)
            {
                RemoveMIDIsRightClick.Enabled = false;
                ClearMIDIsListRightClick.Enabled = false;
                SortByName.Enabled = false;
                MoveUpItem.Enabled = false;
                MoveDownItem.Enabled = false;
            }
            else if (MIDIList.Items.Count < 2)
            {
                RemoveMIDIsRightClick.Enabled = true;
                ClearMIDIsListRightClick.Enabled = true;
                SortByName.Enabled = false;
                MoveUpItem.Enabled = false;
                MoveDownItem.Enabled = false;
            }
            else
            {
                RemoveMIDIsRightClick.Enabled = true;
                ClearMIDIsListRightClick.Enabled = true;
                SortByName.Enabled = true;
                MoveUpItem.Enabled = true;
                MoveDownItem.Enabled = true;
            }
        }

        private void enabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enabledToolStripMenuItem.Checked = true;
            disabledToolStripMenuItem.Checked = false;
            Globals.AutoShutDownEnabled = true;
        }

        private void disabledToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enabledToolStripMenuItem.Checked = false;
            disabledToolStripMenuItem.Checked = true;
            Globals.AutoShutDownEnabled = false;
        }

        private void enabledToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            enabledToolStripMenuItem1.Checked = true;
            disabledToolStripMenuItem1.Checked = false;
            Globals.AutoClearMIDIListEnabled = true;
        }

        private void disabledToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            enabledToolStripMenuItem1.Checked = false;
            disabledToolStripMenuItem1.Checked = true;
            Globals.AutoClearMIDIListEnabled = false;
        }

        private void enabledToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
            Settings.SetValue("audioevents", "1", RegistryValueKind.DWord);
            enabledToolStripMenuItem4.Checked = true;
            disabledToolStripMenuItem4.Checked = false;
            Settings.Close();
        }

        private void disabledToolStripMenuItem4_Click(object sender, EventArgs e)
        {
            RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
            Settings.SetValue("audioevents", "0", RegistryValueKind.DWord);
            enabledToolStripMenuItem4.Checked = false;
            disabledToolStripMenuItem4.Checked = true;
            Settings.Close();
        }

        private void enabledToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
            Settings.SetValue("autoupdatecheck", "1", RegistryValueKind.DWord);
            enabledToolStripMenuItem3.Checked = true;
            disabledToolStripMenuItem2.Checked = false;
            Settings.Close();
        }

        private void disabledToolStripMenuItem3_Click(object sender, EventArgs e)
        {
            RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
            Settings.SetValue("autoupdatecheck", "0", RegistryValueKind.DWord);
            enabledToolStripMenuItem3.Checked = false;
            disabledToolStripMenuItem2.Checked = true;
            Settings.Close();
        }


        private void enabledToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            try
            {
                enabledToolStripMenuItem5.Checked = true;
                disabledToolStripMenuItem5.Checked = false;
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Language = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", true);
                Language.SetValue("langoverride", "1", Microsoft.Win32.RegistryValueKind.DWord);
                Language.Close();
                InitializeLanguage();
                // List
                ChineseCNOverride.Enabled = true;
                ChineseHKOverride.Enabled = true;
                ChineseTWOverride.Enabled = true;
                DutchOverride.Enabled = false;
                EnglishOverride.Enabled = true;
                EstonianOverride.Enabled = true;
                FrenchOverride.Enabled = false;
                GermanOverride.Enabled = true;
                RussianOverride.Enabled = true;
                IndonesianOverride.Enabled = false;
                ItalianOverride.Enabled = true;
                JapaneseOverride.Enabled = true;
                KoreanOverride.Enabled = true;
                SpanishOverride.Enabled = true;
                TurkishOverride.Enabled = false;
                BengaliOverride.Enabled = true;
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler("Error", exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void disabledToolStripMenuItem5_Click(object sender, EventArgs e)
        {
            try
            {
                enabledToolStripMenuItem5.Checked = false;
                disabledToolStripMenuItem5.Checked = true;
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Language = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", true);
                Language.SetValue("langoverride", "0", Microsoft.Win32.RegistryValueKind.DWord);
                Language.Close();
                InitializeLanguage();
                // List
                ChineseCNOverride.Enabled = false;
                ChineseHKOverride.Enabled = false;
                ChineseTWOverride.Enabled = false;
                DutchOverride.Enabled = false;
                EnglishOverride.Enabled = false;
                EstonianOverride.Enabled = false;
                FrenchOverride.Enabled = false;
                GermanOverride.Enabled = false;
                RussianOverride.Enabled = false;
                IndonesianOverride.Enabled = false;
                ItalianOverride.Enabled = false;
                JapaneseOverride.Enabled = false;
                KoreanOverride.Enabled = false;
                SpanishOverride.Enabled = false;
                TurkishOverride.Enabled = false;
                BengaliOverride.Enabled = false;
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler("Error", exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void enabledToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                enabledToolStripMenuItem2.Checked = true;
                disabledToolStripMenuItem2.Checked = false;
                Globals.OldTimeThingy = true;
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Settings.SetValue("oldtimethingy", "1", Microsoft.Win32.RegistryValueKind.DWord);
                Settings.Close();
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler("Error", exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void disabledToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            try
            {
                enabledToolStripMenuItem2.Checked = false;
                disabledToolStripMenuItem2.Checked = true;
                Globals.OldTimeThingy = false;
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
                Settings.SetValue("oldtimethingy", "0", Microsoft.Win32.RegistryValueKind.DWord);
                Settings.Close();
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler("Error", exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void makeADonationToSupportTheDeveloperToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string url = "";

            string business = "prapapappo1999@gmail.com";
            string description = "Donation";
            string country = "US";
            string currency = "USD";

            url += "https://www.paypal.com/cgi-bin/webscr" +
                "?cmd=" + "_donations" +
                "&business=" + business +
                "&lc=" + country +
                "&item_name=" + description +
                "&currency_code=" + currency +
                "&bn=" + "PP%2dDonationsBF";

            Process.Start(url);
        }

        private void forceCloseTheApplicationToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult dialogResult = MessageBox.Show(res_man.GetString("CrashQuestion", cul), "Hey!", MessageBoxButtons.YesNo, MessageBoxIcon.Exclamation);
            if (dialogResult == DialogResult.Yes)
            {
                this.Enabled = false;
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler(res_man.GetString("FatalError", cul), res_man.GetString("CrashTriggeredByUser", cul), 1, 0);
                errordialog.ShowDialog();
                Application.Exit();
            }
        }

        private void PlayConversionStart()
        {
            RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
            if (Convert.ToInt32(Settings.GetValue("audioevents", 1)) == 1)
            {
                System.IO.Stream str = KeppyMIDIConverter.Properties.Resources.convstart;
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(str);
                player.Play();
            }
            Settings.Close();
        }

        private void PlayConversionStop()
        {
            RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Settings", true);
            if (Convert.ToInt32(Settings.GetValue("audioevents", 1)) == 1)
            {
                System.IO.Stream str = KeppyMIDIConverter.Properties.Resources.convfin;
                System.Media.SoundPlayer player = new System.Media.SoundPlayer(str);
                player.Play();
            }
            Settings.Close();
        }

        // Language overrides

        private void ChangeLanguage(string selectedlanguage)
        {
            try
            {
                Registry.CurrentUser.CreateSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", Microsoft.Win32.RegistryKeyPermissionCheck.ReadWriteSubTree);
                RegistryKey Settings = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Keppy's MIDI Converter\\Languages", true);
                Settings.SetValue("selectedlanguage", selectedlanguage, Microsoft.Win32.RegistryValueKind.String);
                Settings.Close();
                AdvancedSettings example = new AdvancedSettings();
                InitializeLanguage();
            }
            catch (Exception exception)
            {
                KeppyMIDIConverter.ErrorHandler errordialog = new KeppyMIDIConverter.ErrorHandler("Error", exception.ToString(), 0, 0);
                errordialog.ShowDialog();
            }
        }

        private void ItalianOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("it");
        }

        private void TurkishOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("tr");
        }

        private void EnglishOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("en");
        }

        private void SpanishOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("es");
        }

        private void GermanOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("de");
        }

        private void EstonianOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("et");
        }

        private void DutchOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("nl");
        }

        private void IndonesianOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("id");
        }

        private void FrenchOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("fr");
        }

        private void ChineseCN_Click(object sender, EventArgs e)
        {
            ChangeLanguage("zh-CN");
        }

        private void ChineseTW_Click(object sender, EventArgs e)
        {
            ChangeLanguage("zh-TW");
        }

        private void ChineseHK_Click(object sender, EventArgs e)
        {
            ChangeLanguage("zh-HK");
        }

        private void JapaneseOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("ja");
        }

        private void KoreanOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("ko");
        }

        private void Bengali_Click(object sender, EventArgs e)
        {
            ChangeLanguage("bn");
        }

        private void RussianOverride_Click(object sender, EventArgs e)
        {
            ChangeLanguage("ru");
        }

        private void FL12Discount_Click(object sender, EventArgs e)
        {
            Process.Start("http://affiliate.image-line.com/BFHHCGAE552");
        }

        protected override CreateParams CreateParams
        {
            get
            {
                var parms = base.CreateParams;
                parms.Style &= ~0x02000000; 
                return parms;
            }
        }
    }

    public static class StringExt
    {
        public static string Truncate(this string value, int maxLength)
        {
            if (string.IsNullOrEmpty(value)) { return value; }

            return value.Substring(0, Math.Min(value.Length, maxLength));
        }
    }

    public static class InputExtensions
    {
        public static int LimitToRange(this int value, int inclusiveMinimum, int inclusiveMaximum)
        {
            if (value < inclusiveMinimum) { return inclusiveMinimum; }
            if (value > inclusiveMaximum) { return inclusiveMaximum; }
            return value;
        }
    }
}