using _4RTools.Utils;
using _4RTools.Model;
using System.Windows.Forms;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _4RTools.Forms
{
    public partial class ClientUpdaterForm : Form
    {
        private System.Net.Http.HttpClient httpClient = new System.Net.Http.HttpClient();

        public ClientUpdaterForm()
        {
            var requestAccepts = httpClient.DefaultRequestHeaders.Accept;
            requestAccepts.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.UserAgent.TryParseAdd("request"); //Set the User Agent to "request"
            InitializeComponent();
            StartUpdate();
        }

        private async void StartUpdate()
        {
            List<ClientDTO> clients = new List<ClientDTO>();


            /**
             * Try to load remote supported_server.json file and append all data in clients list.
             */
            try
            {
                clients.AddRange(LocalServerManager.GetLocalClients()); //Load Local Servers First
                clients.AddRange(LoadBundledServerOverrides());
                //If fetch successfully update and load local file.
                httpClient.Timeout = TimeSpan.FromSeconds(5);
                string remoteServersRaw = await httpClient.GetStringAsync(AppConfig._4RClientsURL);
                clients.AddRange(JsonConvert.DeserializeObject<List<ClientDTO>>(remoteServersRaw));

            }
            catch(Exception)
            {
                //If catch some exception while Fetch, load resource file.
                MessageBox.Show("Cannot load supported_servers file. Loading resource instead....");
                clients.AddRange(JsonConvert.DeserializeObject<List<ClientDTO>>(LoadResourceServerFile()));
            }
            finally
            {
                LoadServers(clients);
                new Container().Show();
                Hide();
            }
        }

        private string LoadResourceServerFile()
        {
            return Resources._4RTools.ETCResource.supported_servers;
        }

        private List<ClientDTO> LoadBundledServerOverrides()
        {
            string path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "custom_supported_servers.json");
            if (!File.Exists(path))
            {
                return new List<ClientDTO>();
            }

            try
            {
                string json = File.ReadAllText(path);
                return JsonConvert.DeserializeObject<List<ClientDTO>>(json) ?? new List<ClientDTO>();
            }
            catch
            {
                return new List<ClientDTO>();
            }
        }

        private void LoadServers(List<ClientDTO> clients)
        {
            foreach (ClientDTO clientDTO in clients
                .Where(c => c != null && !string.IsNullOrWhiteSpace(c.name) && !string.IsNullOrWhiteSpace(c.hpAddress) && !string.IsNullOrWhiteSpace(c.nameAddress))
                .GroupBy(c => $"{c.name}|{c.hpAddress}|{c.nameAddress}")
                .Select(g => g.First()))
            {
                try
                {
                    ClientListSingleton.AddClient(new Client(clientDTO));
                    pbSupportedServer.Increment(1);
                }
                catch { }
                
            }
        }
    }
}
