using Microsoft.Win32;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Input;

namespace Docutain_SDK_Example_Windows_WPF_.NET_Framework
{
    /// <summary>
    /// Interaktionslogik für MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            EnableButtons(false);

            //set log level according to your needs
            Docutain.SDK.Windows.Logger.SetLogLevel(Docutain.SDK.Windows.Logger.Level.Debug);

            string License_Key = "<YOUR-LICENSE-KEY>";
            string TrailLicenseUrl = "https://sdk.docutain.com/TrialLicense?Source=66048";

            //initialize the Docutain SDK with your license key
            if (!Docutain.SDK.Windows.DocutainSDK.InitSDK(License_Key, System.IO.Path.GetTempPath()))
            {
                WriteState("SDK initialization failed");
                btLoadDocument.IsEnabled = false;

                if (License_Key == "<YOUR-LICENSE-KEY>")
                {
                    MessageBoxResult rc = MessageBox.Show("A valid trial license key is required. You can generate a trial license key on our website.", "Trial license needed", MessageBoxButton.YesNo);

                    if (rc == MessageBoxResult.Yes)
                        ShowBrowser(TrailLicenseUrl);

                    WriteState("No valid LICENSE-KEY");
                    tbOutput.Text = "License_Key = <YOUR-LICENSE-KEY> in MainWindows.xaml.cs must be replaced with your trial license key.\n\nYou can generate a trial license key on our website.\n"+ TrailLicenseUrl;
                }
                else
                {
                    MessageBox.Show(Docutain.SDK.Windows.DocutainSDK.GetLastError());
                }
            }
            else
            {
                WriteState("SDK initialized");
            }
        }

        public static void ShowBrowser(string url)
        {
            var ps = new ProcessStartInfo(url)
            {
                UseShellExecute = true,
                Verb = "open"
            };

            Process.Start(ps);
        }

        private void EnableButtons(bool Enable)
        {
            btCreatePDF.IsEnabled = Enable;
            btDocumentData.IsEnabled = Enable;
            btShowText.IsEnabled = Enable;
            btAddPage.IsEnabled = Enable;
        }

        private void WriteState(string Message)
        {
            lbState.Content = Message;
        }

        private async void BtLoadDocument_Click(object sender, RoutedEventArgs e)
        {
            //open file dialog to pick a file
            OpenFileDialog ofd = new OpenFileDialog();
            
            ofd.Filter = "Supported files (*.PDF;*.JPG; *.JPEG; *.PNG; *.TIF; *.TIFF; *.HEIC)|*.PDF; *.JPG; *.JPEG; *.PNG; *.TIF; *.TIFF; *.HEIC";
            ofd.Filter += "|PDF files (*.PDF)|*.PDF|JPG files (*.JPG; *.JPEG)|*.JPG; *.JPEG|PNG files (*.PNG)|*.PNG|TIF files (*.TIF; *.TIFF)|*.TIF; *.TIFF|HEIC files (*.HEIC)|*.HEIC";
            ofd.FilterIndex = 1;
            ofd.RestoreDirectory = false;

            if (ofd.ShowDialog() == true)
            {
                if (ofd.FileName.Length == 0)
                    return;

                Mouse.OverrideCursor = Cursors.Wait;

                tbOutput.Text = "";

                bool rc = false;

                if (rbLDFilepath.IsChecked.Value)
                {
                    //load a document from selected file path
                    if (rc = await Docutain.SDK.Windows.Document.Load(ofd.FileName))
                        WriteState("Document loaded (mode filepath)");
                }
                if (rbLDBinary.IsChecked.Value)
                {
                    //load a document from a byte array
                    byte[] data = File.ReadAllBytes(ofd.FileName);

                    if (rc = await Docutain.SDK.Windows.Document.Load(data, System.IO.Path.GetExtension(ofd.FileName)))
                        WriteState("Document loaded (mode binary)");
                }
                if (rbLDStream.IsChecked.Value)
                {
                    //load a document from a stream
                    byte[] data = File.ReadAllBytes(ofd.FileName);

                    using (Stream stream = new MemoryStream(data))
                    {
                        if (rc = await Docutain.SDK.Windows.Document.Load(stream, System.IO.Path.GetExtension(ofd.FileName)))
                            WriteState("Document loaded (mode stream)");
                    }
                }

                Mouse.OverrideCursor = null;

                if (!rc)
                {
                    EnableButtons(false);
                    WriteState("Loading document failed");
                    MessageBox.Show(Docutain.SDK.Windows.DocutainSDK.GetLastError());
                    return;
                }

                EnableButtons(true);
                lbPageCount.Content = Docutain.SDK.Windows.Document.PageCount().ToString();
            }
        }

        private void BtDocumentData_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            //analyze the currently loaded document and return detected data as JSON string
            string JSON = Docutain.SDK.Windows.Document.Analyze();
            if (!string.IsNullOrEmpty(JSON))
            {
                //deserialize JSON string into an object of type DocumentData
                DocumentData documentData = JsonConvert.DeserializeObject<DocumentData>(JSON);
                tbOutput.Text = JSON.Replace("\n", Environment.NewLine);
            }
            else
            {
                tbOutput.Text = "No data read";
            }

            WriteState("Document data loaded");
            Mouse.OverrideCursor = null;
        }

        private void BtShowText_Click(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = Cursors.Wait;

            //get the detected text of the currently loaded document
            //pass -1 for entire document, else page number
            tbOutput.Text = Docutain.SDK.Windows.Document.Text(-1);
            WriteState("Text loaded");

            Mouse.OverrideCursor = null;
        }

        private void BtCreatePDF_Click(object sender, RoutedEventArgs e)
        {
            //select path where to save the PDF
            SaveFileDialog sfd = new SaveFileDialog();
            
            sfd.Filter = "PDF files (*.PDF)|*.PDF";
            sfd.FilterIndex = 1;
            sfd.RestoreDirectory = true;

            if (sfd.ShowDialog() == true)
            {
                if (sfd.FileName.Length == 0)
                    return;

                Mouse.OverrideCursor = Cursors.Wait;

                //generate PDF from currently loaded document and save it to the selected path
                Docutain.SDK.Windows.Document.WritePDF(sfd.FileName, true, Docutain.SDK.Windows.Document.PDFPageFormat.FitToPages);
                WriteState("PDF created");

                Mouse.OverrideCursor = null;
            }
        }

        private async void BtAddPage_Click(object sender, RoutedEventArgs e)
        {
            //select the file you wish to add as page to the currently loaded document
            OpenFileDialog ofd = new OpenFileDialog();
            
            ofd.Filter = "Supported files (*.PDF;*.JPG; *.JPEG; *.PNG; *.TIF; *.TIFF; *.HEIC)|*.PDF; *.JPG; *.JPEG; *.PNG; *.TIF; *.TIFF; *.HEIC";
            ofd.Filter += "|PDF files (*.PDF)|*.PDF|JPG files (*.JPG; *.JPEG)|*.JPG; *.JPEG|PNG files (*.PNG)|*.PNG|TIF files (*.TIF; *.TIFF)|*.TIF; *.TIFF|HEIC files (*.HEIC)|*.HEIC";
            ofd.FilterIndex = 1;
            ofd.RestoreDirectory = false;

            if (ofd.ShowDialog() == true)
            {
                if (ofd.FileName.Length == 0)
                    return;

                Mouse.OverrideCursor = Cursors.Wait;

                tbOutput.Text = "";

                bool rc = false;

                if (rbAPFilepath.IsChecked.Value)
                {
                    //add a page to the currently loaded document by using path to the file
                    if (rc = await Docutain.SDK.Windows.Document.AddPage(ofd.FileName))
                        WriteState("Page added (mode filepath)");
                }
                if (rbAPBinary.IsChecked.Value)
                {
                    //add a page to the currently loaded document by using byte array
                    byte[] data = File.ReadAllBytes(ofd.FileName);

                    if (rc = await Docutain.SDK.Windows.Document.AddPage(data, System.IO.Path.GetExtension(ofd.FileName)))
                        WriteState("Page added (mode binary)");
                }
                if (rbAPStream.IsChecked.Value)
                {
                    //add a page to the currently loaded document by using a stream
                    using (Stream stream = File.Open(ofd.FileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        if (rc = await Docutain.SDK.Windows.Document.AddPage(stream, System.IO.Path.GetExtension(ofd.FileName)))
                            WriteState("Page added (mode stream)");
                    }
                }

                Mouse.OverrideCursor = null;

                if (!rc)
                {
                    WriteState("Adding the page failed");
                    MessageBox.Show(Docutain.SDK.Windows.DocutainSDK.GetLastError());
                    return;
                }

                lbPageCount.Content = Docutain.SDK.Windows.Document.PageCount().ToString();
            }
        }

        private void BtLogfile_Click(object sender, RoutedEventArgs e)
        {
            //open the Docutain SDK log file
            Process.Start(Docutain.SDK.Windows.Logger.Filename);
            WriteState("Logfile opened");
        }

        private void btSet_Click(object sender, RoutedEventArgs e)
        {
            Docutain.SDK.Windows.AnalyzeConfiguration analyzeConfiguration = new Docutain.SDK.Windows.AnalyzeConfiguration();
            analyzeConfiguration.ReadBIC = cbBIC.IsChecked.Value;
            analyzeConfiguration.ReadPaymentState = cbPaymentState.IsChecked.Value;
            Docutain.SDK.Windows.DocutainSDK.SetAnalyzeConfiguration(analyzeConfiguration);
        }

        private void BtClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class DocumentData
    {
        public Address Address { get; set; }
        public string Date { get; set; }
        public string Amount { get; set; }
        public string InvoiceId { get; set; }
        public string reference { get; set; }
    }

    public class Address
    {
        public string Name1 { get; set; }
        public string Name2 { get; set; }
        public string Name3 { get; set; }
        public string Zipcode { get; set; }
        public string City { get; set; }
        public string Street { get; set; }
        public string Phone { get; set; }
        public string CustomerId { get; set; }
        public string[] IBAN { get; set; }
    }
}
