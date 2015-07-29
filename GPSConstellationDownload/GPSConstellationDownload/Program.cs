using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace GPSConstellationDownload
{
	class Program
	{
		static void Main( string[] args )
		{
			using ( var wc = new WebClient() )
			{
				try
				{
					#region Web access (Credentials)


					#endregion

					#region Web header information

					String ipAddress = "10.2.155.130";
					
					//string urlVector = "http://" + ipAddress + "/xml/vector.html";

					string urlVector = "http://www.navcen.uscg.gov";
					var resultVector = wc.DownloadString( urlVector );
					urlVector = "http://www.navcen.uscg.gov/?Do=constellationStatus";
					resultVector = wc.DownloadString( urlVector );

					string urlVectorXml = urlVector + "/xml/dynamic/merge.xml?posData=&svData= HTTP/1.1";
					var resultVectorXml = wc.DownloadString( urlVectorXml );

					#endregion

				}
				catch
				{

				}

				Console.WriteLine();
				Console.WriteLine();
				Console.WriteLine();
			}
		}
	}
}
