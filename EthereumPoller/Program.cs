using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;
using System.Threading;
using System.Net;
using System.Runtime.Caching;
using Newtonsoft.Json;
using System.Numerics;
using System.Text;

namespace EthereumPoller
{
	public class Program
	{
		public static void Main(string[] args)
		{
			CreateWebHostBuilder(args).Build().Run();
		}

		public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
			WebHost.CreateDefaultBuilder(args)
				.UseStartup<Startup>();



	}

	public class EthereumResponse
	{
		public double jsonrpc { get; set; }
		public long id { get; set; }
		public string result { get; set; }

		/*public string GetResult()
		{
			return result;
		}

		public void SetResult(string value)
		{
			result = value;
		}*/

		/*public void SetResult(string value)
		{
			result = (long)new System.ComponentModel.Int64Converter().ConvertFromString(value);
		}*/
	}


	internal class TimedHostedService : IHostedService, IDisposable
	{
		private readonly ILogger _logger;
		private Timer _timer;

		public TimedHostedService(ILogger<TimedHostedService> logger)
		{
			_logger = logger;
		}

		public Task StartAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Timed Background Service is starting.");

			_timer = new Timer(DoWork, null, TimeSpan.Zero,
				TimeSpan.FromSeconds(20));

			return Task.CompletedTask;
		}

		public string API_Key = "NPM69J2Y5AX7TSUPX7H481S19YU79D4IV7";


		private void DoWork(object state)
		{

			ObjectCache cache = MemoryCache.Default;
			object cacheObject = null;
			/*public ObjectCache getCached()
			{
				return _cache;
			}*/
			
			string blockid = cache["lastEthereumBlock"] as string;


			_logger.LogInformation("Timed Background Service is working.");

			string BlockNumberRequest = "https://api.etherscan.io/api?module=proxy&action=eth_blockNumber&apikey=" + API_Key;

			string response_json = String.Empty;

			HttpWebRequest request = (HttpWebRequest)WebRequest.Create(BlockNumberRequest);
			request.AutomaticDecompression = DecompressionMethods.GZip;

			using (HttpWebResponse response = (HttpWebResponse)request.GetResponse())
			using (Stream stream = response.GetResponseStream())
			using (StreamReader reader = new StreamReader(stream))
			{
				response_json = reader.ReadToEnd();
			}

			string nameWithPath = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location) + @"\cache.dat";

			FileStream cacheFile = File.Open(nameWithPath,FileMode.Create,FileAccess.ReadWrite,FileShare.ReadWrite);
			EthereumResponse jsonResult = JsonConvert.DeserializeObject<EthereumResponse>(response_json);

			Encoding aSCII = Encoding.GetEncoding("ASCII");

			cacheFile.Write(aSCII.GetBytes(jsonResult.result));

			cacheFile.Close();

			/*if (blockid == null)
			{
				CacheItemPolicy policy = new CacheItemPolicy();

				List<string> filePaths = new List<string>();
				filePaths.Add(nameWithPath);
				policy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));
				policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(20);

				// Fetch the file contents.  
				blockid = File.ReadAllText(nameWithPath); //TODO: how to update cached value?

				cacheObject = cache.AddOrGetExisting("lastEthereumBlock", blockid, policy);
				//cache.Set("lastEthereumBlock", blockid, policy);
			}*/

			if (!cache.Contains("lastEthereumBlock"))
			{
				CacheItemPolicy policy = new CacheItemPolicy();

				List<string> filePaths = new List<string>();
				filePaths.Add(nameWithPath);
				policy.ChangeMonitors.Add(new HostFileChangeMonitor(filePaths));
				policy.AbsoluteExpiration = DateTimeOffset.Now.AddSeconds(20);

				// Fetch the file contents.  
				blockid = File.ReadAllText(nameWithPath); //TODO: how to update cached value?


				//var expiration = DateTimeOffset.UtcNow.AddMinutes(5);
				//var sections = context.Sections.ToList();

				cache.Add("lastEthereumBlock", blockid, policy);
			}


			Console.WriteLine(response_json);
			//Console.WriteLine(cache.GetValues((string)null,"lastEthereumBlock"));
			Console.WriteLine(cacheObject);
			Console.WriteLine(cache.Get("lastEthereumBlock",null));

		}

		public Task StopAsync(CancellationToken cancellationToken)
		{
			_logger.LogInformation("Timed Background Service is stopping.");

			_timer?.Change(Timeout.Infinite, 0);

			return Task.CompletedTask;
		}

		public void Dispose()
		{
			_timer?.Dispose();
		}
	}
}
