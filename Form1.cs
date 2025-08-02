using System;
using System.Drawing;
using System.Windows.Forms;

namespace photo
{
    public partial class Form1 : Form
    {
        // �̹��� �巡�� �� ���θ� ��Ÿ���� �÷���
        private bool isDragging = false;

        // �巡�� ���� �� ���콺 Ŭ�� ���� ��ǥ
        private Point clickOffset;

        // ���� �׵θ��� ǥ������ ���� (���콺 Ŭ�� �� true)
        private bool showSelectionBorder = false;

        // ���� �̹��� �����
        private Bitmap originalImage;

        public Form1()
        {
            InitializeComponent();
            pictureBox1.Paint += pictureBox1_Paint;
        }

        private void btn_NewFile_Click(object sender, EventArgs e)
        {
            pictureBox1.Image = null;
        }

        private void btn_Open_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Title = "�̹��� ����";
            openFileDialog.Filter = "�̹��� ����|*.jpg;*.jpeg;*.png;*.bmp;*.gif";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    pictureBox1.Image?.Dispose();
                    Image img = Image.FromFile(openFileDialog.FileName);
                    originalImage = (Bitmap)img;
                    pictureBox1.Image = (Bitmap)originalImage.Clone();
                    pictureBox1.SizeMode = PictureBoxSizeMode.AutoSize;
                    pictureBox1.Size = img.Size;
                    pictureBox1.Location = new Point(10, 10);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("�̹����� �ҷ����� �� ���� �߻�: " + ex.Message);
                }
            }
        }

        private void btn_Save_Click(object sender, EventArgs e)
        {
            // TODO: ���� ��� ����
        }

        private void pictureBox1_MouseDown(object sender, MouseEventArgs e)
        {
            if (pictureBox1.Image != null && e.Button == MouseButtons.Left)
            {
                isDragging = true;
                clickOffset = e.Location;
                showSelectionBorder = true;
                pictureBox1.Invalidate();
            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                Point newLocation = pictureBox1.Location;
                newLocation.X += e.X - clickOffset.X;
                newLocation.Y += e.Y - clickOffset.Y;
                pictureBox1.Location = newLocation;
            }
        }

        private void pictureBox1_MouseUp(object sender, MouseEventArgs e)
        {
            isDragging = false;
            showSelectionBorder = false;
            pictureBox1.Invalidate();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // �ʱ�ȭ �ڵ� �ʿ� �� �ۼ�
            trackRed.Minimum = 0;
            trackRed.Maximum = 255;
            trackRed.Value = 128;

            trackGreen.Minimum = 0;
            trackGreen.Maximum = 255;
            trackGreen.Value = 128;

            trackBlue.Minimum = 0;
            trackBlue.Maximum = 255;
            trackBlue.Value = 128;
        }

        private void pictureBox1_Paint(object sender, PaintEventArgs e)
        {
            if (showSelectionBorder)
            {
                using (Pen pen = new Pen(Color.DeepSkyBlue, 2))
                {
                    pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
                    Rectangle rect = new Rectangle(0, 0, pictureBox1.Width - 1, pictureBox1.Height - 1);
                    e.Graphics.DrawRectangle(pen, rect);
                }
            }
        }


        //�����ϴºκ�
        private void button1_Click(object sender, EventArgs e)
        {
            grpFilterPanel.Visible = !grpFilterPanel.Visible;
        }

        private void label2_Click(object sender, EventArgs e)
        {
            // �̻��
        }

        private Bitmap ApplyWarmFilter(Bitmap img)
        {
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color pixel = img.GetPixel(x, y);
                    int r = Math.Min(pixel.R + 30, 255);
                    int g = pixel.G;
                    int b = Math.Max(pixel.B - 30, 0);
                    img.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return img;
        }

        private Bitmap ApplyCoolFilter(Bitmap img)
        {
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color pixel = img.GetPixel(x, y);
                    int r = Math.Max(pixel.R - 30, 0);
                    int g = pixel.G;
                    int b = Math.Min(pixel.B + 30, 255);
                    img.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return img;
        }

        private Bitmap ApplyVintageFilter(Bitmap img)
        {
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color pixel = img.GetPixel(x, y);
                    int avg = (pixel.R + pixel.G + pixel.B) / 3;
                    int r = Math.Min(avg + 20, 255);
                    int g = avg;
                    int b = Math.Max(avg - 20, 0);
                    img.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return img;
        }

        private void button11_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null && originalImage != null)
                pictureBox1.Image = ApplyWarmFilter((Bitmap)originalImage.Clone());
        }

        private void btnCool_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null && originalImage != null)
                pictureBox1.Image = ApplyCoolFilter((Bitmap)originalImage.Clone());
        }

        private void btnVintage_Click(object sender, EventArgs e)
        {
            if (pictureBox1.Image != null && originalImage != null)
                pictureBox1.Image = ApplyVintageFilter((Bitmap)originalImage.Clone());
        }

        private void btnOriginal_Click(object sender, EventArgs e)
        {
            if (originalImage != null)
                pictureBox1.Image = (Bitmap)originalImage.Clone();
        }

        private void btnApplyRGB_Click(object sender, EventArgs e)
        {
            if (originalImage == null) return;

            int rAdj = trackRed.Value - 128;
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;

            pictureBox1.Image = AdjustRGB((Bitmap)originalImage.Clone(), rAdj, gAdj, bAdj);
        }

        private Bitmap AdjustRGB(Bitmap img, int rAdj, int gAdj, int bAdj)
        {
            for (int y = 0; y < img.Height; y++)
            {
                for (int x = 0; x < img.Width; x++)
                {
                    Color pixel = img.GetPixel(x, y);
                    int r = Math.Min(Math.Max(pixel.R + rAdj, 0), 255);
                    int g = Math.Min(Math.Max(pixel.G + gAdj, 0), 255);
                    int b = Math.Min(Math.Max(pixel.B + bAdj, 0), 255);
                    img.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }
            return img;
        }

        private void trackRed_Scroll(object sender, EventArgs e)
        {
            txtRed.Text = trackRed.Value.ToString();
            ApplyRGBPreview();
        }

        private void trackGreen_Scroll(object sender, EventArgs e)
        {
            txtGreen.Text = trackGreen.Value.ToString();
            ApplyRGBPreview();
        }

        private void trackBlue_Scroll(object sender, EventArgs e)
        {
            txtBlue.Text = trackBlue.Value.ToString();
            ApplyRGBPreview();
        }

        private void ApplyRGBPreview()
        {
            if (originalImage == null) return;

            int rAdj = trackRed.Value - 128;
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;

            pictureBox1.Image = AdjustRGB((Bitmap)originalImage.Clone(), rAdj, gAdj, bAdj);
        }

        private void btnResetRGB_Click(object sender, EventArgs e)
        {
            trackRed.Value = 128;
            trackGreen.Value = 128;
            trackBlue.Value = 128;

            txtRed.Text = "128";
            txtGreen.Text = "128";
            txtBlue.Text = "128";

            // �̹����� �������� ����
            if (originalImage != null)
            {
                pictureBox1.Image = (Bitmap)originalImage.Clone();
            }
        }
        private bool isTextChanging = false;  //�ߺ��̺�Ʈ ����
        private void txtRed_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;

            isTextChanging = true;

            if (int.TryParse(txtRed.Text, out int val))
            {
                if (val < 0)
                {
                    MessageBox.Show("0���� ���� ���� ������ �� �����ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    val = 0;
                }
                else if (val > 255)
                {
                    MessageBox.Show("255���� ū ���� ������ �� �����ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    val = 255;
                }
                txtRed.Text = val.ToString();
                trackRed.Value = val;
                ApplyRGBAdjust();
            }
            else if (!string.IsNullOrEmpty(txtRed.Text))
            {
                MessageBox.Show("���ڸ� �Է� �����մϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRed.Text = trackRed.Value.ToString();
            }

            isTextChanging = false;
        }

        private void txtGreen_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;

            isTextChanging = true;

            if (int.TryParse(txtRed.Text, out int val))
            {
                if (val < 0)
                {
                    MessageBox.Show("0���� ���� ���� ������ �� �����ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    val = 0;
                }
                else if (val > 255)
                {
                    MessageBox.Show("255���� ū ���� ������ �� �����ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    val = 255;
                }
                txtRed.Text = val.ToString();
                trackRed.Value = val;
                ApplyRGBAdjust();
            }
            else if (!string.IsNullOrEmpty(txtRed.Text))
            {
                MessageBox.Show("���ڸ� �Է� �����մϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRed.Text = trackRed.Value.ToString();
            }

            isTextChanging = false;
        }

        private void txtBlue_TextChanged(object sender, EventArgs e)
        {
            if (isTextChanging) return;

            isTextChanging = true;

            if (int.TryParse(txtRed.Text, out int val))
            {
                if (val < 0)
                {
                    MessageBox.Show("0���� ���� ���� ������ �� �����ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    val = 0;
                }
                else if (val > 255)
                {
                    MessageBox.Show("255���� ū ���� ������ �� �����ϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    val = 255;
                }
                txtRed.Text = val.ToString();
                trackRed.Value = val;
                ApplyRGBAdjust();
            }
            else if (!string.IsNullOrEmpty(txtRed.Text))
            {
                MessageBox.Show("���ڸ� �Է� �����մϴ�.", "���", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtRed.Text = trackRed.Value.ToString();
            }

            isTextChanging = false;
        }
        private void ApplyRGBAdjust()
        {
            if (originalImage == null) return;

            // Ʈ���� ���� 0~255�� ����, ���ذ� 128���� �󸶳� �������� ���
            int rAdj = trackRed.Value - 128;
            int gAdj = trackGreen.Value - 128;
            int bAdj = trackBlue.Value - 128;

            pictureBox1.Image = AdjustRGB((Bitmap)originalImage.Clone(), rAdj, gAdj, bAdj);
        }
    }
}