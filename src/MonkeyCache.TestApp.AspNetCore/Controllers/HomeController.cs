using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MonkeyCache.TestApp.AspNetCore.Models;
using Ooui;
using Ooui.AspNetCore;
using Ooui.Forms;
using Xamarin.Forms;

namespace MonkeyCache.TestApp.AspNetCore.Controllers
{
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            var element = new MainPage().GetOouiElement();
            return new ElementResult(element, title: "Monkey Cache!");
        }


        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
