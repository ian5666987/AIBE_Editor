namespace AibeEditor {
  partial class CheckerPanel {
    /// <summary> 
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary> 
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose(bool disposing) {
      if (disposing && (components != null)) {
        components.Dispose();
      }
      base.Dispose(disposing);
    }

    #region Component Designer generated code

    /// <summary> 
    /// Required method for Designer support - do not modify 
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent() {
      this.labelTitle = new System.Windows.Forms.Label();
      this.richTextBoxSyntax = new System.Windows.Forms.RichTextBox();
      this.splitContainerCore = new System.Windows.Forms.SplitContainer();
      this.splitContainerContent = new System.Windows.Forms.SplitContainer();
      this.richTextBoxResult = new System.Windows.Forms.RichTextBox();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainerCore)).BeginInit();
      this.splitContainerCore.Panel1.SuspendLayout();
      this.splitContainerCore.Panel2.SuspendLayout();
      this.splitContainerCore.SuspendLayout();
      ((System.ComponentModel.ISupportInitialize)(this.splitContainerContent)).BeginInit();
      this.splitContainerContent.Panel1.SuspendLayout();
      this.splitContainerContent.Panel2.SuspendLayout();
      this.splitContainerContent.SuspendLayout();
      this.SuspendLayout();
      // 
      // labelTitle
      // 
      this.labelTitle.AutoSize = true;
      this.labelTitle.Dock = System.Windows.Forms.DockStyle.Left;
      this.labelTitle.Location = new System.Drawing.Point(0, 0);
      this.labelTitle.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
      this.labelTitle.Name = "labelTitle";
      this.labelTitle.Size = new System.Drawing.Size(45, 24);
      this.labelTitle.TabIndex = 0;
      this.labelTitle.Text = "Title";
      // 
      // richTextBoxSyntax
      // 
      this.richTextBoxSyntax.Dock = System.Windows.Forms.DockStyle.Fill;
      this.richTextBoxSyntax.Location = new System.Drawing.Point(0, 0);
      this.richTextBoxSyntax.Name = "richTextBoxSyntax";
      this.richTextBoxSyntax.Size = new System.Drawing.Size(285, 207);
      this.richTextBoxSyntax.TabIndex = 1;
      this.richTextBoxSyntax.Text = "";
      this.richTextBoxSyntax.TextChanged += new System.EventHandler(this.richTextBoxSyntax_TextChanged);
      // 
      // splitContainerCore
      // 
      this.splitContainerCore.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitContainerCore.FixedPanel = System.Windows.Forms.FixedPanel.Panel1;
      this.splitContainerCore.Location = new System.Drawing.Point(0, 0);
      this.splitContainerCore.Name = "splitContainerCore";
      this.splitContainerCore.Orientation = System.Windows.Forms.Orientation.Horizontal;
      // 
      // splitContainerCore.Panel1
      // 
      this.splitContainerCore.Panel1.Controls.Add(this.labelTitle);
      // 
      // splitContainerCore.Panel2
      // 
      this.splitContainerCore.Panel2.Controls.Add(this.splitContainerContent);
      this.splitContainerCore.Size = new System.Drawing.Size(589, 236);
      this.splitContainerCore.SplitterDistance = 25;
      this.splitContainerCore.TabIndex = 2;
      // 
      // splitContainerContent
      // 
      this.splitContainerContent.Dock = System.Windows.Forms.DockStyle.Fill;
      this.splitContainerContent.Location = new System.Drawing.Point(0, 0);
      this.splitContainerContent.Name = "splitContainerContent";
      // 
      // splitContainerContent.Panel1
      // 
      this.splitContainerContent.Panel1.Controls.Add(this.richTextBoxSyntax);
      // 
      // splitContainerContent.Panel2
      // 
      this.splitContainerContent.Panel2.Controls.Add(this.richTextBoxResult);
      this.splitContainerContent.Size = new System.Drawing.Size(589, 207);
      this.splitContainerContent.SplitterDistance = 285;
      this.splitContainerContent.TabIndex = 0;
      // 
      // richTextBoxResult
      // 
      this.richTextBoxResult.Dock = System.Windows.Forms.DockStyle.Fill;
      this.richTextBoxResult.Location = new System.Drawing.Point(0, 0);
      this.richTextBoxResult.Name = "richTextBoxResult";
      this.richTextBoxResult.ReadOnly = true;
      this.richTextBoxResult.Size = new System.Drawing.Size(300, 207);
      this.richTextBoxResult.TabIndex = 2;
      this.richTextBoxResult.Text = "";
      // 
      // CheckerPanel
      // 
      this.AutoScaleDimensions = new System.Drawing.SizeF(10F, 22F);
      this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
      this.Controls.Add(this.splitContainerCore);
      this.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
      this.Margin = new System.Windows.Forms.Padding(4);
      this.Name = "CheckerPanel";
      this.Size = new System.Drawing.Size(589, 236);
      this.splitContainerCore.Panel1.ResumeLayout(false);
      this.splitContainerCore.Panel1.PerformLayout();
      this.splitContainerCore.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitContainerCore)).EndInit();
      this.splitContainerCore.ResumeLayout(false);
      this.splitContainerContent.Panel1.ResumeLayout(false);
      this.splitContainerContent.Panel2.ResumeLayout(false);
      ((System.ComponentModel.ISupportInitialize)(this.splitContainerContent)).EndInit();
      this.splitContainerContent.ResumeLayout(false);
      this.ResumeLayout(false);

    }

    #endregion

    private System.Windows.Forms.Label labelTitle;
    private System.Windows.Forms.RichTextBox richTextBoxSyntax;
    private System.Windows.Forms.SplitContainer splitContainerCore;
    private System.Windows.Forms.SplitContainer splitContainerContent;
    private System.Windows.Forms.RichTextBox richTextBoxResult;
  }
}
