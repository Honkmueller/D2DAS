using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using D2DAS.D2Hacker;

namespace D2DAS.DonationTracker
{
	public class Tracker
	{
		public Tracker(Hacker hacker)
		{
			_hacker = hacker;
			_startTime = DateTime.Now;
			_tips = new StreamElementsTips(App.Settings.AccountID, App.Settings.JWTToken);
		}

		public Buff GetBuffFromMessage(string content, int amount)
		{
			Buff buff = null;
			foreach (KeyValuePair<string, Hashtag> kvp in App.Settings.Hashtags)
			{
				if (content.ToLower().Contains(kvp.Key.ToLower()))
				{
					Hashtag hashtag = kvp.Value;

					int duration = 0;
					// Allow the [t]/$[d] syntax
					string[] durationComponents = hashtag.Duration.Split(new string[] { "/$" }, StringSplitOptions.None);
					// from now on, duration is kept in ms
					duration = int.Parse(durationComponents[0]) * 1000;
					if (durationComponents.Length > 1)
					{
						int multiplier = amount / int.Parse(durationComponents[1]);
						duration *= multiplier;
					}

					int value = 0;

					// Allow to use the current max value
					if (hashtag.Value == "max")
					{
						if (hashtag.ValueType == BuffValueType.Total)
						{
							value = _hacker.GetMaxValue(hashtag.Type);
						}
						else
						{
							value = 100;
						}
					}
					else
					{
						// Allow the [v]/$[d] syntax
						string[] valueComponents = hashtag.Value.Split(new string[] { "/$" }, StringSplitOptions.None);
						value = int.Parse(valueComponents[0]);
						if (valueComponents.Length > 1)
						{
							int multiplier = amount / int.Parse(valueComponents[1]);
							value *= multiplier;
						}
					}

					buff = new Buff(hashtag.Type, hashtag.Effect, hashtag.ValueType, value, duration);
					break;
				}
			}

			return buff;
		}

		public async void TrackDonations()
		{
			_cancellationTokenSource = new CancellationTokenSource();
			CancellationToken token = _cancellationTokenSource.Token;
			_isRunning = true;
			await Task.Run(() => DonationTick(token));
			_cancellationTokenSource.Dispose();
		}

		public void PauseTracking()
		{
			_isRunning = false;
			_cancellationTokenSource.Cancel();
		}

		public bool IsRunning()
		{
			return _isRunning;
		}

		private void DonationTick(CancellationToken token)
		{
			while (!token.IsCancellationRequested)
			{
				TipsResponse response = _tips.GetTips();

				foreach (Doc doc in response.docs)
				{
					// Only track new donations...
					if (!_trackedIDs.Contains(doc._id))
					{
						DateTime creationTime = DateTime.Parse(doc.createdAt);

						// Only track donations, that came in after tracking started
						if (creationTime > _startTime)
						{
							Buff buff = GetBuffFromMessage(doc.donation.message, doc.donation.amount);

							if (buff != null)
							{
								_hacker.EnqueueBuff(buff);
							}
						}

						_trackedIDs.Add(doc._id);
					}
				}

				Thread.Sleep(1000);
			}
		}

		private CancellationTokenSource _cancellationTokenSource = null;

		private bool _isRunning = false;
		private List<string> _trackedIDs = new List<string>();
		private DateTime _startTime;

		private Hacker _hacker;
		private StreamElementsTips _tips;
	}
}
