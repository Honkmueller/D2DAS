using System;
using RestSharp;
using RestSharp.Authenticators;
using Newtonsoft.Json;

namespace D2DAS.DonationTracker
{
	public class StreamElementsTips
	{
		public StreamElementsTips(string accountID, string jwtToken)
		{
			_streamElementsClient = new RestClient("https://api.streamelements.com/kappa/v2/");
			_streamElementsClient.Authenticator = new JwtAuthenticator(jwtToken);
			_accountID = accountID;
		}

		public TipsResponse GetTips()
		{
			RestRequest request = new RestRequest($"tips/{_accountID}", DataFormat.Json);
			IRestResponse response = _streamElementsClient.Execute(request);
			TipsResponse tipsResponse = JsonConvert.DeserializeObject<TipsResponse>(response.Content);
			return tipsResponse;
		}

		private RestClient _streamElementsClient;
		private string _accountID;
	}

	public class TipsResponse
	{
		public Doc[] docs;
		public int total;
		public int limit;
		public int offset;
	}

	public class Doc
	{
		public Donation donation;
		public string provider;
		public string status;
		public bool deleted;
		public string _id;
		public string channel;
		public string transactionId;
		public string createdAt;
		public string approved;
		public string updatedAt;
	}

	public class Donation
	{
		public User user;
		public string message;
		public int amount;
		public string currency;
	}

	public class User
	{
		public string username;
		public string geo;
		public string mail;
	}
}
