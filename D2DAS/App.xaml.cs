using System.IO;
using System.Windows;
using Newtonsoft.Json;
using D2DAS.Server;

namespace D2DAS
{
	public partial class App : Application
	{
		private void Application_Startup(object sender, StartupEventArgs e)
		{
			// Load the settings
			Settings = JsonConvert.DeserializeObject<Settings>(File.ReadAllText("settings.json"));
			_server = new SimpleHTTPServer("overlay", 4711);
		}

		private void Application_Exit(object sender, ExitEventArgs e)
		{
			// This needs to be done, otherwise an invisible process will still be running
			_server.Stop();
		}

		private SimpleHTTPServer _server;

		public static Settings Settings;
	}
}
