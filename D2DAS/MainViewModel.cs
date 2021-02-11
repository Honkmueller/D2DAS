using System.Windows.Input;
using System.Windows.Media;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using D2DAS.Mvvm;
using D2DAS.D2Hacker;
using D2DAS.DonationTracker;

namespace D2DAS
{
	public class MainViewModel : ObservableObject
	{
		public MainViewModel()
		{
			_tracker = new Tracker(_hacker);

			StatusMessage = "Not connected";

			// Colors for the status message
			_brushes = new SolidColorBrush[]
			{
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("Black")),
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("DarkGreen")),
				new SolidColorBrush((Color)ColorConverter.ConvertFromString("Red"))
			};
			StatusColor = _brushes[0];

			// Setting the combo box to the first hashtag
			var enumerator = App.Settings.Hashtags.Keys.GetEnumerator();
			enumerator.MoveNext();
			
			SelectedHashtag = enumerator.Current;
			TipAmount = "1";
		}

		// Limit the intput field of the $ amount to just integers
		public void IsAllowedInput(object sender, TextCompositionEventArgs e)
		{
			Regex _regex = new Regex("[^0-9]+");
			e.Handled = _regex.IsMatch(e.Text);
		}

		private void ConnectHacker()
		{
			IsConnected = _hacker.Connect();

			if (IsConnected)
			{
				StatusMessage = "Connected";
				StatusColor = _brushes[1];
			}
			else
			{
				StatusMessage = "Diablo II not found";
				StatusColor = _brushes[2];
			}
		}

		private void SetupOffsets()
		{
			IsHackSetUp = _hacker.SetupOffsetsFromClipboard();

			if (IsHackSetUp)
			{
				StatusMessage = "Set up and ready to go.";
				StatusColor = _brushes[1];
			}
			else
			{
				StatusMessage = "Bad data in clipboard";
				StatusColor = _brushes[2];
			}
		}

		private void SendTip()
		{
			if (string.IsNullOrWhiteSpace(TipAmount))
			{
				return;
			}

			Buff buff = _tracker.GetBuffFromMessage(SelectedHashtag, int.Parse(TipAmount));
			_hacker.EnqueueBuff(buff);
		}

		public ICommand ConnectHackerCommand
		{
			get 
			{
				return new DelegateCommand(ConnectHacker);
			}
		}

		public ICommand SetupOffsetsCommand
		{
			get
			{
				return new DelegateCommand(SetupOffsets);
			}
		}

		public ICommand SendTipCommand
		{
			get
			{
				return new DelegateCommand(SendTip);
			}
		}

		public string StatusMessage
		{
			get => _statusMessage;

			set
			{
				if (value != _statusMessage)
				{
					_statusMessage = value;
					RaisePropertyChangedEvent("StatusMessage");
				}
			}
		}

		public SolidColorBrush StatusColor
		{
			get => _statusColor;

			set
			{
				if (value != _statusColor)
				{
					_statusColor = value;
					RaisePropertyChangedEvent("StatusColor");
				}
			}
		}

		public bool IsConnected
		{
			get => _isConnected;

			set
			{
				if (value != _isConnected)
				{
					_isConnected = value;
					RaisePropertyChangedEvent("IsConnected");

					// IsAllSetUp is dependend to this property
					RaisePropertyChangedEvent("IsAllSetUp");

					if (IsAllSetUp)
					{
						_tracker.TrackDonations();
					}
				}
			}
		}

		public bool IsHackSetUp
		{
			get => _isHackSetUp;

			set
			{
				if (value != _isHackSetUp)
				{
					_isHackSetUp = value;
					RaisePropertyChangedEvent("IsHackSetUp");

					// IsAllSetUp is dependend to this property
					RaisePropertyChangedEvent("IsAllSetUp");

					if (IsAllSetUp)
					{
						_tracker.TrackDonations();
					}
				}
			}
		}

		public string TipAmount
		{
			get; set;
		}

		public Dictionary<string, Hashtag> Hashtags
		{
			get => App.Settings.Hashtags;
		}

		public string SelectedHashtag
		{
			get; set;
		}

		public bool IsAllSetUp
		{
			get
			{
				return _isConnected && _isHackSetUp;
			}
		}

		// Backing fields
		private string _statusMessage = "";
		private SolidColorBrush _statusColor;
		private bool _isConnected = false;
		private bool _isHackSetUp = false;

		private SolidColorBrush[] _brushes;
		private Hacker _hacker = new Hacker();
		private Tracker _tracker;
	}
}
