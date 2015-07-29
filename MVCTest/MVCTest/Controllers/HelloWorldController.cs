using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace MVCTest.Controllers
{
    public class HelloWorldController : Controller
    {
        // GET: HelloWorld
		public ActionResult Index( string Name, int nTimes = 1 )
		{
			ViewBag.Message = "Hello " + Name;
			ViewBag.NumTimes = nTimes;
			return View();
		}

		//public String Index()
		//{
		//	//return View();
		//	return "This is my <b>default</b> action...";
		//}

		//public string Welcome()
		//{
		//	return "This is the Welcome action method...";
		//}

		public string Welcome( string name, int ID = 1)
		{
			return HttpUtility.HtmlEncode( "Hello " + name + ", ID: " + ID );
		}  

    }
}