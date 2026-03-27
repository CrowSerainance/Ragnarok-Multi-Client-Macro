using System;
namespace _4RTools
{
    internal static class Program
    {
        /// <summary>
        /// Ponto de entrada principal para o aplicativo.
        /// </summary>
        [STAThread]
        static void Main()
        {
            System.Windows.Forms.Application.EnableVisualStyles();
            System.Windows.Forms.Application.SetCompatibleTextRenderingDefault(false);

            System.Windows.Forms.Application.SetUnhandledExceptionMode(
                System.Windows.Forms.UnhandledExceptionMode.CatchException);
            System.Windows.Forms.Application.ThreadException += (s, e) =>
            {
                System.IO.File.WriteAllText("crash.log",
                    $"[ThreadException] {DateTime.Now}\n{e.Exception}");
                System.Windows.Forms.MessageBox.Show(
                    e.Exception.ToString(), "4RTools Crash",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            };
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                System.IO.File.WriteAllText("crash.log",
                    $"[UnhandledException] {DateTime.Now}\n{e.ExceptionObject}");
                System.Windows.Forms.MessageBox.Show(
                    e.ExceptionObject.ToString(), "4RTools Crash",
                    System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
            };

            // Skip auto-patcher and server fetch — go straight to main window.
            // The ClientUpdaterForm normally loads supported_servers.json, which populates
            // the process list. We do a minimal local-only load instead.
            try
            {
                var clients = new System.Collections.Generic.List<Model.ClientDTO>();

                // Load local servers
                clients.AddRange(Model.LocalServerManager.GetLocalClients());

                // Load bundled custom overrides
                string customPath = System.IO.Path.Combine(
                    AppDomain.CurrentDomain.BaseDirectory, "custom_supported_servers.json");
                if (System.IO.File.Exists(customPath))
                {
                    var custom = Newtonsoft.Json.JsonConvert.DeserializeObject<
                        System.Collections.Generic.List<Model.ClientDTO>>(
                        System.IO.File.ReadAllText(customPath));
                    if (custom != null) clients.AddRange(custom);
                }

                // Load bundled resource fallback
                try
                {
                    var resource = Newtonsoft.Json.JsonConvert.DeserializeObject<
                        System.Collections.Generic.List<Model.ClientDTO>>(
                        Resources._4RTools.ETCResource.supported_servers);
                    if (resource != null) clients.AddRange(resource);
                }
                catch { }

                // Register clients
                foreach (var dto in clients)
                {
                    if (dto == null || string.IsNullOrWhiteSpace(dto.name) ||
                        string.IsNullOrWhiteSpace(dto.hpAddress) ||
                        string.IsNullOrWhiteSpace(dto.nameAddress)) continue;
                    try { Model.ClientListSingleton.AddClient(new Model.Client(dto)); }
                    catch { }
                }
            }
            catch { }

            Forms.Container app = new Forms.Container();
            System.Windows.Forms.Application.Run(app);
        }
    }
}
