using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;

namespace BulkPhotoRename
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();
            AllowDrop = true;
            DragEnter += Form1_DragEnter;
            DragDrop += Form1_DragDrop;
        }

        void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) e.Effect = DragDropEffects.Copy;
        }

        void Form1_DragDrop(object sender, DragEventArgs e)
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop);
            // 1st pass, check
            var newNames = new Dictionary<string, DateTime>();
            var filesMissingTimestamp = new List<string>();
            var filesHaveSameTimestamp = new Dictionary<string, string>();
            foreach (string file in files)
            {
                var timestamp = GetDateTakenFromImage(file);
                if (timestamp == DateTime.MinValue)
                {
                    filesMissingTimestamp.Add(file);
                }
                else if (newNames.Values.Any(d => d == timestamp))
                {
                    filesHaveSameTimestamp.Add(file, newNames.First(n => n.Value == timestamp).Key);
                }
                else
                {
                    newNames.Add(file, timestamp);
                }
            }
            // show if has any bad files
            var msg = "";
            if (filesMissingTimestamp.Count != 0)
            {
                msg = "Files that are missing time stamp: ";
                foreach (var filename in filesMissingTimestamp)
                {
                    msg += "\n" + filename;
                }
            }
            else if (filesHaveSameTimestamp.Count() != 0)
            {
                msg = "\nFiles that have the same time stamp: ";
                foreach (var filenamePair in filesHaveSameTimestamp)
                {
                    msg += "\n" + filenamePair.Key + " | " + filenamePair.Value;
                }
            }

            if (!string.IsNullOrEmpty(msg))
            {
                MessageBox.Show(msg);
                if (MessageBox.Show(this, "Continue?", "", MessageBoxButtons.YesNo) == DialogResult.No) return;
            }

            // passed checking, start renaming
            foreach (var file in files)
            {
                // skip bad files
                if(filesMissingTimestamp.Contains(file)) continue;
                if(filesHaveSameTimestamp.ContainsKey(file)) continue;

                File.Move(file,
                    Path.Combine(Path.GetDirectoryName(file),
                    GetDateTakenFromImage(file).ToString("yyyyMMddHHmmssfff") + Path.GetExtension(file)));
            }
            MessageBox.Show("All done.");
        }

        //we init this once so that if the function is repeatedly called
        //it isn't stressing the garbage man
        private static readonly Regex r = new Regex(":");

        //retrieves the datetime WITHOUT loading the whole image
        public static DateTime GetDateTakenFromImage(string path)
        {
            using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
            {
                using (Image myImage = Image.FromStream(fs, false, false))
                {
                    if (myImage.PropertyIdList.Any(i => i == 36867))
                    {
                        PropertyItem propItem = myImage.GetPropertyItem(36867);
                        string dateTaken = r.Replace(Encoding.UTF8.GetString(propItem.Value), "-", 2);
                        return DateTime.Parse(dateTaken);
                    }
                    return DateTime.MinValue;
                }
            }
        }
    }
}
