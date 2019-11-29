using System.Windows.Forms;
using CefSharp.WinForms;

namespace TSSArt.StateMachine.Services
{
	public partial class BrowserForm : Form
	{
		public BrowserForm(string url)
		{
			InitializeComponent();
			var webBrowser = new ChromiumWebBrowser(url) { Dock = DockStyle.Fill };
			Controls.Add(webBrowser);
		}
	}
}