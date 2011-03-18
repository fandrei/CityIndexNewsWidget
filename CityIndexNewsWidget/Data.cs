using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

using CIAPI.DTO;
using CIAPI.Rpc;
using CIAPI.Streaming;
using StreamingClient;

namespace CityIndexNewsWidget
{
	public class Data : IDisposable
	{
		public NewsDTO[] News { get; private set; }

		public void RefreshNews(Action onSuccess, Action<Exception> onError)
		{
			SubscribeNewsUpdates(null, onError);

			RefreshNewsAsync(onSuccess, onError);
			//ThreadPool.QueueUserWorkItem(x => RefreshNewsSyncThreadEntry(onSuccess, onError));
		}

		private void RefreshNewsAsync(Action onSuccess, Action<Exception> onError)
		{
			var client = new Client(RPC_URI);
			client.BeginLogIn(USERNAME, PASSWORD,
				ar =>
				{
					try
					{
						client.EndLogIn(ar);

						client.BeginListNewsHeadlines(ApplicationSettings.Instance.CategoryCode,
							ApplicationSettings.Instance.MaxCount,
							ar2 =>
							{
								try
								{
									var resp = client.EndListNewsHeadlines(ar2);
									News = resp.Headlines;

									_client.BeginLogOut(ar3 => _client.EndLogOut(ar3), null);

									onSuccess();
								}
								catch (Exception exc)
								{
									onError(exc);
								}
							}, null);
					}
					catch (Exception exc)
					{
						onError(exc);
					}
				}, null);
		}

		public void GetNewsDetailAsync(int storyId, Action<NewsDetailDTO> onSuccess, Action<Exception> onError)
		{
			var client = new Client(RPC_URI);
			client.BeginLogIn(USERNAME, PASSWORD,
				ar =>
				{
					try
					{
						client.EndLogIn(ar);

						client.BeginGetNewsDetail(storyId.ToString(),
							ar2 =>
							{
								try
								{
									var resp = client.EndGetNewsDetail(ar2);

									_client.BeginLogOut(ar3 => _client.EndLogOut(ar3), null);

									onSuccess(resp.NewsDetail);
								}
								catch (Exception exc)
								{
									onError(exc);
								}
							}, null);
					}
					catch (Exception exc)
					{
						onError(exc);
					}
				}, null);
		}

		void RefreshNewsSyncThreadEntry(Action onSuccess, Action<Exception> onError)
		{
			try
			{
				var client = new Client(RPC_URI);
				client.LogIn(USERNAME, PASSWORD);
				var resp = client.ListNewsHeadlines(ApplicationSettings.Instance.CategoryCode,
					ApplicationSettings.Instance.MaxCount);
				News = resp.Headlines;
				client.LogOut();

				onSuccess();

			}
			catch (Exception exc)
			{
				onError(exc);
			}
		}

		void SubscribeNewsUpdates(Action<NewsDTO> onUpdate, Action<Exception> onError)
		{
			_client = new Client(RPC_URI);
			_client.BeginLogIn(USERNAME, PASSWORD,
				ar =>
				{
					try
					{
						_client.EndLogIn(ar);
						Debug.WriteLine("Login ok");

						_streamingClient = StreamingClientFactory.CreateStreamingClient(
							STREAMING_URI, USERNAME, _client.SessionId);
						_streamingClient.Connect();
						Debug.WriteLine("\r\n\r\n\r\n\r\n\r\nStreaming connected ok");

						_newsListener = _streamingClient.BuildListener<NewsDTO>("NEWS.MOCKHEADLINES.UK");
						_newsListener.MessageRecieved +=
							(s, e) =>
							{
								var msg = e.Data.Headline;
								Debug.WriteLine(msg);
								//onUpdate(e.Data);
							};
						_newsListener.Start();

						Debug.WriteLine("\r\n\r\n\r\n\r\n\r\nListener started ok");
					}
					catch (Exception exc)
					{
						onError(exc);
					}
				}, null);
		}

		public void Dispose()
		{
			if (_newsListener != null)
			{
				_newsListener.Stop();
				_newsListener = null;
			}
			if (_streamingClient != null)
			{
				_streamingClient.Disconnect();
				_streamingClient = null;
			}
			if (_client != null)
			{
				_client.BeginLogOut(ar => _client.EndLogOut(ar), null);
				_client = null;
			}
		}

		private static readonly Uri RPC_URI = new Uri("http://ciapipreprod.cityindextest9.co.uk/tradingapi");
		private static readonly Uri STREAMING_URI = new Uri("http://pushpreprod.cityindextest9.co.uk/CITYINDEXSTREAMING");

		private const string USERNAME = "xx189949";
		private const string PASSWORD = "password";

		private Client _client;
		private IStreamingClient _streamingClient;
		private IStreamingListener<NewsDTO> _newsListener;
	}
}
