using System;
using System.Collections.Generic;
using System.Threading;

using CIAPI.DTO;
using CIAPI.Rpc;
using CIAPI.Streaming;

namespace CityIndexNewsWidget
{
	public class Data : IDisposable
	{
		private NewsDTO[] _news;
		public NewsDTO[] News
		{
			get { return _news; }
		}

		public void GetNews(Action onSuccess, Action<NewsDTO> onUpdate, Action<Exception> onError)
		{
			//RefreshNewsAsync(onSuccess, onError);
			//ThreadPool.QueueUserWorkItem(x => SubscribeToNewsHeadlineStream(onUpdate));
		}

		public void RefreshNews(Action onSuccess, Action<Exception> onError)
		{
			throw new Exception("aaa");
			RefreshNewsAsync(onSuccess, onError);
			//ThreadPool.QueueUserWorkItem(x => RefreshNewsSyncThreadEntry(onSuccess, onError));
			//RefreshNewsDummy(onSuccess, onError);
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
									_news = resp.Headlines;

									client.BeginLogOut(ar3 => { }, null);

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

									client.BeginLogOut(ar3 => { }, null);

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
				_news = resp.Headlines;
				client.LogOut();

				onSuccess();

			}
			catch (Exception exc)
			{
				onError(exc);
			}
		}

		void RefreshNewsDummy(Action onSuccess, Action<Exception> onError)
		{
			var dummyList = new List<NewsDTO>();
			for (int i = 0; i < ApplicationSettings.Instance.MaxCount; i++)
			{
				var text = "news " + i;
				for (int t = 0; t < 5; t++)
					text += text;
				dummyList.Add(
					new NewsDTO
					{
						Headline = text,
						PublishDate = DateTime.Now,
						StoryId = i
					});
			}
			_news = dummyList.ToArray();

			onSuccess();
		}

		private static readonly Uri RPC_URI = new Uri("http://ciapipreprod.cityindextest9.co.uk/tradingapi");
		private static readonly Uri STREAMING_URI = new Uri("http://pushpreprod.cityindextest9.co.uk/CITYINDEXSTREAMING");

		private const string USERNAME = "xx189949";
		private const string PASSWORD = "password";

		public void Dispose()
		{
		}
	}
}
