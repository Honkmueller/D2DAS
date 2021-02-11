using System;
using System.Diagnostics;
using System.Windows;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Xml.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jupiter;
using Newtonsoft.Json;
using System.IO;

namespace D2DAS.D2Hacker
{
	public class Hacker
	{
		public Hacker()
		{
			// Initialize the dictionary entries
			_buffState.BuffQueue[BuffType.Health] = new Queue<Buff>();
			_buffState.BuffQueue[BuffType.Mana] = new Queue<Buff>();
			_buffState.BuffQueue[BuffType.Gold] = new Queue<Buff>();
			_buffState.BuffQueue[BuffType.Speed] = new Queue<Buff>();

			_buffState.CurrentBuffs[BuffType.Health] = null;
			_buffState.CurrentBuffs[BuffType.Mana] = null;
			_buffState.CurrentBuffs[BuffType.Gold] = null;
			_buffState.CurrentBuffs[BuffType.Speed] = null;

			_maxValues[BuffType.Health] = 0;
			_maxValues[BuffType.Mana] = 0;
			_maxValues[BuffType.Gold] = 0;
			_maxValues[BuffType.Speed] = 0;
		}

		public bool Connect()
		{
			Process[] processes = Process.GetProcessesByName("Game");
			if (processes.Length > 0)
			{
				Process process = null;
				foreach (Process gameProcess in processes)
				{
					if (gameProcess.MainWindowTitle == "Diablo II")
					{
						process = gameProcess;
						break;
					}
				}

				if (process == null)
				{
					Console.WriteLine("Did not find Diablo II");
					return false;
				}

				ProcessModule module = process.Modules.Cast<ProcessModule>().SingleOrDefault(m => string.Equals(m.ModuleName, "Game.exe", StringComparison.OrdinalIgnoreCase));

				if (module != null)
				{
					_baseAddress = module.BaseAddress;
					Console.WriteLine($"Found Diablo II - Game.exe at BaseAddress {_baseAddress}");
					_memoryModule = new MemoryModule(process.Id);

					// Now, that we have the base address of the game we can initialize the only real pointer-chains we have
					_offsets[BuffType.Speed] = new List<int>
					{
						(0x3A069C + (int)_baseAddress),
						0x10F8,
						0x0,
						0x2C,
						0x7C
					};

					_speedReadOffsets = new List<int>
					{
						(0x3A069C + (int)_baseAddress),
						0x10F8,
						0x0,
						0x2C,
						0x84
					};

					if (_offsets.Count > 1)
					{
						EnableHack();
					}

					return true;
				}
				Console.WriteLine("Found Diablo II but not Game.exe");
				return false;
			}
			Console.WriteLine("Did not find Diablo II");
			return false;
		}

		// We parse the infos from a copied pointer group from Cheat Engine.
		// Yes, we use direkt addresses. I know :( It's the best we got for now. 
		public bool SetupOffsetsFromClipboard()
		{
			string textInClipboard = Clipboard.GetText();

			if (string.IsNullOrWhiteSpace(textInClipboard))
			{
				return false;
			}

			bool xmlValid = true;

			// let's YOLO the parsing...
			try
			{
				XElement element = XElement.Parse(textInClipboard);
				XElement entries = element.Element("CheatEntries").Element("CheatEntry").Element("CheatEntries");

				int offsetsSet = 0;

				foreach (XElement entry in entries.Elements("CheatEntry"))
				{
					if (entry.Element("Description").Value == "\"Health\"")
					{
						int address = int.Parse(entry.Element("LastState").Attribute("RealAddress").Value, NumberStyles.HexNumber);
						_offsets[BuffType.Health] = new List<int> { address };
						offsetsSet++;
					}
					else if (entry.Element("Description").Value == "\"Mana\"")
					{
						int address = int.Parse(entry.Element("LastState").Attribute("RealAddress").Value, NumberStyles.HexNumber);
						_offsets[BuffType.Mana] = new List<int> { address };
						offsetsSet++;
					}
					else if (entry.Element("Description").Value == "\"Gold on person\"")
					{
						int address = int.Parse(entry.Element("LastState").Attribute("RealAddress").Value, NumberStyles.HexNumber);
						_offsets[BuffType.Gold] = new List<int> { address };
						offsetsSet++;
					}
				}

				xmlValid = offsetsSet == 3;
			}
			catch
			{
				xmlValid = false;
			}

			if (xmlValid && IsConnected())
			{
				EnableHack();
			}

			return xmlValid;
		}

		public int GetMaxValue(BuffType type)
		{
			int maxValue;
			_maxValuesMutex.WaitOne();
			maxValue = _maxValues[type];
			_maxValuesMutex.ReleaseMutex();
			return maxValue;
		}

		public void EnqueueBuff(Buff buff)
		{
			_buffQueueMutex.WaitOne();
			_buffState.BuffQueue[buff.Type].Enqueue(buff);
			_buffQueueMutex.ReleaseMutex();
		}

		private async void EnableHack()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = _cancellationTokenSource.Token;
			await Task.Run(() => ApplyHack(token));
			_cancellationTokenSource.Dispose();
		}

		private void ApplyHack(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{

				ApplyBuff(BuffType.Health);

				ApplyBuff(BuffType.Mana);

				ApplyBuff(BuffType.Gold);

				// Why treat speed differently, you say?
				// There is a handy memory address, where the result of the movement speed calculation is stored.
				// This value is kinda redundant and not used in further calculations.
				// We use that value and apply the boost to it. This way there is a difference between wlaking and running or dashing. Neat, huh?
				IntPtr pointer = GetPointer(_speedReadOffsets);
				int baseSpeed = _memoryModule.ReadVirtualMemory<int>(pointer);
				ApplyBuff(BuffType.Speed, baseSpeed);

				_buffQueueMutex.WaitOne();
				// The buffState.json is periodically read by the overlay
				string buffStateJson = JsonConvert.SerializeObject(_buffState);
				try
				{
					File.WriteAllText("overlay/buffState.json", buffStateJson);
				}
				catch { /* It can fail, due to another process (web server) reading from it. We do not worry and try again next tick. */ }
				_buffQueueMutex.ReleaseMutex();

				Thread.Sleep(FRAME_DURATION);
			}
		}

		private void ApplyBuff(
			BuffType type, 
			int baseValue = -1)
		{
			IntPtr pointer = GetPointer(_offsets[type]);

			bool getNextQueuedBuff = true;

			if (_buffState.CurrentBuffs[type] != null)
			{
				Buff buff = _buffState.CurrentBuffs[type];

				if (buff.DurationLeft >= 0)
				{
					// I know.. calculating the applied value each tick is unnecessary for the most cases.
					// Feel free to optimize it.
					if (baseValue == -1)
					{
						if (buff.BaseValue == -1)
						{
							buff.BaseValue = _memoryModule.ReadVirtualMemory<int>(pointer);
						}
						baseValue = buff.BaseValue;
					}

					int appliedValue;

					int boost;

					if (buff.ValueType == BuffValueType.Percentage)
					{
						boost = (int)(baseValue * (((float)buff.Value) / 100.0f));
					}
					else
					{
						boost = buff.Value;
					}

					if (buff.Effect == BuffEffect.Relative)
					{
						appliedValue = baseValue + boost;
					}
					else
					{
						appliedValue = boost;
					}

					if (appliedValue < 0)
					{
						appliedValue = 0;
					}

					_memoryModule.WriteVirtualMemory(pointer, appliedValue);
				}

				buff.DurationLeft -= FRAME_DURATION;
				// I added a short grace period between buffs.
				getNextQueuedBuff = buff.DurationLeft <= BUFF_COOLDOWN;
			}

			if (getNextQueuedBuff)
			{
				_buffQueueMutex.WaitOne();
				if (_buffState.BuffQueue[type].Count > 0)
				{
					_buffState.CurrentBuffs[type] = _buffState.BuffQueue[type].Dequeue();
				}
				else
				{
					_buffState.CurrentBuffs[type] = null;
				}
				_buffQueueMutex.ReleaseMutex();
			}

			// If the baseValue is not yet set, do it now
			if (baseValue == -1)
			{
				baseValue = _memoryModule.ReadVirtualMemory<int>(pointer);
			}


			// baseValue is the unmodified value. Here we try to get the max value at any given time.
			// This way we can determain max health and max mana
			_maxValuesMutex.WaitOne();
			if (baseValue > _maxValues[type])
			{
				_maxValues[type] = baseValue;
			}
			_maxValuesMutex.ReleaseMutex();
		}

		private void DisableHack()
		{
			_cancellationTokenSource.Cancel();
		}

		private IntPtr GetPointer(List<int> offsets)
		{
			IntPtr pointer = (IntPtr)offsets[0];

			for (int i = 1; i < offsets.Count; i++)
			{
				pointer = (IntPtr)_memoryModule.ReadVirtualMemory<int>(pointer);
				pointer += offsets[i];
			}

			return pointer;
		}

		public bool IsConnected()
		{
			return _memoryModule != null;
		}


		private CancellationTokenSource _cancellationTokenSource = null;

		private IntPtr _baseAddress = IntPtr.Zero;
		private MemoryModule _memoryModule = null;
		private Dictionary<BuffType, List<int>> _offsets = new Dictionary<BuffType, List<int>>();
		private List<int> _speedReadOffsets;

		private Mutex _buffQueueMutex = new Mutex();
		private BuffState _buffState = new BuffState();

		private Mutex _maxValuesMutex = new Mutex();
		public Dictionary<BuffType, int> _maxValues = new Dictionary<BuffType, int>();

		private readonly int FRAME_DURATION = 1000 / 60;
		private readonly int BUFF_COOLDOWN = -1000;
	}

	class BuffState
	{
		public Dictionary<BuffType, Queue<Buff>> BuffQueue = new Dictionary<BuffType, Queue<Buff>>();
		public Dictionary<BuffType, Buff> CurrentBuffs = new Dictionary<BuffType, Buff>();
	}
}
