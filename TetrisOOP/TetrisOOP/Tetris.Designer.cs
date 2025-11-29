namespace TetrisOOP
{
    partial class TetrisForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            this.ClientSize = new System.Drawing.Size(560, 660);
            this.Text = "Тетрис — ООП (все требования выполнены)";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.KeyPreview = true;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.ResumeLayout(false);
        }
    }
}