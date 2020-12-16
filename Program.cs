using System;
using System.Collections.Generic;
using Amazon.Route53;
using Amazon.Route53.Model;

namespace route53
{
	class Program
	{

		private enum Op
		{
			Add,
			Delete,
			List,
			ListZones
		}

		private delegate void Func();

		private static Func[] functions = { CreateHost, DeleteHostName, ListHostNames, ListZones };
		private static Op? op = null;
		private static Amazon.Route53.RRType recordType;
		private static AmazonRoute53Client c;
		private static string host;

		private static string domain;
		private static string value;
		private static bool quiet = false;
		private static int ttl=60;

		static void Main(string[] args)
		{
			Console.WriteLine();
			c = new AmazonRoute53Client();
			if (parseArgs(args))
			{
				if (!quiet)
				{
					Console.WriteLine("route53 - AWS Route53 DNS management helper - Tristan Phillips");
				}
				try 
				{
					functions[(int)op]();
				}
				catch(Exception ex)
				{
					Console.WriteLine($"{ex.Message}");
				}
			}
			else
			{
				showUsage();
			}
		}

		private static bool parseArgs(string[] args)
		{
			for(int x = 0; x < args.Length; x++)
			{
				if (args[x] == "-a") 
				{
					op = Op.Add;
				}
				if (args[x] == "-D") 
				{
					op = Op.Delete;
				}
				if (args[x] == "-z") 
				{
					op = Op.ListZones;
				}
				if (args[x] == "-l") 
				{
					op = Op.List;
				}
				if (args[x] == "-A") 
				{
					recordType = RRType.A;
				}
				if (args[x] == "-T") 
				{
					recordType = RRType.TXT;
				}
				if (args[x] == "-C") 
				{
					recordType = RRType.CNAME;
				}
				if (args[x] == "-h") 
				{
					host = args[x+1];
				}
				if (args[x] == "-d") 
				{
					domain = args[x+1];
				}
				if (args[x] == "-v") 
				{
					value = args[x+1];
				}
				if (args[x] == "-q") 
				{
					quiet = true;
				}
				if (args[x] == "-t") 
				{
					ttl = int.Parse(args[x+1]);
				}
			}
			if (op == null)
			{
				Console.WriteLine("You must specify an operation (-a, -D, -z or -l).");
				return false;
			}
			if (op == Op.Add && (recordType == null || String.IsNullOrEmpty(host) || String.IsNullOrEmpty(domain) || String.IsNullOrEmpty(value)))
			{
				Console.WriteLine("To add you must specify the domain (-d), host (-h), type (-A, -T or -C) and value (-v).");
				return false;
			}
			if (op == Op.Delete && (String.IsNullOrEmpty(host) || String.IsNullOrEmpty(domain)))
			{
				Console.WriteLine("To delete you must specify the domain (-d) and host (-h).");
				return false;
			}
			if (op == Op.List && String.IsNullOrEmpty(domain))
			{
				Console.WriteLine("To list you must specify the domain (-d).");
				return false;
			}
			return true;
		}

		private static void showUsage()
		{
			Console.WriteLine("\r\nusage: route53 -a -D -z -l -A -T -C -h -d -v -q\r\n");
			Console.WriteLine("Examples:");
			Console.WriteLine("\r\n\tList all hosted zones (domains):");
			Console.WriteLine("\t\troute53 -z");
			Console.WriteLine("\r\n\tList all entries for a domain:");
			Console.WriteLine("\t\troute53 -d domain.com -l");
			Console.WriteLine("\r\n\tList all entries for a domain without details:");
			Console.WriteLine("\t\troute53 -d domain.com -l -q");
			Console.WriteLine("\r\n\tCreate an A record entry for a domain:");
			Console.WriteLine("\t\troute53 -d domain.com -a -A -h <hostname> -v <ip> -t <ttl (default 60)>");
			Console.WriteLine("\r\n\tCreate a CNAME record entry for a domain:");
			Console.WriteLine("\t\troute53 -d domain.com -a -C -h <hostname> -v <cname value> -t <ttl (default 60)>");
			Console.WriteLine("\r\n\tDelete a CNAME record entry for a domain:");
			Console.WriteLine("\t\troute53 -d domain.com -D -h <hostname> -C -v <cname value> -t <ttl (default 60)>");
			Console.WriteLine("\r\n\tDelete an A record record entry for a domain:");
			Console.WriteLine("\t\troute53 -d domain.com -D -h <hostname> -A -v <ip> -t <ttl (default 60)>");
			Console.WriteLine();
		}

		public static void CreateHost()
		{
			Console.WriteLine($"Creating {host}.{domain} ({recordType} = {value})");
			CreateDNSChangeAction(domain, host, value, recordType, ChangeAction.CREATE, ttl);
		}

		public static void ListZones()
		{
			
			var listHostedZonesRequest = new ListHostedZonesRequest();
			var listHostedZonesResponse = c.ListHostedZonesAsync(listHostedZonesRequest).Result;
			foreach(var z in listHostedZonesResponse.HostedZones)
			{
				Console.WriteLine($"{z.Name}");
			}
		}

		public static void ListHostNames()
		{
			if (!quiet)
			{
				Console.WriteLine($"Domain = {domain}");
			}
			string zoneId = FindHostedZoneID(domain);
			if (zoneId == null)
			{
				throw new Exception("Zone not found");
			}
			bool more = true;
			bool first = true;
			string start = "";
			while(more)
			{
				ListResourceRecordSetsRequest r = new ListResourceRecordSetsRequest(zoneId);
				r.MaxItems = "150";
				if (!first)
				{
					r.StartRecordName = start;
				}
				first = false;
				ListResourceRecordSetsResponse res = c.ListResourceRecordSetsAsync(r).Result;
				foreach(var s in res.ResourceRecordSets)
				{
					string firstVal = (s.ResourceRecords != null && s.ResourceRecords.Count > 0) ? s.ResourceRecords[0].Value : "";
					Console.WriteLine($"{s.Name.PadRight(60)}" + (quiet ? "" : $"\t{s.Type}\t{firstVal}"));
					/*
					if (!quiet)
					{
						foreach(var rr in s.ResourceRecords)
						{
							Console.WriteLine($"\t{rr.Value}");
						}
					}
					*/
				}
				more = res.IsTruncated;
				if (more)
				{
					start = res.NextRecordName;
				}
			}
		}

		public static void DeleteHostName()
		{
			Console.WriteLine($"Deleting {host}.{domain}");
			CreateDNSChangeAction(domain, host, value, recordType, ChangeAction.DELETE, ttl);
		}

		private static void CreateDNSChangeAction(string domain, string hostName, string value, RRType type, ChangeAction action, int ttl)
		{
			string zoneId = FindHostedZoneID(domain);
			if (zoneId == null)
			{
				throw new Exception("Zone not found");
			}

			ResourceRecord resourceRecord = null;
			if (value != null)
			{
				resourceRecord = new ResourceRecord() { Value = value };
			}

			var change = new Change
			{
				Action = action,
				ResourceRecordSet = new ResourceRecordSet
				{
					Name = $"{hostName}.{domain}",
					Type = type,
					TTL = ttl,
					ResourceRecords = resourceRecord != null ? new List<ResourceRecord>() { resourceRecord } : null
				},
			};

			var changeBatch = new ChangeBatch();
			changeBatch.Changes.Add(change);
			var changeResourceRecordSetsRequest = new ChangeResourceRecordSetsRequest
			{
				ChangeBatch = changeBatch,
				HostedZoneId = zoneId
			};

			var changeResourceResponse = c.ChangeResourceRecordSetsAsync(changeResourceRecordSetsRequest).Result;
			Console.WriteLine($"{changeResourceResponse.ChangeInfo.Status} {changeResourceResponse.ChangeInfo.Comment}");
		}

		private static string FindHostedZoneID(string domain)
		{
			string ret = null;
			var listHostedZonesRequest = new ListHostedZonesByNameRequest
			{
				DNSName = domain
			};
			var listHostedZonesResponse = c.ListHostedZonesByNameAsync(listHostedZonesRequest).Result;
			if (listHostedZonesResponse.HostedZones.Count > 0)
			{
				ret = listHostedZonesResponse.HostedZones[0].Id;
			}
			return ret;
		}

	}
}
