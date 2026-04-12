using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Net;
using _4RTools.Utils.MuhBotCore;

namespace _4RTools.Model
{

    public class ClientDTO
    {
        public int index { get; set; }
        public string name { get; set; }
        public string description { get; set; }
        public string hpAddress { get; set; }
        public string nameAddress { get; set; }

        public int hpAddressPointer { get; set; }
        public int nameAddressPointer { get; set; }

        public ClientDTO() { }

        public ClientDTO(string name, string description, string hpAddress, string nameAddress)
        {
            this.name= name;
            this.description = description;
            this.hpAddress = hpAddress;
            this.nameAddress = nameAddress;

            this.hpAddressPointer = Convert.ToInt32(hpAddress, 16);
            this.nameAddressPointer = Convert.ToInt32(nameAddress, 16);
        }

    }


    public sealed class ClientListSingleton
    {
        private static List<Client> clients = new List<Client>();
        
        public static void AddClient(Client c)
        {
            clients.Add(c);
        }

        public static void RemoveClient(Client c)
        {
            clients.Remove(c);
        }

        public static List<Client> GetAll()
        {
            return clients;
        }

        public static bool ExistsByProcessName(string processName)
        {
            return clients.Exists(client => client.processName == processName);
        }
    }

    public sealed class ClientSingleton
    {
        private static Client client;
        private ClientSingleton(Client client)
        {
            ClientSingleton.client = client;
        }

        public static ClientSingleton Instance(Client client)
        {
            return new ClientSingleton(client);
        }

        public static Client GetClient()
        {
            return client;
        }
    }

    public class Client
    {
        public Process process => processManager?.Process;

        public string processName { get; private set; }
        public ProcessManager processManager { get; private set; }
        public MemoryReader memory { get; private set; }
        public InputSimulator input { get; private set; }

        public int currentNameAddress { get; set; }
        public int currentHPBaseAddress { get; set; }
        private int statusBufferAddress { get; set; }

        public AddressConfig AddressConfig { get; private set; }

        public Client(string processName, int currentHPBaseAddress, int currentNameAddress)
        {
            this.currentNameAddress = currentNameAddress;
            this.currentHPBaseAddress = currentHPBaseAddress;
            this.processName = processName;
            this.statusBufferAddress = currentHPBaseAddress + 0x474;
        }

        public Client(ClientDTO dto)
        {
            this.processName = dto.name;
            this.currentHPBaseAddress = Convert.ToInt32(dto.hpAddress, 16);
            this.currentNameAddress = Convert.ToInt32(dto.nameAddress, 16);
            this.statusBufferAddress = this.currentHPBaseAddress + 0x474;
        }

        public Client(string processAndPid)
        {
            string rawProcessName = processAndPid.Split(new string[] { ".exe - " }, StringSplitOptions.None)[0];
            int chosenPid = int.Parse(processAndPid.Split(new string[] { ".exe - " }, StringSplitOptions.None)[1]);

            this.processName = rawProcessName;
            this.processManager = new ProcessManager();
            if (this.processManager.Attach(chosenPid))
            {
                this.memory = new MemoryReader(this.processManager);
                this.AddressConfig = AddressConfig.LoadOrDefault();
                this.input = new InputSimulator(this.processManager, this.memory, this.AddressConfig.MousePosX, this.AddressConfig.MousePosY);

                try
                {
                    Client c = GetClientByProcess(rawProcessName);
                    if (c == null) throw new Exception();

                    this.currentHPBaseAddress = c.currentHPBaseAddress;
                    this.currentNameAddress = c.currentNameAddress;
                    this.statusBufferAddress = c.statusBufferAddress;
                }
                catch
                {
                    MessageBox.Show("This client is not supported. Only Spammers and macro will works.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    this.currentHPBaseAddress = 0;
                    this.currentNameAddress = 0;
                    this.statusBufferAddress = 0;
                }
            }
        }

        private string ReadMemoryAsString(int address)
        {
            if (memory == null) return "Unknown";
            return memory.ReadString((IntPtr)address, 40);
        }

        private uint ReadMemory(int address)
        {
            if (memory == null) return 0;
            return memory.ReadUInt32((IntPtr)address);
        }

        public void WriteMemory(int address, uint intToWrite)
        {
            if (memory == null) return;
            memory.WriteUInt32((IntPtr)address, intToWrite);
        }

        public void WriteMemory(int address, byte[] bytesToWrite)
        {
            if (memory == null) return;
            memory.WriteBytes((IntPtr)address, bytesToWrite);
        }

        public bool IsHpBelow(int percent)
        {
            return ReadCurrentHp() * 100 < (uint)percent * ReadMaxHp();
        }

        public bool IsSpBelow(int percent)
        {
            return ReadCurrentSp() * 100 < (uint)percent * ReadMaxSp();
        }

        public uint ReadCurrentHp()
        {
            return ReadMemory(this.currentHPBaseAddress);
        }

        public uint ReadCurrentSp()
        {
            return ReadMemory(this.currentHPBaseAddress + 8);
        }

        public uint ReadMaxHp()
        {
            return ReadMemory(this.currentHPBaseAddress + 4);
        }

        public string ReadCharacterName()
        {
            return ReadMemoryAsString(this.currentNameAddress);
        }

        public uint ReadMaxSp()
        {
            return ReadMemory(this.currentHPBaseAddress + 12);
        }

        public uint CurrentBuffStatusCode(int effectStatusIndex)
        {
            return ReadMemory(this.statusBufferAddress + effectStatusIndex * 4);
        }

        /// <summary>
        /// Reads the player entity's motion/action state from memory.
        /// Resolves the pointer chain: [[WorldBaseIntermed] + WorldBaseOffset] + PlayerBaseOffset → entity + PlayerStateOffset.
        /// Returns the raw state value, or uint.MaxValue if the chain can't be resolved.
        /// Common RO client values: 0 = idle/standing, 1 = walking, 2 = sitting, 3+ = busy (attacking/casting/etc).
        /// </summary>
        public uint ReadPlayerMotionState()
        {
            if (memory == null || processManager == null || !processManager.IsAttached)
                return uint.MaxValue;

            try
            {
                var cfg = new AddressConfig();
                IntPtr stateAddr = memory.ResolvePointerChain(
                    (IntPtr)cfg.WorldBaseIntermed,
                    new int[] { 0, cfg.WorldBaseOffset, cfg.PlayerBaseOffset },
                    cfg.PlayerStateOffset);

                if (stateAddr == IntPtr.Zero)
                    return uint.MaxValue;

                return memory.ReadUInt32(stateAddr);
            }
            catch
            {
                return uint.MaxValue;
            }
        }

        /// <summary>
        /// Returns true when the character is in a state that can accept skill input (idle or sitting).
        /// Returns true if memory reading fails (fail-open so the spammer doesn't get stuck).
        /// </summary>
        public bool IsPlayerIdle()
        {
            uint state = ReadPlayerMotionState();
            if (state == uint.MaxValue)
                return true; // can't read → don't block
            return state == 0 || state == 2; // 0 = idle/standing, 2 = sitting
        }

        public Client GetClientByProcess(string processName)
        {
            foreach(Client c in ClientListSingleton.GetAll())
            {
                if (c.processName == processName)
                {
                    if (LooksLikeLiveClient(c)) return c;
                }
            }
            return null;
        }

        private bool LooksLikeLiveClient(Client candidate)
        {
            try
            {
                uint currentHp = ReadMemory(candidate.currentHPBaseAddress);
                uint maxHp = ReadMemory(candidate.currentHPBaseAddress + 4);
                uint currentSp = ReadMemory(candidate.currentHPBaseAddress + 8);
                uint maxSp = ReadMemory(candidate.currentHPBaseAddress + 12);
                string characterName = ReadMemoryAsString(candidate.currentNameAddress).Trim();

                bool statsLookValid =
                    maxHp > 1 &&
                    maxHp < 10_000_000 &&
                    currentHp <= maxHp &&
                    maxSp < 10_000_000 &&
                    currentSp <= maxSp;

                bool nameLooksValid =
                    characterName.Length >= 2 &&
                    characterName.Length <= 24 &&
                    characterName.All(ch => !char.IsControl(ch));

                return statsLookValid && nameLooksValid;
            }
            catch
            {
                return false;
            }
        }
    
        public static Client FromDTO(ClientDTO dto)
        {
            return ClientListSingleton.GetAll()
                .Where(c => c.processName == dto.name)
                .Where(c => c.currentHPBaseAddress == dto.hpAddressPointer)
                .Where(c => c.currentNameAddress == dto.nameAddressPointer).FirstOrDefault();
        }
    }
}
