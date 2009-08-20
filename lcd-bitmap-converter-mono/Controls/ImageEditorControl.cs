using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.Drawing.Imaging;
using System.Xml;
using System.IO;
using System.Xml.Xsl;

namespace lcd_bitmap_converter_mono
{
    public partial class ImageEditorControl : UserControl, IConvertorPart
    {
        private string mFileName;
        public ImageEditorControl()
        {
            InitializeComponent();
            this.mFileName = String.Empty;
        }
        private ColorMatrix ColorMatrixBW
        {
            get
            {
                ColorMatrix colorMatrixBW;
                float[][] matrixItemsBW ={
                    new float[] {0.299f, 0.299f, 0.299f, 0, 0},
                    new float[] {0.587f, 0.587f, 0.587f, 0, 0},
                    new float[] {0.114f, 0.114f, 0.114f, 0, 0},
                    new float[] {0, 0, 0, 1, 0},
                    new float[] {0, 0, 0, 0, 1}};
                colorMatrixBW = new ColorMatrix(matrixItemsBW);
                return colorMatrixBW;
            }
        }

        private Bitmap GetMonochrome(Bitmap bmp, float edge)
        {
            Bitmap bmp2 = new Bitmap(bmp.Width, bmp.Height, PixelFormat.Format1bppIndexed);

            //ImageAttributes imageAtt = new ImageAttributes();
            //imageAtt.SetColorMatrix(this.ColorMatrixBW, ColorMatrixFlag.Default, ColorAdjustType.Bitmap);

            //Graphics.FromImage(bmp2).DrawImage(
            //    bmp,
            //    Rectangle.FromLTRB(0, 0, bmp2.Width, bmp2.Height),
            //    0.0f,
            //    0.0f,
            //    bmp2.Width,
            //    bmp2.Height,
            //    GraphicsUnit.Pixel,
            //    imageAtt);

            BitmapData bmd = bmp2.LockBits(new Rectangle(0, 0, bmp.Width, bmp.Height), ImageLockMode.WriteOnly, PixelFormat.Format1bppIndexed);
            for (int i = 0; i < bmp.Width; i++)
            {
                for (int j = 0; j < bmp.Height; j++)
                {
                    float br = bmp.GetPixel(i, j).GetBrightness();
                    //Console.WriteLine(br.ToString());
                    if (br > edge)
                        //bmp2.SetPixel(i, j, Color.White);
                        BitmapHelper.SetPixel(bmd, i, j, true);
                    else
                        //bmp2.SetPixel(i, j, Color.Black);
                        BitmapHelper.SetPixel(bmd, i, j, false);
                }
            }
            bmp2.UnlockBits(bmd);
            return bmp2;
        }

        private XmlDocument GetXmlDocument(bool flipHorizontal, bool flipVertical, RotateAngle angle)
        {
            XmlDocument doc = new XmlDocument();
            doc.AppendChild(doc.CreateXmlDeclaration("1.0", "utf-8", null));
            XmlNode root = doc.AppendChild(doc.CreateElement("data"));
            (root as XmlElement).SetAttribute("type", "image");
            (root as XmlElement).SetAttribute("filename", this.mFileName);
            (root as XmlElement).SetAttribute("name", Path.GetFileNameWithoutExtension(this.mFileName));
            //XmlNode nodeImage = root.AppendChild(doc.CreateElement("item"));
            XmlNode nodeBitmap = root.AppendChild(doc.CreateElement("bitmap"));
            this.mBmpEditor.SaveToXml(nodeBitmap, flipHorizontal, flipVertical, angle);
            return doc;
        }
        private void SaveBitmapToXml(string filename)
        {
            this.mFileName = filename;
            XmlDocument doc = this.GetXmlDocument(false, false, RotateAngle.None);
            doc.Save(filename);
        }

        private void LoadBitmapFromXml(string filename)
        {
            try
            {
                XmlDocument doc = new XmlDocument();
                doc.Load(filename);
                XmlNode root = doc.DocumentElement;
                if (root.Attributes["type"] != null)
                {
                    if (root.Attributes["type"].Value == "image")
                    {
                        XmlNode nodeBitmap = root.SelectSingleNode("bitmap");
                        if (nodeBitmap != null)
                            this.mBmpEditor.LoadFromXml(nodeBitmap);
                        else
                            throw new Exception("Invalid format of file, 'bitmap' node not found");
                    }
                    else
                        throw new Exception("Invalid format of file, attribute 'type' must be equal to 'image'");
                }
                else
                    throw new Exception("Invalid format of file, attribute 'type' not defined");
            }
            catch (Exception exc)
            {
                MessageBox.Show(exc.Message, "Error while loading file", MessageBoxButtons.OK, MessageBoxIcon.Stop);
            }
        }

        #region IConvertorPart
        public void LoadData()
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;
                ofd.DefaultExt = ".xml";
                ofd.Filter = "Bitmaps (*.bmp)|*.bmp|Images (*.bmp; *.jpg; *.png)|*.bmp;*.png;*.jpg;*.jpeg|XML files(*.xml)|*.xml";
                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    string filename = ofd.FileName;
                    string ext = Path.GetExtension(filename);
                    //MessageBox.Show(filename);
                    if (ext == ".bmp" || ext == ".jpeg" || ext == ".jpg" || ext == ".png")
                    {
                        Bitmap bmp = new Bitmap(filename);
                        //Image im = Image.FromFile(filename);
                        this.mBmpEditor.Bmp = this.GetMonochrome(bmp, 0.5f);
                    }
                    if (ext == ".xml")
                    {
                        this.LoadBitmapFromXml(filename);
                    }
                }
            }
        }
        public void SaveData()
        {
            if (String.IsNullOrEmpty(this.mFileName))
                this.SaveDataAs();
            else
            {
                string ext = Path.GetExtension(this.mFileName);
                if (ext == ".bmp")
                    this.mBmpEditor.Bmp.Save(this.mFileName);
                if (ext == ".xml")
                    this.SaveBitmapToXml(this.mFileName);
            }
        }
        public void SaveDataAs()
        {
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.AddExtension = true;
                sfd.CheckPathExists = true;
                sfd.DefaultExt = ".bmp";
                sfd.Filter = "*Bitmaps (*.bmp)|*.bmp|XML files (*.xml)|*.xml";
                sfd.OverwritePrompt = true;
                sfd.Title = "Save file...";
                if (sfd.ShowDialog() == DialogResult.OK)
                {
                    this.mFileName = sfd.FileName;
                    this.SaveData();
                }
            }
        }
        public void RotateFlip(bool horizontalFlip, bool verticalFlip, RotateAngle angle)
        {
            this.mBmpEditor.Bmp = BitmapHelper.RotateFlip(this.mBmpEditor.Bmp, horizontalFlip, verticalFlip, angle);
            this.mBmpEditor.Invalidate();
        }
        public void Inverse()
        {
            this.mBmpEditor.Inverse();
        }
        public void Convert()
        {
            string xsltFilename = SavedContainer<Options>.Instance.ImageStyleFilename;
            if (String.IsNullOrEmpty(xsltFilename))
            {
                MessageBox.Show("Conversion not possible, because xslt file not specified.", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }
            else if (!File.Exists(xsltFilename))
            {
                MessageBox.Show("Conversion not possible, because specified xslt file not exists.", "Error", MessageBoxButtons.OKCancel, MessageBoxIcon.Error);
            }
            else
            {
                try
                {
                    using (SaveFileDialog sfd = new SaveFileDialog())
                    {
                        sfd.AddExtension = true;
                        sfd.CheckPathExists = true;
                        sfd.DefaultExt = ".c";
                        sfd.Filter = "All files (*.*)|*.*";
                        sfd.OverwritePrompt = true;
                        sfd.Title = "Save file...";

                        XslCompiledTransform trans = new XslCompiledTransform();
                        trans.Load(xsltFilename);

                        if (sfd.ShowDialog() == DialogResult.OK)
                        {
                            using (XmlWriter writer = XmlWriter.Create(sfd.FileName, trans.OutputSettings))
                            {
                                XmlDocument doc = this.GetXmlDocument(
                                    SavedContainer<Options>.Instance.OperationFlipHorizontal,
                                    SavedContainer<Options>.Instance.OperationFlipVertical,
                                    SavedContainer<Options>.Instance.OperationRotateAngle);
                                trans.Transform(doc, writer);
                            }
                        }
                    }
                }
                catch (Exception exc)
                {
                    MessageBox.Show(exc.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        #endregion
    }
}
