using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Linq;
using HtmlAgilityPack;

namespace TrimbleReceiverVector
{

	class Program
	{

		private static System.Timers.Timer aTimer;

		static void Main( string[] args )
		{
			//Console.WriteLine( "Enter the Rover IP address" );
			//String ipAddress = Console.ReadLine();

			aTimer = new System.Timers.Timer();

			aTimer.Interval = 1000;

			// Fires the event
			aTimer.Elapsed += OnTimedEvent;

			// Have the timer fire repeated events (true is the default)
			aTimer.AutoReset = true;

			// Start the timer
			aTimer.Enabled = true;

			//// To finish the Application
			//Console.WriteLine( "Press the Enter key to exit the program at any time... " );
			Console.ReadLine();

		}

		private static void OnTimedEvent( Object source, ElapsedEventArgs e )
		{

			using ( var wc = new WebClient() )
			{
				try
				{
					#region Web access (Credentials)

					String username = "admin";
					String password = "password";

					wc.Credentials = new NetworkCredential( username, password );

					string credentials = Convert.ToBase64String( Encoding.ASCII.GetBytes( username + ":" + password ) );

					wc.Headers[HttpRequestHeader.Authorization] = "Basic " + credentials;

					#endregion

					#region Web header information

					string ipAddress = "10.2.155.130";

					string urlVector = "http://" + ipAddress + "/xml/vector.html";
					string urlVectorXml = "http://" + ipAddress + "/xml/dynamic/merge.xml?posData=&svData=%20HTTP/1.1";

					//var resultVector = wc.DownloadString( urlVector );
					var resultVectorXml = wc.DownloadString( urlVectorXml );

					String SourceVectorXml = WebUtility.HtmlDecode( resultVectorXml );

					//HtmlAgilityPack.HtmlDocument htmlDocu = new HtmlAgilityPack.HtmlDocument();
					//HtmlAgilityPack.HtmlDocument htmlDocuXml = new HtmlAgilityPack.HtmlDocument();

					XmlDocument document = new XmlDocument();
					document.LoadXml( resultVectorXml );

					#endregion

					#region Parsing of <Time> section in XML

					XmlNode nodeTime = document.GetElementsByTagName( "time" )[0];

					String Status = String.Empty;
					int WeekNo = 0, sec = 0, utcOff = 0, msecs = 0;

					foreach ( XmlNode childNodeTime in nodeTime )
					{
						switch ( childNodeTime.Name )
						{
							case "status":
								Status = childNodeTime.InnerText;
								break;
							case "week":
								WeekNo = Convert.ToInt32( childNodeTime.InnerText );
								break;
							case "sec":
								sec = Convert.ToInt32( childNodeTime.InnerText );
								break;
							case "utcOff":
								utcOff = Convert.ToInt32( childNodeTime.InnerText );
								break;
							case "msecs":
								msecs = Convert.ToInt32( childNodeTime.InnerText );
								break;
							default:
								break;
						}
					}

					Time roverCurrentEpoch = new Time( Status, WeekNo, sec, utcOff, msecs );

					#endregion
					// At this point roverCurrentEpoch contains info about current epoch in Rover

					#region Parsing of <rtk> section in XML

					List<ProcessedSatellite> roverCurrentProcessedSatellites = new List<ProcessedSatellite>();
					int satellitesUsingL5 = 0;

					XmlNodeList nodesRtk = document.GetElementsByTagName( "rtk" );

					XmlNode node = nodesRtk[0];
					if ( node != null )
					{


						XmlDocument x = new XmlDocument();
						String s = node.InnerXml;
						x.LoadXml( "<rtk>" + s + "</rtk>" );
						XmlNodeList nodesSv = x.GetElementsByTagName( "sv" );

						Boolean L1stat, L2stat, L5stat;


						foreach ( XmlNode nodeSv in nodesSv )
						{
							L1stat = false;
							L2stat = false;
							L5stat = false;

							int constellation = Convert.ToInt32( nodeSv.Attributes["sys"].Value );
							int svId = Convert.ToInt32( nodeSv.Attributes["id"].Value );

							foreach ( XmlNode childNode in nodeSv )
							{
								switch ( childNode.Name )
								{
									case "L1stat":
										if ( childNode.InnerText == "R" )
											L1stat = true;
										break;
									case "L2stat":
										if ( childNode.InnerText == "R" )
											L2stat = true;
										break;

									case "L5stat":
										if ( childNode.InnerText == "R" )
										{
											L5stat = true;
											satellitesUsingL5++;
										}
										break;

									default:
										break;
								}
							}

							//int constellation = 0; // Convert.ToInt32( reader.GetAttribute( "sys" ) );
							//int svId = 0; // Convert.ToInt32( reader.GetAttribute( "id" ) );
							ProcessedSatellite currentSatellite = new ProcessedSatellite( constellation, svId, L1stat, L2stat, L5stat );
							roverCurrentProcessedSatellites.Add( currentSatellite );

						}
					}
					#endregion
					// At this point roverCurrentProcessedSatellites contains info about current Satellites processed by Rover

					#region Print Processing on Screen real-time

					String Line1 = ipAddress.ToString();
					Console.WriteLine( "Rover IP Address: " + Line1 );

					String Line2 = roverCurrentEpoch.ToString();
					Console.WriteLine( "Current Time: " + Line2 );

					String Line3 = roverCurrentProcessedSatellites.Count.ToString();
					Console.WriteLine( "Number of Processed satellites: " + Line3 );

					String Line4 = satellitesUsingL5.ToString();
					Console.WriteLine( "Number of Processed satellites using L5: " + Line4 );
					Console.WriteLine( String.Empty );


					String epochProcessedTime = Line1 + ";" + Line2 + ";" + Line3 + ";" + Line4 + ";";

					String epochProcessedSatelliteInfo = String.Empty;

					foreach ( ProcessedSatellite satellite in roverCurrentProcessedSatellites )
					{
						Console.WriteLine( "Constellation: " + satellite.Constellation );
						Console.WriteLine( "Sv: " + satellite.SvId.ToString() );
						Console.WriteLine( "L1 freq: " + satellite.IsL1Resolved.ToString() );
						Console.WriteLine( "L2 freq: " + satellite.IsL2Resolved.ToString() );
						Console.WriteLine( "L5 freq: " + satellite.IsL5Resolved.ToString() );
						Console.WriteLine();
					}


					#endregion

					#region Save info into a file

					String filePath = @"C:\\Temp\\Rover_" + ipAddress + ".txt";
					int saveAttempt = 0;

					//while ( !File.Exists( filePath ) )
					//{
					//	filePath = "C:\\Temp\\Rover_" + ipAddress + "_" + saveAttempt.ToString() + ".txt";
					//	saveAttempt++;
					//}

					if ( !File.Exists( filePath ) )
					{
						using ( StreamWriter sw = File.CreateText( filePath ) ) 
						{
							sw.WriteLine( "Rover IP Address; Current Time; Number of Processed satellites; Number of Processed satellites using L5" );
							sw.WriteLine( "Constellation; Sv; L1Freq; L2Freq; L5Freq " );
							sw.WriteLine(epochProcessedTime);

							

							foreach ( ProcessedSatellite satellite in roverCurrentProcessedSatellites )
							{
								String processedSatelliteInfo = satellite.Constellation + ";" +
									satellite.SvId.ToString() + ";" +
									satellite.IsL1Resolved.ToString() + ";" +
									satellite.IsL2Resolved.ToString() + ";" +
									satellite.IsL5Resolved.ToString() + ";";

								sw.WriteLine( processedSatelliteInfo );
							}
						}
							//byte[] data = Encoding.UTF8.GetBytes( epochProcessedTime );
							//fileStream.Write( data, 0, data.Length );

							//byte[] data2 = Encoding.UTF8.GetBytes( epochProcessedSatelliteInfo );
							//fileStream.Write( data2, 0, data2.Length );
						//}
					}
					else
					{
						using ( StreamWriter sw = File.AppendText( filePath ) )
						{
							sw.WriteLine( epochProcessedTime );

							foreach ( ProcessedSatellite satellite in roverCurrentProcessedSatellites )
							{
								String processedSatelliteInfo = satellite.Constellation + ";" +
									satellite.SvId.ToString() + ";" +
									satellite.IsL1Resolved.ToString() + ";" +
									satellite.IsL2Resolved.ToString() + ";" +
									satellite.IsL5Resolved.ToString() + ";";

								sw.WriteLine( processedSatelliteInfo );
							}
						}	

					}

					//	//epochProcessedSatelliteInfo
					//	//epochProcessedTime
					//	//String.Empty

					#endregion

				}
				catch ( WebException )
				{

					throw;
				}

			}

			Console.WriteLine();
			Console.WriteLine();
			Console.WriteLine();

		}

	}
}
